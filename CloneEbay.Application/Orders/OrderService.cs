using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Orders;

public sealed class OrderService(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IClock clock) : IOrderService
{
    public OrderService(IOrderRepository orderRepository, IUnitOfWork unitOfWork, IClock clock)
        : this(orderRepository, null!, null!, unitOfWork, clock)
    {
    }
    public async Task<OrderDetailResponse> CreateManualOrderAsync(
        int sellerId,
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new AppProblemException(422, "ITEMS_REQUIRED", "At least one product item is required to create a manual order.");
        }

        var now = clock.UtcNow;
        var orderItems = new List<OrderItem>();
        decimal totalPrice = 0m;

        foreach (var itemReq in request.Items)
        {
            if (itemReq.Quantity <= 0)
            {
                throw new AppProblemException(422, "INVALID_QUANTITY", "Item quantity must be greater than zero.");
            }

            var product = await productRepository.GetByIdAsync(itemReq.ProductId, true, cancellationToken)
                ?? throw new AppProblemException(404, "PRODUCT_NOT_FOUND", $"Product #{itemReq.ProductId} was not found.");

            if (product.SellerId != sellerId)
            {
                throw new AppProblemException(403, "FORBIDDEN_NOT_OWNER", "You can only create orders for your own products.");
            }

            var availableQty = product.Inventory?.Quantity ?? 0;
            if (availableQty < itemReq.Quantity)
            {
                throw new AppProblemException(409, "INSUFFICIENT_STOCK", $"Insufficient stock for '{product.Title}'. Required: {itemReq.Quantity}, Available: {availableQty}.");
            }

            var unitPrice = itemReq.UnitPrice.HasValue && itemReq.UnitPrice.Value > 0 ? itemReq.UnitPrice.Value : product.Price;
            totalPrice += unitPrice * itemReq.Quantity;

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                Product = product,
                Quantity = itemReq.Quantity,
                UnitPrice = unitPrice
            });
        }

        var fvf = Math.Round(totalPrice * 0.10m, 2);

        var paymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Pay Cash on Delivery (COD)" : request.PaymentMethod.Trim();
        var isCod = paymentMethod.Contains("COD", StringComparison.OrdinalIgnoreCase);
        var status = isCod ? "AWAITING_SHIPMENT" : "AWAITING_PAYMENT";

        var buyerId = request.BuyerId ?? 1;
        if (!string.IsNullOrWhiteSpace(request.BuyerUsername))
        {
            var user = await userRepository.GetByEmailAsync(request.BuyerUsername.Trim().ToUpperInvariant(), cancellationToken);
            if (user != null) buyerId = user.Id;
        }

        Address? address = null;
        if (!string.IsNullOrWhiteSpace(request.BuyerFullName) || !string.IsNullOrWhiteSpace(request.Street) || !string.IsNullOrWhiteSpace(request.City))
        {
            var country = string.IsNullOrWhiteSpace(request.Country) ? "Vietnam" : request.Country.Trim();
            var city = string.IsNullOrWhiteSpace(request.City) ? "Hà Nội" : request.City.Trim();
            var state = string.IsNullOrWhiteSpace(request.District) ? "Quận Cầu Giấy" : request.District.Trim();
            var street = string.IsNullOrWhiteSpace(request.Street) ? "Địa chỉ giao hàng" : request.Street.Trim();

            address = new Address
            {
                UserId = buyerId,
                FullName = string.IsNullOrWhiteSpace(request.BuyerFullName) ? "Khách hàng" : request.BuyerFullName.Trim(),
                Phone = request.BuyerPhone?.Trim() ?? "0900000000",
                Street = street,
                City = city,
                State = state,
                Country = country,
                IsDefault = false
            };
        }

        var payment = new Payment
        {
            UserId = buyerId,
            Amount = totalPrice,
            Method = paymentMethod,
            Status = "Pending",
            PaymentGateway = paymentMethod,
            PaidAtUtc = null
        };

        ShippingInfo? shipping = null;
        if (isCod)
        {
            var (carrier, trackingNumber) = GenerateOfficialEbayTracking(address?.Country ?? "Vietnam", paymentMethod);
            shipping = new ShippingInfo
            {
                Carrier = carrier,
                TrackingNumber = trackingNumber,
                Status = "AWAITING_SHIPMENT",
                EstimatedArrivalUtc = now.AddDays(3)
            };

            foreach (var item in orderItems)
            {
                var prod = item.Product;
                if (prod?.Inventory is not null)
                {
                    prod.Inventory.Quantity = Math.Max(0, prod.Inventory.Quantity - item.Quantity);
                }
            }
        }

        var order = new Order
        {
            BuyerId = buyerId,
            OrderDateUtc = now,
            TotalPrice = totalPrice,
            Status = status,
            FinalValueFee = fvf,
            PayoutStatus = "Pending",
            Items = orderItems,
            Address = address,
            Payment = payment,
            ShippingInfo = shipping
        };

        await orderRepository.AddOrderAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetSellerOrderDetailAsync(order.Id, sellerId, cancellationToken);
    }

    public async Task<PagedResult<OrderSummaryDto>> GetSellerOrdersAsync(
        int sellerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagedOrders = await orderRepository.GetSellerOrdersAsync(sellerId, status, page, pageSize, cancellationToken);

        var items = pagedOrders.Items.Select(order =>
        {
            var sellerItems = order.Items
                .Where(item => item.Product.SellerId == sellerId)
                .ToList();
            var sellerSubtotal = sellerItems.Sum(item => item.UnitPrice * item.Quantity);
            var fvf = order.FinalValueFee > 0 ? order.FinalValueFee : Math.Round(sellerSubtotal * 0.10m, 2);
            var net = sellerSubtotal - fvf;
            var payoutStatus = string.Equals(order.Status, "DELIVERED", StringComparison.OrdinalIgnoreCase) ? "Released" : (order.PayoutStatus ?? "Pending");

            var firstItem = sellerItems.FirstOrDefault();
            var firstTitle = firstItem?.Product?.Title ?? $"Đơn hàng #{order.Id}";
            var totalQty = sellerItems.Sum(i => i.Quantity);
            var productTitleDisplay = sellerItems.Count > 1
                ? $"{firstTitle} (+{sellerItems.Count - 1} SP khác)"
                : firstTitle;

            return new OrderSummaryDto(
                order.Id,
                order.OrderDateUtc,
                sellerSubtotal,
                fvf,
                net,
                order.Status,
                payoutStatus,
                productTitleDisplay,
                totalQty);
        }).ToList();

        return new PagedResult<OrderSummaryDto>(
            items,
            pagedOrders.Page,
            pagedOrders.PageSize,
            pagedOrders.TotalItems);
    }

    public async Task<OrderDetailResponse> GetSellerOrderDetailAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetSellerOrderByIdAsync(orderId, sellerId, cancellationToken);
        if (order is null)
        {
            throw new AppProblemException(404, "ORDER_NOT_FOUND", "Order was not found or does not contain items for this seller.");
        }

        var sellerItems = order.Items
            .Where(item => item.Product.SellerId == sellerId)
            .Select(item => new OrderItemDto(
                item.ProductId,
                item.Product.Title,
                item.Quantity,
                item.UnitPrice))
            .ToList();

        AddressDto? addressDto = order.Address is null ? null : new AddressDto(
            order.Address.Id,
            order.Address.FullName,
            order.Address.Phone,
            order.Address.Street,
            order.Address.City,
            order.Address.State,
            order.Address.Country);

        PaymentSummaryDto? paymentDto = order.Payment is null ? null : new PaymentSummaryDto(order.Payment.Method, order.Payment.Status);

        ShippingInfoSummaryDto? shippingDto = order.ShippingInfo is null ? null : new ShippingInfoSummaryDto(
            order.ShippingInfo.Carrier,
            order.ShippingInfo.TrackingNumber,
            order.ShippingInfo.Status,
            order.ShippingInfo.EstimatedArrivalUtc);

        var fvf = order.FinalValueFee > 0 ? order.FinalValueFee : Math.Round(order.TotalPrice * 0.10m, 2);
        var net = order.TotalPrice - fvf;
        var payoutStatus = order.PayoutStatus ?? (string.Equals(order.Status, "DELIVERED", StringComparison.OrdinalIgnoreCase) ? "On Hold (eMBG Hold)" : "Pending");

        return new OrderDetailResponse(
            order.Id,
            order.Status,
            order.TotalPrice,
            fvf,
            net,
            payoutStatus,
            sellerItems,
            addressDto,
            paymentDto,
            shippingDto);
    }

    public async Task<PaymentDetailResponse> GetPaymentInfoAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        var payment = await orderRepository.GetPaymentByOrderIdAsync(orderId, sellerId, cancellationToken);
        if (payment is null)
        {
            var order = await orderRepository.GetSellerOrderByIdAsync(orderId, sellerId, cancellationToken);
            if (order is null)
            {
                throw new AppProblemException(404, "ORDER_NOT_FOUND", "Order was not found or does not contain items for this seller.");
            }

            throw new AppProblemException(404, "PAYMENT_NOT_FOUND", "Payment information not found for this order.");
        }

        return new PaymentDetailResponse(
            payment.Amount,
            payment.Method,
            payment.Status,
            payment.PaidAtUtc);
    }

    public async Task<ShippingInfoSummaryDto> GetShippingInfoAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetSellerOrderByIdAsync(orderId, sellerId, cancellationToken)
            ?? throw new AppProblemException(404, "ORDER_NOT_FOUND", "Order was not found or does not contain items for this seller.");

        if (order.ShippingInfo is null)
        {
            throw new AppProblemException(404, "SHIPPING_INFO_NOT_FOUND", "Shipping information not found for this order.");
        }

        return new ShippingInfoSummaryDto(
            order.ShippingInfo.Carrier,
            order.ShippingInfo.TrackingNumber,
            order.ShippingInfo.Status,
            order.ShippingInfo.EstimatedArrivalUtc);
    }

    public async Task<OrderStatusResponse> CancelOrderAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetSellerOrderByIdAsync(orderId, sellerId, cancellationToken);
        if (order is null)
        {
            throw new AppProblemException(404, "ORDER_NOT_FOUND", "Order was not found or does not contain items for this seller.");
        }

        if (string.Equals(order.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppProblemException(400, "ORDER_ALREADY_CANCELLED", "Order is already cancelled.");
        }

        if (string.Equals(order.Status, "DELIVERED", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppProblemException(400, "CANNOT_CANCEL_DELIVERED", "Delivered orders cannot be cancelled.");
        }

        var now = clock.UtcNow;
        var sellerItems = order.Items.Where(item => item.Product.SellerId == sellerId).ToList();

        if (string.Equals(order.Status, "AWAITING_SHIPMENT", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(order.Status, "SHIPPED", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var item in sellerItems)
            {
                if (item.Product.Inventory is not null)
                {
                    item.Product.Inventory.Quantity += item.Quantity;
                    item.Product.Inventory.LastUpdatedUtc = now;
                }
            }
        }

        order.Status = "CANCELLED";
        if (order.ShippingInfo is not null)
        {
            order.ShippingInfo.Status = "CANCELLED";
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new OrderStatusResponse(order.Id, order.Status);
    }

    public async Task<ConfirmShipmentResponse> ConfirmShipmentAsync(
        int orderId,
        int sellerId,
        ConfirmShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetSellerOrderByIdAsync(orderId, sellerId, cancellationToken);
        if (order is null)
        {
            throw new AppProblemException(404, "ORDER_NOT_FOUND", "Order was not found or does not contain items for this seller.");
        }

        if (string.IsNullOrWhiteSpace(request.TrackingNumber) || string.IsNullOrWhiteSpace(request.CarrierCode))
        {
            throw new AppProblemException(422, "TRACKING_INFO_REQUIRED", "Tracking number and carrier code are required to confirm shipment.");
        }

        var sellerItems = order.Items.Where(item => item.Product.SellerId == sellerId).ToList();
        foreach (var item in sellerItems)
        {
            var availableQty = item.Product.Inventory?.Quantity ?? 0;
            if (availableQty < item.Quantity)
            {
                throw new AppProblemException(409, "INSUFFICIENT_STOCK", $"Insufficient stock for product '{item.Product.Title}'. Required: {item.Quantity}, Available: {availableQty}.");
            }
        }

        var now = clock.UtcNow;
        foreach (var item in sellerItems)
        {
            if (item.Product.Inventory is not null)
            {
                item.Product.Inventory.Quantity -= item.Quantity;
                item.Product.Inventory.LastUpdatedUtc = now;
            }
        }

        if (order.ShippingInfo is null)
        {
            order.ShippingInfo = new ShippingInfo
            {
                OrderId = order.Id,
                Carrier = request.CarrierCode.Trim(),
                TrackingNumber = request.TrackingNumber.Trim(),
                Status = "SHIPPED",
                EstimatedArrivalUtc = now.AddDays(3)
            };
            await orderRepository.AddShippingInfoAsync(order.ShippingInfo, cancellationToken);
        }
        else
        {
            order.ShippingInfo.Carrier = request.CarrierCode.Trim();
            order.ShippingInfo.TrackingNumber = request.TrackingNumber.Trim();
            order.ShippingInfo.Status = "SHIPPED";
        }

        order.Status = "SHIPPED";
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ConfirmShipmentResponse(order.Id, order.Status, request.TrackingNumber, request.CarrierCode);
    }

    public async Task<CreatePayPalOrderResponse> CreatePayPalOrderAsync(
        int orderId,
        int sellerId,
        CreatePayPalOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetSellerOrderByIdAsync(orderId, sellerId, cancellationToken);
        if (order is null)
        {
            throw new AppProblemException(404, "ORDER_NOT_FOUND", "Order was not found or does not contain items for this seller.");
        }

        var paypalOrderId = $"PAYPAL-ORDER-{order.Id}-{Guid.NewGuid():N}";
        var returnUrl = request.ReturnUrl ?? $"https://localhost:7248/orders/{orderId}/paypal-return";
        var cancelUrl = request.CancelUrl ?? $"https://localhost:7248/orders/{orderId}";
        var approvalUrl = $"https://www.sandbox.paypal.com/checkoutnow?token={paypalOrderId}&returnUrl={Uri.EscapeDataString(returnUrl)}";

        if (order.Payment is null)
        {
            order.Payment = new Payment
            {
                OrderId = order.Id,
                UserId = order.BuyerId,
                Amount = order.TotalPrice,
                Method = "PayPal Sandbox",
                Status = "Pending",
                PaypalOrderId = paypalOrderId,
                PaymentGateway = "PayPal Sandbox"
            };
        }
        else
        {
            order.Payment.Method = "PayPal Sandbox";
            order.Payment.PaypalOrderId = paypalOrderId;
            order.Payment.PaymentGateway = "PayPal Sandbox";
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CreatePayPalOrderResponse(order.Id, paypalOrderId, approvalUrl);
    }

    public async Task<OrderDetailResponse> CapturePayPalOrderAsync(
        int orderId,
        int sellerId,
        CapturePayPalOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetSellerOrderByIdAsync(orderId, sellerId, cancellationToken);
        if (order is null)
        {
            throw new AppProblemException(404, "ORDER_NOT_FOUND", "Order was not found or does not contain items for this seller.");
        }

        var now = clock.UtcNow;
        if (order.Payment is null)
        {
            order.Payment = new Payment
            {
                OrderId = order.Id,
                UserId = order.BuyerId,
                Amount = order.TotalPrice,
                Method = "PayPal Sandbox Gateway",
                Status = "Completed",
                PaypalOrderId = request.PaypalOrderId,
                PaymentGateway = "PayPal Sandbox Gateway",
                PaidAtUtc = now
            };
        }
        else
        {
            order.Payment.Method = "PayPal Sandbox Gateway";
            order.Payment.Status = "Completed";
            order.Payment.PaypalOrderId = request.PaypalOrderId;
            order.Payment.PaymentGateway = "PayPal Sandbox Gateway";
            order.Payment.PaidAtUtc = now;
        }

        // Automatic warehouse preparation & inventory deduction
        order.Status = "AWAITING_SHIPMENT";
        var (carrier, trackingNumber) = GenerateOfficialEbayTracking(order.Address?.Country ?? "United States", "PayPal");
        if (order.ShippingInfo is null)
        {
            order.ShippingInfo = new ShippingInfo
            {
                OrderId = order.Id,
                Carrier = carrier,
                TrackingNumber = trackingNumber,
                Status = "AWAITING_SHIPMENT",
                EstimatedArrivalUtc = now.AddDays(3)
            };
            await orderRepository.AddShippingInfoAsync(order.ShippingInfo, cancellationToken);
        }
        else
        {
            order.ShippingInfo.Carrier = carrier;
            order.ShippingInfo.TrackingNumber = trackingNumber;
            order.ShippingInfo.Status = "AWAITING_SHIPMENT";
        }

        foreach (var item in order.Items)
        {
            var prod = item.Product;
            if (prod?.Inventory is not null)
            {
                prod.Inventory.Quantity = Math.Max(0, prod.Inventory.Quantity - item.Quantity);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetSellerOrderDetailAsync(order.Id, sellerId, cancellationToken);
    }

    public async Task<CreateVNPayOrderResponse> CreateVNPayOrderAsync(
        int orderId,
        int sellerId,
        CreateVNPayOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetSellerOrderByIdAsync(orderId, sellerId, cancellationToken);
        if (order is null)
        {
            throw new AppProblemException(404, "ORDER_NOT_FOUND", "Order was not found or does not contain items for this seller.");
        }

        var returnUrl = request.ReturnUrl ?? $"https://localhost:7248/orders/{orderId}/vnpay-return";

        var vnpParams = new SortedList<string, string>(new VnPayCompare())
        {
            { "vnp_Version", "2.1.0" },
            { "vnp_Command", "pay" },
            { "vnp_TmnCode", "CGXZLS0Z" },
            { "vnp_Amount", ((long)(order.TotalPrice * 100)).ToString(System.Globalization.CultureInfo.InvariantCulture) },
            { "vnp_CreateDate", DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss") },
            { "vnp_CurrCode", "VND" },
            { "vnp_IpAddr", "127.0.0.1" },
            { "vnp_Locale", "vn" },
            { "vnp_OrderInfo", $"Thanh toan don hang {order.Id}" },
            { "vnp_OrderType", "other" },
            { "vnp_ReturnUrl", returnUrl },
            { "vnp_TxnRef", $"{order.Id}_{DateTime.UtcNow.Ticks}" }
        };

        var query = new System.Text.StringBuilder();
        foreach (var (key, value) in vnpParams)
        {
            if (!string.IsNullOrEmpty(value))
            {
                query.Append(System.Net.WebUtility.UrlEncode(key)).Append('=').Append(System.Net.WebUtility.UrlEncode(value)).Append('&');
            }
        }

        var queryString = query.ToString();
        var rawData = queryString.Remove(queryString.Length - 1, 1);
        var hashSecret = "XNBCJFAKAZQSGTARRLGCHVZWCIOIGSHN";

        using var hmac = new System.Security.Cryptography.HMACSHA512(System.Text.Encoding.UTF8.GetBytes(hashSecret));
        var hashValue = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
        var secureHash = string.Concat(hashValue.Select(b => b.ToString("x2")));
        var paymentUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?{queryString}vnp_SecureHash={secureHash}";

        if (order.Payment is null)
        {
            order.Payment = new Payment
            {
                OrderId = order.Id,
                UserId = order.BuyerId,
                Amount = order.TotalPrice,
                Method = "VNPay Sandbox Gateway",
                Status = "Pending",
                PaymentGateway = "VNPay Sandbox Gateway"
            };
        }
        else
        {
            order.Payment.Method = "VNPay Sandbox Gateway";
            order.Payment.PaymentGateway = "VNPay Sandbox Gateway";
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CreateVNPayOrderResponse(order.Id, paymentUrl);
    }

    public async Task<OrderDetailResponse> CaptureVNPayOrderAsync(
        int orderId,
        int sellerId,
        CaptureVNPayOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetSellerOrderByIdAsync(orderId, sellerId, cancellationToken);
        if (order is null)
        {
            throw new AppProblemException(404, "ORDER_NOT_FOUND", "Order was not found or does not contain items for this seller.");
        }

        var now = clock.UtcNow;
        if (order.Payment is null)
        {
            order.Payment = new Payment
            {
                OrderId = order.Id,
                UserId = order.BuyerId,
                Amount = order.TotalPrice,
                Method = "VNPay Sandbox Gateway",
                Status = "Completed",
                PaymentGateway = "VNPay Sandbox Gateway",
                PaidAtUtc = now
            };
        }
        else
        {
            order.Payment.Method = "VNPay Sandbox Gateway";
            order.Payment.Status = "Completed";
            order.Payment.PaymentGateway = "VNPay Sandbox Gateway";
            order.Payment.PaidAtUtc = now;
        }

        // Automatic warehouse preparation & inventory deduction
        order.Status = "AWAITING_SHIPMENT";
        var (carrier, trackingNumber) = GenerateOfficialEbayTracking(order.Address?.Country ?? "Vietnam", "VNPay");
        if (order.ShippingInfo is null)
        {
            order.ShippingInfo = new ShippingInfo
            {
                OrderId = order.Id,
                Carrier = carrier,
                TrackingNumber = trackingNumber,
                Status = "AWAITING_SHIPMENT",
                EstimatedArrivalUtc = now.AddDays(3)
            };
            await orderRepository.AddShippingInfoAsync(order.ShippingInfo, cancellationToken);
        }
        else
        {
            order.ShippingInfo.Carrier = carrier;
            order.ShippingInfo.TrackingNumber = trackingNumber;
            order.ShippingInfo.Status = "AWAITING_SHIPMENT";
        }

        foreach (var item in order.Items)
        {
            var prod = item.Product;
            if (prod?.Inventory is not null)
            {
                prod.Inventory.Quantity = Math.Max(0, prod.Inventory.Quantity - item.Quantity);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetSellerOrderDetailAsync(order.Id, sellerId, cancellationToken);
    }

    public async Task<OrderDetailResponse> SimulateDeliveryAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetSellerOrderByIdAsync(orderId, sellerId, cancellationToken);
        if (order is null)
        {
            throw new AppProblemException(404, "ORDER_NOT_FOUND", "Order was not found or does not contain items for this seller.");
        }

        var now = clock.UtcNow;
        if (string.Equals(order.Status, "AWAITING_SHIPMENT", StringComparison.OrdinalIgnoreCase))
        {
            order.Status = "SHIPPED";
            if (order.ShippingInfo is not null)
            {
                order.ShippingInfo.Status = "SHIPPED";
            }
        }
        else
        {
            order.Status = "DELIVERED";
            if (order.ShippingInfo is not null)
            {
                order.ShippingInfo.Status = "DELIVERED";
            }

            if (order.Payment is not null)
            {
                order.Payment.Status = "Completed";
                if (!order.Payment.PaidAtUtc.HasValue)
                {
                    order.Payment.PaidAtUtc = now;
                }
            }

            order.PayoutStatus = "On Hold (eMBG Hold)";
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetSellerOrderDetailAsync(order.Id, sellerId, cancellationToken);
    }

    public async Task<OrderDetailResponse> UpdateTrackingStatusAsync(
        int orderId,
        int sellerId,
        string targetStatus,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetSellerOrderByIdAsync(orderId, sellerId, cancellationToken);
        if (order is null)
        {
            throw new AppProblemException(404, "ORDER_NOT_FOUND", "Order was not found or does not contain items for this seller.");
        }

        var normalizedStatus = targetStatus?.Trim().ToUpperInvariant() switch
        {
            "AWAITING_PAYMENT" => "AWAITING_PAYMENT",
            "AWAITING_SHIPMENT" => "AWAITING_SHIPMENT",
            "SHIPPED" => "SHIPPED",
            "DELIVERED" => "DELIVERED",
            "CANCELLED" => "CANCELLED",
            _ => "AWAITING_SHIPMENT"
        };

        int GetStatusRank(string s) => s?.ToUpperInvariant() switch
        {
            "AWAITING_PAYMENT" => 1,
            "AWAITING_SHIPMENT" => 2,
            "SHIPPED" => 3,
            "DELIVERED" => 4,
            "CANCELLED" => 99,
            _ => 0
        };

        var currentRank = GetStatusRank(order.Status);
        var targetRank = GetStatusRank(normalizedStatus);

        if (normalizedStatus != "CANCELLED" && targetRank < currentRank)
        {
            throw new AppProblemException(400, "INVALID_STATUS_TRANSITION", $"Không thể lùi trạng thái đơn hàng từ [{order.Status}] về [{normalizedStatus}]. Tracking chỉ cho phép tiến theo lộ trình vận chuyển.");
        }
        if (order.Status == "DELIVERED" || order.Status == "CANCELLED")
        {
            throw new AppProblemException(400, "ORDER_COMPLETED", $"Đơn hàng đã ở trạng thái kết thúc [{order.Status}], không thể thay đổi thêm.");
        }

        var now = clock.UtcNow;
        order.Status = normalizedStatus;

        if (order.ShippingInfo is not null)
        {
            order.ShippingInfo.Status = normalizedStatus switch
            {
                "AWAITING_PAYMENT" => "PENDING",
                "AWAITING_SHIPMENT" => "PREPARING",
                "SHIPPED" => "SHIPPED",
                "DELIVERED" => "DELIVERED",
                "CANCELLED" => "CANCELLED",
                _ => "PREPARING"
            };
        }

        if (normalizedStatus == "AWAITING_PAYMENT")
        {
            if (order.Payment is not null)
            {
                order.Payment.Status = "Pending";
            }
            order.PayoutStatus = "Pending";
        }
        else if (normalizedStatus == "AWAITING_SHIPMENT")
        {
            if (order.Payment is not null)
            {
                order.Payment.Status = "Completed";
                if (!order.Payment.PaidAtUtc.HasValue)
                {
                    order.Payment.PaidAtUtc = now;
                }
            }
            order.PayoutStatus = "Pending";
        }
        else if (normalizedStatus == "SHIPPED")
        {
            if (order.Payment is not null)
            {
                order.Payment.Status = "Completed";
                if (!order.Payment.PaidAtUtc.HasValue)
                {
                    order.Payment.PaidAtUtc = now;
                }
            }
            order.PayoutStatus = "In Transit";
        }
        else if (normalizedStatus == "DELIVERED")
        {
            if (order.Payment is not null)
            {
                order.Payment.Status = "Completed";
                if (!order.Payment.PaidAtUtc.HasValue)
                {
                    order.Payment.PaidAtUtc = now;
                }
            }
            order.PayoutStatus = "On Hold (eMBG Hold)";
        }
        else if (normalizedStatus == "CANCELLED")
        {
            if (order.Payment is not null)
            {
                order.Payment.Status = "Cancelled";
            }
            order.PayoutStatus = "Cancelled";
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetSellerOrderDetailAsync(order.Id, sellerId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<SellerPayoutSummaryDto>> GetSellerPayoutsAsync(
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        var pagedOrders = await orderRepository.GetSellerOrdersAsync(sellerId, null, 1, 100, cancellationToken);
        return pagedOrders.Items.Select(order =>
        {
            var fee = order.FinalValueFee > 0 ? order.FinalValueFee : Math.Round(order.TotalPrice * 0.10m, 2);
            var net = order.TotalPrice - fee;
            var payoutStatus = order.PayoutStatus ?? (string.Equals(order.Status, "DELIVERED", StringComparison.OrdinalIgnoreCase) ? "On Hold (eMBG Hold)" : "Pending");
            return new SellerPayoutSummaryDto(
                order.Id,
                order.OrderDateUtc,
                order.TotalPrice,
                fee,
                net,
                payoutStatus);
        }).ToList();
    }

    public async Task<int> AutoProgressOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await orderRepository.GetActiveInFlightOrdersAsync(cancellationToken);
        if (orders.Count == 0) return 0;

        var now = clock.UtcNow;
        int progressedCount = 0;

        foreach (var order in orders)
        {
            if (string.Equals(order.Status, "AWAITING_SHIPMENT", StringComparison.OrdinalIgnoreCase))
            {
                order.Status = "SHIPPED";
                if (order.ShippingInfo is not null)
                {
                    order.ShippingInfo.Status = "SHIPPED";
                }
                progressedCount++;
            }
            else if (string.Equals(order.Status, "SHIPPED", StringComparison.OrdinalIgnoreCase))
            {
                order.Status = "DELIVERED";
                if (order.ShippingInfo is not null)
                {
                    order.ShippingInfo.Status = "DELIVERED";
                }

                if (order.Payment is not null)
                {
                    order.Payment.Status = "Completed";
                    if (!order.Payment.PaidAtUtc.HasValue)
                    {
                        order.Payment.PaidAtUtc = now;
                    }
                }

                order.PayoutStatus = "On Hold (eMBG Hold)";
                progressedCount++;
            }
        }

        if (progressedCount > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return progressedCount;
    }

    private static (string Carrier, string TrackingNumber) GenerateOfficialEbayTracking(string country, string paymentMethod)
    {
        var random = Random.Shared;
        if (paymentMethod.Contains("PayPal", StringComparison.OrdinalIgnoreCase))
        {
            // Official eBay International Shipping (eIS) or USPS (eBay US Standard)
            if (random.Next(2) == 0)
            {
                return ("USPS", $"9400111899562{random.NextInt64(100000000, 999999999)}");
            }
            return ("eBay International Shipping (eIS)", $"ESUS{random.Next(10000000, 99999999)}");
        }

        if (string.Equals(country, "Vietnam", StringComparison.OrdinalIgnoreCase) || paymentMethod.Contains("VNPay", StringComparison.OrdinalIgnoreCase) || paymentMethod.Contains("COD", StringComparison.OrdinalIgnoreCase))
        {
            // Official VNPost EMS UPU Standard (eBay Cross-Border CBT Partner)
            return ("VNPost EMS", $"EA{random.Next(100000000, 999999999)}VN");
        }

        // Global FedEx Express (Official eBay Carrier)
        return ("FedEx", $"{random.NextInt64(700000000000, 799999999999)}");
    }
}

public sealed class VnPayCompare : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        var vnpCompare = System.Globalization.CompareInfo.GetCompareInfo("en-US");
        return vnpCompare.Compare(x, y, System.Globalization.CompareOptions.Ordinal);
    }
}

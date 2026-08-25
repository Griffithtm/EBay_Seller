using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;
using CloneEbay.Application.Orders;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Tests;

public sealed class Role4OrderShippingTests
{
    private readonly FixedClock _clock = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Get_seller_orders_calculates_total_price_for_seller_items_only()
    {
        var sellerA = 10;
        var sellerB = 20;

        var productA = new Product { Id = 1, SellerId = sellerA, Title = "Product A", Price = 100, Inventory = new Inventory { Quantity = 10 } };
        var productB = new Product { Id = 2, SellerId = sellerB, Title = "Product B", Price = 300, Inventory = new Inventory { Quantity = 10 } };

        var order = new Order
        {
            Id = 555,
            BuyerId = 1,
            Status = "AWAITING_PAYMENT",
            OrderDateUtc = _clock.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 101, ProductId = 1, Product = productA, Quantity = 2, UnitPrice = 100 },
                new OrderItem { Id = 102, ProductId = 2, Product = productB, Quantity = 1, UnitPrice = 300 }
            }
        };

        var repository = new FakeOrderRepository(order);
        var service = new OrderService(repository, _unitOfWork, _clock);

        var result = await service.GetSellerOrdersAsync(sellerA, null, 1, 20);

        var summary = Assert.Single(result.Items);
        Assert.Equal(555, summary.Id);
        Assert.Equal(200m, summary.TotalPrice); // 2 * 100 = 200, excluding Seller B's 300
    }

    [Fact]
    public async Task Create_manual_order_with_cod_starts_auto_shipped()
    {
        var sellerId = 10;
        var product = new Product
        {
            Id = 1,
            SellerId = sellerId,
            Title = "Manual Product",
            Price = 200m,
            Inventory = new Inventory { Quantity = 10 }
        };

        var productRepo = new FakeProductRepository(product);
        var userRepo = new FakeUserRepository();
        var orderRepo = new FakeOrderRepository();
        var service = new OrderService(orderRepo, productRepo, userRepo, _unitOfWork, _clock);

        var request = new CreateOrderRequest(
            BuyerId: 1,
            BuyerUsername: null,
            BuyerFullName: "Nguyễn Văn B",
            BuyerPhone: "0987654321",
            Country: "Vietnam",
            City: "Hà Nội",
            District: "Quận Cầu Giấy",
            Street: "123 Đường Cầu Giấy",
            ZipCode: "100000",
            PaymentMethod: "Pay Cash on Delivery (COD)",
            Items: [new CreateOrderItemRequest(1, 2, 200m)]);

        var result = await service.CreateManualOrderAsync(sellerId, request);

        Assert.NotNull(result);
        Assert.Equal(400m, result.TotalPrice); // 2 * 200 = 400
        Assert.Equal(40m, result.FinalValueFee); // 10% FVF = 40
        Assert.Equal(360m, result.NetPayoutAmount); // 400 - 40 = 360
        Assert.Equal("AWAITING_SHIPMENT", result.Status); // COD automatically starts in AWAITING_SHIPMENT
    }

    [Fact]
    public async Task Create_manual_order_with_vnpay_and_capture_transitions_status()
    {
        var sellerId = 10;
        var product = new Product
        {
            Id = 1,
            SellerId = sellerId,
            Title = "Manual Product VNPay",
            Price = 500m,
            Inventory = new Inventory { Quantity = 10 }
        };

        var productRepo = new FakeProductRepository(product);
        var userRepo = new FakeUserRepository();
        var orderRepo = new FakeOrderRepository();
        var service = new OrderService(orderRepo, productRepo, userRepo, _unitOfWork, _clock);

        var request = new CreateOrderRequest(
            BuyerId: 1,
            BuyerUsername: null,
            BuyerFullName: "Trần Văn C",
            BuyerPhone: "0912345678",
            Country: "Vietnam",
            City: "TP. Hồ Chí Minh",
            District: "Quận 1",
            Street: "456 Lê Lợi",
            ZipCode: "700000",
            PaymentMethod: "VNPay Sandbox Gateway",
            Items: [new CreateOrderItemRequest(1, 1, 500m)]);

        var result = await service.CreateManualOrderAsync(sellerId, request);
        Assert.Equal("AWAITING_PAYMENT", result.Status);

        var captureResult = await service.CaptureVNPayOrderAsync(result.Id, sellerId, new CaptureVNPayOrderRequest(new Dictionary<string, string> { { "vnp_ResponseCode", "00" } }));
        Assert.Equal("AWAITING_SHIPMENT", captureResult.Status);
        Assert.Equal("Completed", captureResult.Payment?.Status);

        var shippedResult = await service.SimulateDeliveryAsync(result.Id, sellerId);
        Assert.Equal("SHIPPED", shippedResult.Status);

        var deliveredResult = await service.SimulateDeliveryAsync(result.Id, sellerId);
        Assert.Equal("DELIVERED", deliveredResult.Status);
        Assert.Equal("On Hold (eMBG Hold)", deliveredResult.PayoutStatus);
    }

    [Fact]
    public async Task Get_seller_order_detail_hides_items_belonging_to_other_sellers()
    {
        var sellerA = 10;
        var sellerB = 20;

        var productA = new Product { Id = 1, SellerId = sellerA, Title = "Product A", Price = 100, Inventory = new Inventory { Quantity = 10 } };
        var productB = new Product { Id = 2, SellerId = sellerB, Title = "Product B", Price = 300, Inventory = new Inventory { Quantity = 10 } };

        var order = new Order
        {
            Id = 555,
            BuyerId = 1,
            Status = "AWAITING_SHIPMENT",
            OrderDateUtc = _clock.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 101, ProductId = 1, Product = productA, Quantity = 2, UnitPrice = 100 },
                new OrderItem { Id = 102, ProductId = 2, Product = productB, Quantity = 1, UnitPrice = 300 }
            }
        };

        var repository = new FakeOrderRepository(order);
        var service = new OrderService(repository, _unitOfWork, _clock);

        var detail = await service.GetSellerOrderDetailAsync(555, sellerA);

        var item = Assert.Single(detail.OrderItems);
        Assert.Equal(1, item.ProductId);
        Assert.Equal("Product A", item.Title);
    }

    [Fact]
    public async Task Confirm_shipment_sets_status_to_SHIPPED_and_deducts_warehouse_and_inventory()
    {
        var sellerId = 10;
        var product = new Product
        {
            Id = 1,
            SellerId = sellerId,
            Title = "Phone",
            Price = 500,
            Inventory = new Inventory { Quantity = 5 }
        };
        var order = new Order
        {
            Id = 100,
            BuyerId = 1,
            Status = "AWAITING_SHIPMENT",
            OrderDateUtc = _clock.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductId = 1, Product = product, Quantity = 2, UnitPrice = 500 }
            },
            Payment = new Payment { Id = 1, Status = "Completed", Amount = 1000 }
        };

        var repository = new FakeOrderRepository(order);
        var service = new OrderService(repository, _unitOfWork, _clock);

        var result = await service.ConfirmShipmentAsync(100, sellerId, new ConfirmShipmentRequest("TRACK123456", "FEDEX"));

        Assert.Equal("SHIPPED", result.Status);
        Assert.Equal("SHIPPED", order.Status);
        Assert.Equal(3, product.Inventory.Quantity);
        Assert.NotNull(order.ShippingInfo);
        Assert.Equal("FEDEX", order.ShippingInfo.Carrier);
        Assert.Equal("TRACK123456", order.ShippingInfo.TrackingNumber);
    }

    [Fact]
    public async Task Confirm_shipment_fails_when_tracking_info_is_missing()
    {
        var sellerId = 10;
        var product = new Product
        {
            Id = 1,
            SellerId = sellerId,
            Title = "Phone",
            Price = 500,
            Inventory = new Inventory { Quantity = 5 }
        };
        var order = new Order
        {
            Id = 100,
            BuyerId = 1,
            Status = "AWAITING_SHIPMENT",
            OrderDateUtc = _clock.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductId = 1, Product = product, Quantity = 2, UnitPrice = 500 }
            }
        };

        var repository = new FakeOrderRepository(order);
        var service = new OrderService(repository, _unitOfWork, _clock);

        var ex = await Assert.ThrowsAsync<AppProblemException>(() =>
            service.ConfirmShipmentAsync(100, sellerId, new ConfirmShipmentRequest("", "FEDEX")));

        Assert.Equal(422, ex.StatusCode);
        Assert.Equal("TRACKING_INFO_REQUIRED", ex.ErrorCode);
    }

    [Fact]
    public async Task Confirm_shipment_fails_when_warehouse_stock_is_insufficient()
    {
        var sellerId = 10;
        var product = new Product
        {
            Id = 1,
            SellerId = sellerId,
            Title = "Phone",
            Price = 500,
            Inventory = new Inventory { Quantity = 1 }
        };
        var order = new Order
        {
            Id = 100,
            BuyerId = 1,
            Status = "AWAITING_SHIPMENT",
            OrderDateUtc = _clock.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductId = 1, Product = product, Quantity = 2, UnitPrice = 500 }
            }
        };

        var repository = new FakeOrderRepository(order);
        var service = new OrderService(repository, _unitOfWork, _clock);

        var ex = await Assert.ThrowsAsync<AppProblemException>(() =>
            service.ConfirmShipmentAsync(100, sellerId, new ConfirmShipmentRequest("TRACK123", "FEDEX")));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("INSUFFICIENT_STOCK", ex.ErrorCode);
    }

    [Fact]
    public async Task Create_paypal_order_generates_approval_url()
    {
        var sellerId = 10;
        var product = new Product { Id = 1, SellerId = sellerId, Title = "Phone", Price = 500 };
        var order = new Order
        {
            Id = 100,
            BuyerId = 1,
            TotalPrice = 500,
            Status = "AWAITING_PAYMENT",
            OrderDateUtc = _clock.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductId = 1, Product = product, Quantity = 1, UnitPrice = 500 }
            }
        };

        var repository = new FakeOrderRepository(order);
        var service = new OrderService(repository, _unitOfWork, _clock);

        var result = await service.CreatePayPalOrderAsync(100, sellerId, new CreatePayPalOrderRequest(null, null));

        Assert.Equal(100, result.OrderId);
        Assert.StartsWith("https://www.sandbox.paypal.com/checkoutnow", result.ApprovalUrl);
        Assert.NotNull(order.Payment);
        Assert.Equal("PayPal Sandbox", order.Payment.Method);
    }

    [Fact]
    public async Task Capture_paypal_order_completes_payment_and_transitions_to_AWAITING_SHIPMENT()
    {
        var sellerId = 10;
        var product = new Product { Id = 1, SellerId = sellerId, Title = "Phone", Price = 500 };
        var order = new Order
        {
            Id = 100,
            BuyerId = 1,
            TotalPrice = 500,
            Status = "AWAITING_PAYMENT",
            OrderDateUtc = _clock.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductId = 1, Product = product, Quantity = 1, UnitPrice = 500 }
            }
        };

        var repository = new FakeOrderRepository(order);
        var service = new OrderService(repository, _unitOfWork, _clock);

        var result = await service.CapturePayPalOrderAsync(100, sellerId, new CapturePayPalOrderRequest("PAYPAL-12345"));

        Assert.Equal("AWAITING_SHIPMENT", result.Status);
        Assert.Equal("AWAITING_SHIPMENT", order.Status);
        Assert.NotNull(order.Payment);
        Assert.Equal("Completed", order.Payment.Status);
    }

    [Fact]
    public async Task Cancel_order_restores_inventory_and_marks_order_cancelled()
    {
        var sellerId = 10;
        var product = new Product
        {
            Id = 1,
            SellerId = sellerId,
            Title = "Phone",
            Price = 500,
            Inventory = new Inventory { Quantity = 3 }
        };
        var order = new Order
        {
            Id = 100,
            BuyerId = 1,
            Status = "SHIPPED",
            OrderDateUtc = _clock.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductId = 1, Product = product, Quantity = 2, UnitPrice = 500 }
            }
        };

        var repository = new FakeOrderRepository(order);
        var service = new OrderService(repository, _unitOfWork, _clock);

        var result = await service.CancelOrderAsync(100, sellerId);

        Assert.Equal("CANCELLED", result.Status);
        Assert.Equal("CANCELLED", order.Status);
        Assert.Equal(5, product.Inventory.Quantity); // 3 + 2 = 5
    }

    [Fact]
    public async Task Cancel_order_fails_when_order_is_already_delivered()
    {
        var sellerId = 10;
        var product = new Product { Id = 1, SellerId = sellerId, Title = "Phone", Price = 500 };
        var order = new Order
        {
            Id = 100,
            BuyerId = 1,
            Status = "DELIVERED",
            OrderDateUtc = _clock.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductId = 1, Product = product, Quantity = 1, UnitPrice = 500 }
            }
        };

        var repository = new FakeOrderRepository(order);
        var service = new OrderService(repository, _unitOfWork, _clock);

        var ex = await Assert.ThrowsAsync<AppProblemException>(() => service.CancelOrderAsync(100, sellerId));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("CANNOT_CANCEL_DELIVERED", ex.ErrorCode);
    }

    [Fact]
    public async Task Get_seller_payouts_calculates_final_value_fee_and_net_payout()
    {
        var sellerId = 10;
        var product = new Product { Id = 1, SellerId = sellerId, Title = "Laptop", Price = 1000, Inventory = new Inventory { Quantity = 5 } };
        var order = new Order
        {
            Id = 888,
            BuyerId = 1,
            Status = "DELIVERED",
            OrderDateUtc = _clock.UtcNow,
            TotalPrice = 1000m,
            FinalValueFee = 100m,
            PayoutStatus = "Released",
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductId = 1, Product = product, Quantity = 1, UnitPrice = 1000m }
            }
        };

        var repository = new FakeOrderRepository(order);
        var service = new OrderService(repository, _unitOfWork, _clock);

        var payouts = await service.GetSellerPayoutsAsync(sellerId);

        var payout = Assert.Single(payouts);
        Assert.Equal(888, payout.OrderId);
        Assert.Equal(1000m, payout.TotalPrice);
        Assert.Equal(100m, payout.FinalValueFee); // 10% FVF
        Assert.Equal(900m, payout.NetPayoutAmount); // 1000 - 100
        Assert.Equal("Released", payout.PayoutStatus);
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        private readonly List<Order> _orders;

        public FakeOrderRepository(params Order[] orders)
        {
            _orders = orders.ToList();
        }

        public Task<PagedResult<Order>> GetSellerOrdersAsync(
            int sellerId,
            string? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _orders
                .Where(o => o.Items.Any(i => i.Product.SellerId == sellerId));

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var totalItems = query.Count();
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new PagedResult<Order>(items, page, pageSize, totalItems));
        }

        public Task<Order?> GetSellerOrderByIdAsync(int orderId, int sellerId, CancellationToken cancellationToken = default)
        {
            var order = _orders.FirstOrDefault(o => o.Id == orderId && o.Items.Any(i => i.Product.SellerId == sellerId));
            return Task.FromResult(order);
        }

        public Task AddOrderAsync(Order order, CancellationToken cancellationToken = default)
        {
            if (order.Id == 0) order.Id = _orders.Count + 1000;
            _orders.Add(order);
            return Task.CompletedTask;
        }

        public Task AddShippingInfoAsync(ShippingInfo shippingInfo, CancellationToken cancellationToken = default)
        {
            var order = _orders.FirstOrDefault(o => o.Id == shippingInfo.OrderId);
            if (order is not null)
            {
                order.ShippingInfo = shippingInfo;
            }
            return Task.CompletedTask;
        }

        public Task<ShippingInfo?> GetShippingInfoByOrderIdAsync(int orderId, int sellerId, CancellationToken cancellationToken = default)
        {
            var order = _orders.FirstOrDefault(o => o.Id == orderId && o.Items.Any(i => i.Product.SellerId == sellerId));
            return Task.FromResult(order?.ShippingInfo);
        }

        public Task<Payment?> GetPaymentByOrderIdAsync(int orderId, int sellerId, CancellationToken cancellationToken = default)
        {
            var order = _orders.FirstOrDefault(o => o.Id == orderId && o.Items.Any(i => i.Product.SellerId == sellerId));
            return Task.FromResult(order?.Payment);
        }

        public Task<IReadOnlyList<Order>> GetActiveInFlightOrdersAsync(CancellationToken cancellationToken = default)
        {
            var list = _orders.Where(o => o.Status == "AWAITING_SHIPMENT" || o.Status == "SHIPPED").ToList();
            return Task.FromResult<IReadOnlyList<Order>>(list);
        }
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 8, 24, 10, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeProductRepository(Product product) : IProductRepository
    {
        public Task AddAsync(Product value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddBidAsync(Bid bid, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddOrderAsync(Order order, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<Order?> GetAuctionOrderAsync(int productId, CancellationToken cancellationToken = default) => Task.FromResult<Order?>(null);
        public Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Category>>([]);
        public Task<IReadOnlyList<int>> GetExpiredAuctionIdsAsync(DateTime utcNow, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<int>>([]);
        public Task<Product?> GetByIdAsync(int productId, bool tracked, CancellationToken cancellationToken = default) => Task.FromResult<Product?>(product.Id == productId ? product : null);
        public Task<Product?> GetByIdForUpdateAsync(int productId, CancellationToken cancellationToken = default) => Task.FromResult<Product?>(product.Id == productId ? product : null);
        public Task<bool> HasOrderItemsAsync(int productId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<(IReadOnlyList<Product> Items, int TotalItems)> ListAsync(int? sellerId, ProductStatus? status, int? categoryId, string? keyword, bool includeHidden, string? sortBy, bool descending, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<Product>)[product], 1));
        public void Remove(Product value) { }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
    }
}

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
            Status = "Pending",
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
            Status = "Pending",
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
    public async Task Confirm_order_updates_status_and_deducts_inventory()
    {
        var sellerId = 10;
        var product = new Product { Id = 1, SellerId = sellerId, Title = "Phone", Price = 500, Inventory = new Inventory { Quantity = 5 } };
        var order = new Order
        {
            Id = 100,
            BuyerId = 1,
            Status = "Pending",
            OrderDateUtc = _clock.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductId = 1, Product = product, Quantity = 2, UnitPrice = 500 }
            },
            Payment = new Payment { Id = 1, Status = "Completed", Amount = 1000 }
        };

        var repository = new FakeOrderRepository(order);
        var service = new OrderService(repository, _unitOfWork, _clock);

        var result = await service.ConfirmOrderAsync(100, sellerId);

        Assert.Equal("Confirmed", result.Status);
        Assert.Equal("Confirmed", order.Status);
        Assert.Equal(3, product.Inventory.Quantity);
    }

    [Fact]
    public async Task Confirm_order_fails_when_payment_is_not_completed()
    {
        var sellerId = 10;
        var product = new Product { Id = 1, SellerId = sellerId, Title = "Phone", Price = 500, Inventory = new Inventory { Quantity = 5 } };
        var order = new Order
        {
            Id = 100,
            BuyerId = 1,
            Status = "Pending",
            OrderDateUtc = _clock.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductId = 1, Product = product, Quantity = 2, UnitPrice = 500 }
            },
            Payment = new Payment { Id = 1, Status = "Pending", Amount = 1000 }
        };

        var repository = new FakeOrderRepository(order);
        var service = new OrderService(repository, _unitOfWork, _clock);

        var ex = await Assert.ThrowsAsync<AppProblemException>(() => service.ConfirmOrderAsync(100, sellerId));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("PAYMENT_NOT_COMPLETED", ex.ErrorCode);
        Assert.Equal("Pending", order.Status);
        Assert.Equal(5, product.Inventory.Quantity);
    }

    [Fact]
    public async Task Confirm_order_fails_when_inventory_is_insufficient()
    {
        var sellerId = 10;
        var product = new Product { Id = 1, SellerId = sellerId, Title = "Phone", Price = 500, Inventory = new Inventory { Quantity = 1 } };
        var order = new Order
        {
            Id = 100,
            BuyerId = 1,
            Status = "Pending",
            OrderDateUtc = _clock.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductId = 1, Product = product, Quantity = 2, UnitPrice = 500 }
            },
            Payment = new Payment { Id = 1, Status = "Completed", Amount = 1000 }
        };

        var repository = new FakeOrderRepository(order);
        var service = new OrderService(repository, _unitOfWork, _clock);

        var ex = await Assert.ThrowsAsync<AppProblemException>(() => service.ConfirmOrderAsync(100, sellerId));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("INSUFFICIENT_INVENTORY", ex.ErrorCode);
        Assert.Equal("Pending", order.Status);
        Assert.Equal(1, product.Inventory.Quantity);
    }

    [Fact]
    public async Task Create_shipping_label_sets_LabelCreated_and_keeps_order_status_Confirmed()
    {
        var sellerId = 10;
        var product = new Product { Id = 1, SellerId = sellerId, Title = "Phone", Price = 500, Inventory = new Inventory { Quantity = 5 } };
        var order = new Order
        {
            Id = 100,
            BuyerId = 1,
            Status = "Confirmed",
            OrderDateUtc = _clock.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductId = 1, Product = product, Quantity = 1, UnitPrice = 500 }
            }
        };

        var repository = new FakeOrderRepository(order);
        var service = new OrderService(repository, _unitOfWork, _clock);

        var result = await service.CreateShippingLabelAsync(100, sellerId, new CreateShippingLabelRequest("GHN"));

        Assert.Equal("LabelCreated", result.Status);
        Assert.Equal("GHN", result.Carrier);
        Assert.StartsWith("GHN-", result.TrackingNumber);
        Assert.Equal("Confirmed", order.Status);
        Assert.NotNull(order.ShippingInfo);
        Assert.Equal("LabelCreated", order.ShippingInfo.Status);
    }

    [Fact]
    public async Task Create_shipping_label_fails_if_order_is_not_confirmed()
    {
        var sellerId = 10;
        var product = new Product { Id = 1, SellerId = sellerId, Title = "Phone", Price = 500, Inventory = new Inventory { Quantity = 5 } };
        var order = new Order
        {
            Id = 100,
            BuyerId = 1,
            Status = "Pending",
            OrderDateUtc = _clock.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductId = 1, Product = product, Quantity = 1, UnitPrice = 500 }
            }
        };

        var repository = new FakeOrderRepository(order);
        var service = new OrderService(repository, _unitOfWork, _clock);

        var ex = await Assert.ThrowsAsync<AppProblemException>(() =>
            service.CreateShippingLabelAsync(100, sellerId, new CreateShippingLabelRequest("GHN")));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("ORDER_NOT_CONFIRMED", ex.ErrorCode);
    }

    [Fact]
    public async Task Update_shipping_status_to_HandedToCarrier_changes_order_status_to_Shipped()
    {
        var sellerId = 10;
        var product = new Product { Id = 1, SellerId = sellerId, Title = "Phone", Price = 500, Inventory = new Inventory { Quantity = 5 } };
        var order = new Order
        {
            Id = 100,
            BuyerId = 1,
            Status = "Confirmed",
            OrderDateUtc = _clock.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductId = 1, Product = product, Quantity = 1, UnitPrice = 500 }
            },
            ShippingInfo = new ShippingInfo { Id = 1, Carrier = "GHN", TrackingNumber = "GHN-100", Status = "LabelCreated" }
        };

        var repository = new FakeOrderRepository(order);
        var service = new OrderService(repository, _unitOfWork, _clock);

        var result = await service.UpdateShippingStatusAsync(100, sellerId, new UpdateShippingStatusRequest("HandedToCarrier"));

        Assert.Equal("Shipped", result.Status);
        Assert.Equal("Shipped", order.Status);
        Assert.Equal("HandedToCarrier", order.ShippingInfo.Status);
    }

    [Fact]
    public async Task Update_shipping_status_fails_on_invalid_transition()
    {
        var sellerId = 10;
        var product = new Product { Id = 1, SellerId = sellerId, Title = "Phone", Price = 500, Inventory = new Inventory { Quantity = 5 } };
        var order = new Order
        {
            Id = 100,
            BuyerId = 1,
            Status = "Delivered",
            OrderDateUtc = _clock.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductId = 1, Product = product, Quantity = 1, UnitPrice = 500 }
            },
            ShippingInfo = new ShippingInfo { Id = 1, Carrier = "GHN", TrackingNumber = "GHN-100", Status = "Delivered" }
        };

        var repository = new FakeOrderRepository(order);
        var service = new OrderService(repository, _unitOfWork, _clock);

        var ex = await Assert.ThrowsAsync<AppProblemException>(() =>
            service.UpdateShippingStatusAsync(100, sellerId, new UpdateShippingStatusRequest("Preparing")));

        Assert.Equal(422, ex.StatusCode);
        Assert.Equal("INVALID_STATUS_TRANSITION", ex.ErrorCode);
        Assert.Equal("Delivered", order.Status);
        Assert.Equal("Delivered", order.ShippingInfo.Status);
    }

    private sealed class FakeOrderRepository(params Order[] orders) : IOrderRepository
    {
        private readonly List<Order> _orders = orders.ToList();

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

        public Task<PagedResult<Order>> GetUserOrdersAsync(int userId, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _orders.Where(o => o.BuyerId == userId || o.Items.Any(i => i.Product.SellerId == userId));
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status);
            return Task.FromResult(new PagedResult<Order>(query.Skip((page - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, query.Count()));
        }

        public Task<Order?> GetUserOrderByIdAsync(int orderId, int userId, CancellationToken cancellationToken = default)
        {
            var order = _orders.FirstOrDefault(o => o.Id == orderId && (o.BuyerId == userId || o.Items.Any(i => i.Product.SellerId == userId)));
            return Task.FromResult(order);
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

        public Task CreateMockOrderAsync(int sellerId, DateTime now, CancellationToken cancellationToken = default)
        {
            var newId = _orders.Count > 0 ? _orders.Max(o => o.Id) + 1 : 1;
            var order = new Order
            {
                Id = newId,
                BuyerId = 999,
                OrderDateUtc = now,
                TotalPrice = 100,
                Status = "Pending",
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 1, UnitPrice = 100, Product = new Product { SellerId = sellerId, Title = "Test Product" } }
                },
                Payment = new Payment { Amount = 100, Method = "VNPay", Status = "Completed", PaidAtUtc = now }
            };
            _orders.Add(order);
            return Task.CompletedTask;
        }

        public Task<Order> CreateManualOrderAsync(int sellerId, CreateManualOrderRequest request, DateTime now, CancellationToken cancellationToken = default)
        {
            var newId = _orders.Count > 0 ? _orders.Max(o => o.Id) + 1 : 1;
            var order = new Order
            {
                Id = newId,
                BuyerId = 999,
                OrderDateUtc = now,
                TotalPrice = request.Quantity * request.UnitPrice,
                Status = "Pending",
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = request.ProductId, Quantity = request.Quantity, UnitPrice = request.UnitPrice, Product = new Product { SellerId = sellerId, Title = "Test Product" } }
                },
                Payment = new Payment { Amount = request.Quantity * request.UnitPrice, Method = request.PaymentMethod, Status = request.PaymentStatus, PaidAtUtc = now }
            };
            _orders.Add(order);
            return Task.FromResult(order);
        }
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 8, 24, 10, 0, 0, DateTimeKind.Utc);
    }
}

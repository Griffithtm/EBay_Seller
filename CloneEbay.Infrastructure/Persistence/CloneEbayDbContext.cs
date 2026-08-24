using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Domain.Entities;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CloneEbay.Infrastructure.Persistence;

public sealed class CloneEbayDbContext(DbContextOptions<CloneEbayDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ShippingInfo> ShippingInfos => Set<ShippingInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CloneEbayDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public async Task<IAppTransaction> BeginSerializableTransactionAsync(CancellationToken cancellationToken = default) =>
        new EfAppTransaction(await Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken));

    private sealed class EfAppTransaction(IDbContextTransaction transaction) : IAppTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}

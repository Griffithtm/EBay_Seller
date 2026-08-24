using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Security;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Products;
using CloneEbay.Infrastructure.Persistence;
using CloneEbay.Infrastructure.Persistence.Repositories;
using CloneEbay.Infrastructure.RateLimiting;
using CloneEbay.Infrastructure.Security;
using CloneEbay.Infrastructure.Time;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CloneEbay.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        var sqlConnection = new SqlConnectionStringBuilder(connectionString)
        {
            MultipleActiveResultSets = false
        }.ConnectionString;
        services.AddDbContext<CloneEbayDbContext>(options => options.UseSqlServer(sqlConnection));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<CloneEbayDbContext>());
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IClock, SystemClock>();
        if (bool.TryParse(configuration["Redis:Enabled"], out var redisEnabled) && redisEnabled)
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var redisConnection = configuration["Redis:ConnectionString"]
                    ?? throw new InvalidOperationException("Redis:ConnectionString is required when Redis is enabled.");
                var options = ConfigurationOptions.Parse(redisConnection);
                options.AbortOnConnectFail = false;
                options.ConnectRetry = 3;
                return ConnectionMultiplexer.Connect(options);
            });
            services.AddSingleton<IProductCreateRateLimiter, RedisProductCreateRateLimiter>();
        }
        else
        {
            services.AddSingleton<IProductCreateRateLimiter, InMemoryProductCreateRateLimiter>();
        }
        return services;
    }
}

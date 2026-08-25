using System.Text;
using System.Text.Json.Serialization;
using CloneEbay.API.Authorization;
using CloneEbay.API.Health;
using CloneEbay.API.HostedServices;
using CloneEbay.API.Middleware;
using CloneEbay.API.Models;
using CloneEbay.API.Security;
using CloneEbay.API.Storage;
using CloneEbay.Application.Abstractions.Security;
using CloneEbay.Application.Auth;
using CloneEbay.Application.Dashboard;
using CloneEbay.Application.Sellers;
using CloneEbay.Application.Stores;
using CloneEbay.Application.Products;
using CloneEbay.Application.Coupons;
using CloneEbay.Application.Orders;
using CloneEbay.Application.Reviews;
using CloneEbay.Application.Returns;
using CloneEbay.Application.Disputes;
using CloneEbay.Application.Messages;
using CloneEbay.API.Hubs;
using CloneEbay.Infrastructure;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "CloneEbay.API")
    .WriteTo.Console(new CompactJsonFormatter()));

var dataProtectionPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, ".data-protection");
Directory.CreateDirectory(dataProtectionPath);
builder.Services.AddDataProtection()
    .SetApplicationName("CloneEbay.API")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());
        return new BadRequestObjectResult(ApiEnvelope<object>.Failure(
            "VALIDATION_ERROR",
            "One or more validation errors occurred.",
            context.HttpContext.TraceIdentifier,
            errors));
    };
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISellerService, SellerService>();
builder.Services.AddScoped<ISellerPerformanceService, SellerPerformanceService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IReturnService, ReturnService>();
builder.Services.AddScoped<IDisputeService, DisputeService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddSignalR();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.Issuer), "JWT issuer is required.")
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.Audience), "JWT audience is required.")
    .Validate(settings => settings.Key.Length >= 32, "JWT key must contain at least 32 characters.")
    .ValidateOnStart();

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings are missing.");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (string.IsNullOrEmpty(token))
                {
                    token = context.Request.Cookies["cloneebay.api.access"];
                }
                if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/messages"))
                    context.Token = token;
                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(ApiEnvelope<object>.Failure(
                    "UNAUTHORIZED",
                    "A valid access token is required.",
                    context.HttpContext.TraceIdentifier));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                var sellerNotVerified = context.HttpContext.User.IsInRole("Seller");
                await context.Response.WriteAsJsonAsync(ApiEnvelope<object>.Failure(
                    sellerNotVerified ? "SELLER_NOT_VERIFIED" : "FORBIDDEN",
                    sellerNotVerified
                        ? "Only approved sellers can perform this action."
                        : "You do not have permission to perform this action.",
                    context.HttpContext.TraceIdentifier));
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.VerifiedSeller, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Seller");
        policy.AddRequirements(new VerifiedSellerRequirement());
    });
});
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthorizationHandler, VerifiedSellerAuthorizationHandler>();
builder.Services.AddSingleton<IStoreBannerStorage, LocalStoreBannerStorage>();
builder.Services.AddSingleton<IProductImageStorage, LocalProductImageStorage>();
builder.Services.AddHostedService<AuctionClosingService>();
builder.Services.AddHostedService<SellerPerformanceEvaluationService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length > 0)
        {
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
    });
});

var healthChecks = builder.Services.AddHealthChecks()
    .AddCheck<SelfHealthCheck>("self", tags: ["live", "startup"])
    .AddCheck<DatabaseHealthCheck>("sqlserver", tags: ["ready"]);
if (builder.Configuration.GetValue<bool>("Redis:Enabled"))
{
    healthChecks.AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CloneEbay Seller API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ApiExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("WebClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MessageHub>("/hubs/messages");
app.MapHealthChecks("/health/live", new() { Predicate = registration => registration.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new() { Predicate = registration => registration.Tags.Contains("ready") });
app.MapHealthChecks("/health/startup", new() { Predicate = registration => registration.Tags.Contains("startup") });

var migrateOnly = args.Any(argument => string.Equals(argument, "--migrate-only", StringComparison.OrdinalIgnoreCase));
if (migrateOnly || builder.Configuration.GetValue<bool>("Database:ApplyMigrationsAtStartup"))
{
    await DatabaseInitializer.InitializeAsync(app.Services, builder.Configuration, seedData: !migrateOnly);
}

if (migrateOnly) return;
app.Run();

public partial class Program;

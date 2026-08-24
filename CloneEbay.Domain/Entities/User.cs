using CloneEbay.Domain.Common;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Domain.Entities;

public sealed class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Buyer;
    public string? AvatarUrl { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsActive { get; set; } = true;
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Store? Store { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    public ICollection<Product> WonAuctions { get; set; } = new List<Product>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
}

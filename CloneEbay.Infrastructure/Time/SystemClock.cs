using CloneEbay.Application.Abstractions.Time;

namespace CloneEbay.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

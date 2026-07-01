using VSky.Application.Common.Interfaces;

namespace VSky.Infrastructure.Services;

/// <summary>System-clock implementation of <see cref="IDateTimeProvider"/>.</summary>
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

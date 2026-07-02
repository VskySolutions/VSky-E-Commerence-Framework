using VSky.Domain.Common;
using VSky.Domain.Enums;

namespace VSky.Domain.Entities;

/// <summary>
/// A time-limited, single-use token backing email verification and password reset (REQ-CUS-001).
/// Stored hashed; validated by matching the hash, checking <see cref="ExpiresOnUtc"/>, and ensuring
/// it has not already been consumed.
/// </summary>
public class UserToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public UserTokenPurpose Purpose { get; set; }
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedOnUtc { get; set; }
    public DateTime ExpiresOnUtc { get; set; }
    public DateTime? ConsumedOnUtc { get; set; }

    public bool IsActive => ConsumedOnUtc is null && DateTime.UtcNow < ExpiresOnUtc;
}

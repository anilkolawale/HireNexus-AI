using ATS.Domain.Common;
using ATS.Domain.Enums;

namespace ATS.Domain.Entities;

public class User : AuditableEntity
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ProfileImageUrl { get; set; }

    // Brute-force protection: incremented on each failed login, reset on success.
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedOutUntilUtc { get; set; }

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = default!;

    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

public class Role : BaseEntity
{
    public UserRoleType Type { get; set; }
    public string Name { get; set; } = default!;
    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class Permission : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!; // e.g. "jobs.create"
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}

public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByToken { get; set; }
    public bool IsActive => RevokedAtUtc == null && DateTime.UtcNow < ExpiresAtUtc;

    // Captured at login for the session-management UI ("Chrome on Windows, last used...").
    // Best-effort only — IP/user-agent are self-reported by the client and not proof of
    // identity, so these are for the user's own awareness, not a security control by themselves.
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime LastUsedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
}

public class PasswordResetToken : BaseEntity
{
    public string Token { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UsedAtUtc { get; set; }
    public bool IsActive => UsedAtUtc == null && DateTime.UtcNow < ExpiresAtUtc;

    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
}

public class EmailVerificationToken : BaseEntity
{
    public string Token { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAtUtc { get; set; }
    public bool IsActive => VerifiedAtUtc == null && DateTime.UtcNow < ExpiresAtUtc;

    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
}

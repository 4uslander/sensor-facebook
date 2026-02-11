using SensorFacebook.Domain.Enums;
using System;
using System.Collections.Generic;
using static System.Collections.Specialized.BitVector32;

namespace SensorFacebook.Infrastructure.Entities;

public partial class FbAccount
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;
    public string? DisplayName { get; set; }

    public int? ProxyGroupId { get; set; }
    public int? PreferredProxyGroupId { get; set; }

    public string? ProfileDir { get; set; }
    public string? EncryptedCookie { get; set; }

    public int? CheckpointCount { get; set; }
    public DateTimeOffset? LastCheckpoint { get; set; }

    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public AccountStatus Status { get; set; }

    // >>> NEW
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset? CooldownUntil { get; set; }

    public virtual ProxyGroup? ProxyGroup { get; set; }
    public virtual ProxyGroup? PreferredProxyGroup { get; set; }

    public virtual User? CreatedByNavigation { get; set; }
    public virtual ICollection<AccountEvent> AccountEvents { get; set; } = new List<AccountEvent>();
    public virtual ICollection<SearchJob> SearchJobs { get; set; } = new List<SearchJob>();
    public virtual ICollection<Listing> Listings { get; set; } = new List<Listing>();
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}

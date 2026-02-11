using System;
using System.Collections.Generic;

namespace SensorFacebook.Infrastructure.Entities;

public partial class Session
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }
    public int? ProxyGroupId { get; set; }
    public string? ConsumerKey { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public string? Note { get; set; }

    public virtual FbAccount Account { get; set; } = null!;
}
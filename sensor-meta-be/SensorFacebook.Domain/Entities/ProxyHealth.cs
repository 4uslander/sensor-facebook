using System;
using System.Collections.Generic;

namespace SensorFacebook.Infrastructure.Entities;

public partial class ProxyHealth
{
    public Guid Id { get; set; }

    public int? ProxyGroupId { get; set; }

    public bool? Healthy { get; set; }

    public int? LatencyMs { get; set; }

    public string? LastStatus { get; set; }

    public DateTime? CheckedAt { get; set; }

    public virtual ProxyGroup? ProxyGroup { get; set; }
}

using System;
using System.Collections.Generic;

namespace SensorFacebook.Infrastructure.Entities;

public partial class Subscription
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public int? KeywordId { get; set; }

    public string? NotifyChannel { get; set; }

    public bool? Active { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Keyword? Keyword { get; set; }

    public virtual User? User { get; set; }
}

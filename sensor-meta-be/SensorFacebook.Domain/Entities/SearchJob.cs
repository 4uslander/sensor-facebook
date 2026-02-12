using SensorFacebook.Domain.Enums;
using System;
using System.Collections.Generic;

namespace SensorFacebook.Infrastructure.Entities;

public partial class SearchJob
{
    public Guid Id { get; set; }

    public int? KeywordId { get; set; }

    public Guid? AccountId { get; set; }

    public int? ProxyGroupId { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public int? ResultCount { get; set; }

    public string? ErrorMessage { get; set; }

    public int? Attempts { get; set; }

    public DateTime? LastErrorAt { get; set; }

    public long? TimeBucket180s { get; set; }

    public JobStatus Status { get; set; } = JobStatus.queued;

    public virtual FbAccount? Account { get; set; }

    public virtual Keyword? Keyword { get; set; }

    public virtual ICollection<ListingChange> ListingChanges { get; set; } = new List<ListingChange>();

    public virtual ProxyGroup? ProxyGroup { get; set; }
}

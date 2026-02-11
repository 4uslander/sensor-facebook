using System;
using System.Collections.Generic;

namespace SensorFacebook.Infrastructure.Entities;

public partial class ListingChange
{
    public Guid Id { get; set; }

    public Guid? ListingId { get; set; }

    public string ChangeType { get; set; } = null!;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime? OccurredAt { get; set; }

    public Guid? DetectedByJob { get; set; }

    public virtual SearchJob? DetectedByJobNavigation { get; set; }

    public virtual Listing? Listing { get; set; }
}

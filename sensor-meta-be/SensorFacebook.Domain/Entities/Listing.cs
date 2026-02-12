using System;
using System.Collections.Generic;

namespace SensorFacebook.Infrastructure.Entities;

public partial class Listing
{
    public Guid Id { get; set; }

    public string ExternalId { get; set; } = null!;

    public int? KeywordId { get; set; }

    public Guid? AccountId { get; set; }

    public string? Title { get; set; }

    public decimal? Price { get; set; }

    public string? Currency { get; set; }

    public string? Location { get; set; }

    public string? Condition { get; set; }

    public string? Payload { get; set; }

    public DateTime? FirstSeen { get; set; }

    public DateTime? LastSeen { get; set; }

    public bool? IsActive { get; set; }

    public virtual FbAccount? Account { get; set; }

    public virtual Keyword? Keyword { get; set; }

    public virtual ICollection<ListingChange> ListingChanges { get; set; } = new List<ListingChange>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

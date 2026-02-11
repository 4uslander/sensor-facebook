using SensorFacebook.Domain.Enums;
using System;
using System.Collections.Generic;

namespace SensorFacebook.Infrastructure.Entities;

public partial class Keyword
{
    public int Id { get; set; }

    public string Text { get; set; } = null!;

    public int? CategoryId { get; set; }

    public int? Priority { get; set; }

    public bool? Active { get; set; }

    public DateTime? NextRun { get; set; }

    public DateTime? CreatedAt { get; set; }

    public decimal? LocationLat { get; set; }

    public decimal? LocationLon { get; set; }

    public int? RadiusKm { get; set; }
    public string? RadiusPolicy { get; set; }

    public string? SortBy { get; set; }     
    // USER-DEFINED
    public string[]? Conditions { get; set; }  
    // ARRAY
    public string? ListedTime { get; set; }
    // USER-DEFINED
    public string? Availability { get; set; }
    // USER-DEFINED

    public virtual Category? Category { get; set; }

    public virtual ICollection<Listing> Listings { get; set; } = new List<Listing>();

    public virtual ICollection<SearchJob> SearchJobs { get; set; } = new List<SearchJob>();

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}

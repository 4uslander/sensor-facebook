using System;
using System.Collections.Generic;

namespace SensorFacebook.Infrastructure.Entities;

public partial class Notification
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public Guid? ListingId { get; set; }

    public string? Channel { get; set; }

    public string? Message { get; set; }

    public DateTime? SentAt { get; set; }

    public string? RuleName { get; set; }

    public string? Context { get; set; }

    public virtual Listing? Listing { get; set; }

    public virtual User? User { get; set; }
}

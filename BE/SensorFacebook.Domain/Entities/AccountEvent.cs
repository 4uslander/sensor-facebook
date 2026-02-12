using System;
using System.Collections.Generic;

namespace SensorFacebook.Infrastructure.Entities;

public partial class AccountEvent
{
    public Guid Id { get; set; }

    public Guid? AccountId { get; set; }

    public string EventType { get; set; } = null!;

    public string? Payload { get; set; }

    public DateTime? OccurredAt { get; set; }

    public virtual FbAccount? Account { get; set; }
}

using System;
using System.Collections.Generic;

namespace SensorFacebook.Infrastructure.Entities;

public partial class AuditLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string? Details { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}

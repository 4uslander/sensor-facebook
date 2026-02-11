using System;
using System.Collections.Generic;

namespace SensorFacebook.Infrastructure.Entities;

public partial class DeveloperApp
{
    public Guid Id { get; set; }

    public string AppId { get; set; } = null!;

    public string? AppName { get; set; }

    public string? CertId { get; set; }

    public string? TokenEncrypted { get; set; }

    public string? Status { get; set; }

    public int? Weight { get; set; }

    public DateTime? LastChecked { get; set; }

    public Guid? OwnerId { get; set; }

    public virtual User? Owner { get; set; }
}

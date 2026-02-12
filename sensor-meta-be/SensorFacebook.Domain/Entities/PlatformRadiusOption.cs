using System;
using System.Collections.Generic;

namespace SensorFacebook.Infrastructure.Entities;

public partial class PlatformRadiusOption
{
    public int Id { get; set; }

    public string Platform { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public int Value { get; set; }

    public bool? Active { get; set; }

    public int? SortOrder { get; set; }
}

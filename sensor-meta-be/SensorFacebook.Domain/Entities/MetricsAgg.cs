using System;
using System.Collections.Generic;

namespace SensorFacebook.Infrastructure.Entities;

public partial class MetricsAgg
{
    public Guid Id { get; set; }

    public string MetricName { get; set; } = null!;

    public string? Labels { get; set; }

    public decimal? Value { get; set; }

    public DateTime? Timestamp { get; set; }
}

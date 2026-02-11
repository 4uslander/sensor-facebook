using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.ProxyGroups
{
    public sealed record ProxyHealthDto(
     Guid Id,
     int ProxyGroupId,
     bool Healthy,
     int? LatencyMs,
     string? LastStatus,
     DateTimeOffset CheckedAt
 );
}

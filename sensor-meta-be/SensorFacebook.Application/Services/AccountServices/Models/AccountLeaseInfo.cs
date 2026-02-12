using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.AccountServices.Models
{
    public sealed record AccountLeaseInfo(
    Guid SessionId,
    Guid AccountId,
    int? ProxyGroupId,
    string Email,
    string? DisplayName
);
}

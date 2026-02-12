using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.AccountServices.Models
{
    public sealed record AccountLease(
    Guid SessionId,
    Guid AccountId,
    int ProxyGroupId,
    DateTimeOffset ExpiresAt
);
}

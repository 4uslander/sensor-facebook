using SensorFacebook.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.AccountServices.Models
{
    public sealed record FbAccountDto(
    Guid Id,
    string Email,
    string? DisplayName,
    int? ProxyGroupId,
    string? ProxyGroupName,
    string? Region,
    AccountStatus Status,
    int CheckpointCount,
    DateTimeOffset? LastCheckpoint,
    Guid? CreatedBy,
    DateTimeOffset? CreatedAt,
    bool HasCookie,
    string? ProfileDir
);
}

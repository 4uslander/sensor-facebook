using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.AccountServices.Models
{
    public sealed record AccountEventDto(
    Guid Id,
    Guid AccountId,
    string EventType,         // checkpoint, captcha, login_success, banned, ...
    string? Payload,
    DateTime? OccurredAt
);
}

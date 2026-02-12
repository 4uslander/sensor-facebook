using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.SearchJobServices
{
    public sealed record JobListItemDto(
    Guid Id, int? KeywordId, string Status, int Attempts,
    DateTimeOffset? ScheduledAt, DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt);

    public sealed record JobDetailDto(
        Guid Id, int? KeywordId, string Status, int Attempts, int ResultCount,
        string? ErrorMessage, DateTimeOffset? ScheduledAt, DateTimeOffset? StartedAt,
        DateTimeOffset? FinishedAt, DateTimeOffset? LastErrorAt);
}

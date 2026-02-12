using Hangfire.Storage.Monitoring;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.SearchJobServices
{
    public interface ISearchJobService
    {
        Task<Guid> RunNowAsync(
            int keywordId,
            string priority,
            int proxyGroupId,
            Guid? accountId,
            CancellationToken ct = default);
        Task<JobDetailDto?> GetAsync(Guid jobId, CancellationToken ct = default);
        Task<(IReadOnlyList<JobListItemDto> items, int total)> ListAsync(
            string? status, int? keywordId, DateTimeOffset? from, DateTimeOffset? to,
            int page, int pageSize, CancellationToken ct = default);
        Task<bool> RetryAsync(Guid jobId, CancellationToken ct = default);
        Task<bool> CancelAsync(Guid jobId, CancellationToken ct = default);
        Task<(IReadOnlyList<JobListItemDto> items, int total)> ListFailedAsync(
            int? keywordId, int page, int pageSize, CancellationToken ct = default);
    }
}

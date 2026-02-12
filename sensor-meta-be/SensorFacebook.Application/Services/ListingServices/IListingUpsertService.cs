using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.ListingServices
{
    public interface IListingUpsertService
    {
        Task<int> UpsertAsync(
            SearchExecutor.SearchItem it,
            int? keywordId,
            Guid? accountId,
            int? proxyGroupId,
            DateTimeOffset now,
            CancellationToken ct = default);
    }
}

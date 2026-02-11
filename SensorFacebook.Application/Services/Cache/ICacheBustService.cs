using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.Cache
{
    public interface ICacheBustService
    {
        Task BustCategoryListCacheAsync(CancellationToken ct = default);
        Task BustKeywordListCacheAsync(CancellationToken ct = default);
        Task BustProxyListCacheAsync(CancellationToken ct = default);

        // Optionally public generic
        Task BustByPrefixAsync(string prefixWithoutInstance, CancellationToken ct = default);
    }
}

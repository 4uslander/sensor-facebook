using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.Cache
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
        Task SetAsync<T>(string key, T value, TimeSpan? absolute = null, TimeSpan? sliding = null, CancellationToken ct = default);
        Task RemoveAsync(string key, CancellationToken ct = default);
        string BuildKey(params string[] parts);
    }
}

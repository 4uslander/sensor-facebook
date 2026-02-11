using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SensorFacebook.Application.Services.Cache
{
    public sealed class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public RedisCacheService(IDistributedCache cache) => _cache = cache;

        public string BuildKey(params string[] parts)
            => string.Join(':', parts).ToLowerInvariant();

        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            var bytes = await _cache.GetAsync(key, ct);
            if (bytes is null) return default;
            return JsonSerializer.Deserialize<T>(bytes, _json);
        }

        public async Task SetAsync<T>(
            string key, T value,
            TimeSpan? absolute = null, TimeSpan? sliding = null,
            CancellationToken ct = default)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _json);
            var opts = new DistributedCacheEntryOptions();
            if (absolute is not null) opts.AbsoluteExpirationRelativeToNow = absolute;
            if (sliding is not null) opts.SlidingExpiration = sliding;
            await _cache.SetAsync(key, bytes, opts, ct);
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
            => _cache.RemoveAsync(key, ct);
    }
}

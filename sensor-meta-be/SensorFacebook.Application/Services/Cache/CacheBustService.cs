using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace SensorFacebook.Application.Services.Cache
{
    public sealed class CacheBustService : ICacheBustService
    {
        private readonly IConnectionMultiplexer _mux;
        private readonly string _instanceName;
        private readonly int _db;

        public CacheBustService(
            IConnectionMultiplexer mux,
            IConfiguration configuration,
            IOptions<Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions> cacheOptions // to read InstanceName
            )
        {
            _mux = mux; 
            _instanceName = cacheOptions.Value.InstanceName ?? string.Empty;

            // Resolve target database index
            // Try parse from "Redis:Configuration" defaultDatabase=...
            var cfg = configuration["Redis:Configuration"] ?? "";
            _db = ParseDefaultDb(cfg) ?? 0;
        }

        public Task BustCategoryListCacheAsync(CancellationToken ct = default)
            => BustByPrefixAsync("cat:list:v1:", ct);

        public Task BustKeywordListCacheAsync(CancellationToken ct = default)
            => BustByPrefixAsync("kw:list:v2:", ct);

        public Task BustProxyListCacheAsync(CancellationToken ct = default)
            => BustByPrefixAsync("proxy:list:v1:", ct);

        public async Task BustByPrefixAsync(string prefixWithoutInstance, CancellationToken ct = default)
        {
            var db = _mux.GetDatabase(_db);
            var fullPrefix = _instanceName + prefixWithoutInstance;   // e.g. "sensor:" + "kw:list:v2:"
            var pattern = fullPrefix + "*";

            foreach (var ep in _mux.GetEndPoints())
            {
                var server = _mux.GetServer(ep);
                if (!server.IsConnected) continue;

                // Dùng SCAN để tránh block Redis
                var batch = db.CreateBatch();
                var tasks = server.KeysAsync(database: _db, pattern: pattern, pageSize: 512);
                await foreach (var key in tasks.WithCancellation(ct))
                {
                    // xoá bằng batch để nhanh hơn
                    _ = db.KeyDeleteAsync(key);
                }
                batch.Execute();
            }
        }

        private static int? ParseDefaultDb(string configuration)
        {
            
            var parts = configuration.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var p in parts)
            {
                if (p.StartsWith("defaultDatabase=", StringComparison.OrdinalIgnoreCase))
                {
                    var v = p.Substring("defaultDatabase=".Length);
                    if (int.TryParse(v, out var i)) return i;
                }
            }
            return null;
        }
    }
}

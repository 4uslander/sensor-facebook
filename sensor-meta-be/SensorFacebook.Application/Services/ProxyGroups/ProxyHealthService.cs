using Microsoft.EntityFrameworkCore;
using SensorFacebook.Application.Services.AccountServices.Security;
using SensorFacebook.Infrastructure.Entities;
using SensorFacebook.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace SensorFacebook.Application.Services.ProxyGroups
{
    public sealed class ProxyHealthService : IProxyHealthService
    {
        private readonly SensorDbContext _db;
        private readonly ICookieCryptoService _crypto;

        public ProxyHealthService(SensorDbContext db, ICookieCryptoService crypto)
        {
            _db = db;
            _crypto = crypto;
        }

        public async Task<ProxyHealthDto?> GetLatestAsync(int proxyGroupId, CancellationToken ct = default)
        {
            var h = await _db.ProxyHealths.AsNoTracking()
                .Where(x => x.ProxyGroupId == proxyGroupId)
                .OrderByDescending(x => x.CheckedAt)
                .FirstOrDefaultAsync(ct);

            if (h is null) return null;

            // ProxyGroupId/CheckedAt trong entity là nullable => cần guard
            return new ProxyHealthDto(
                h.Id,
                h.ProxyGroupId ?? 0,
                h.Healthy ?? true,
                h.LatencyMs,
                h.LastStatus,
                h.CheckedAt.HasValue ? (DateTimeOffset)h.CheckedAt.Value : DateTimeOffset.UtcNow
            );
        }

        public async Task<ProxyHealthDto> CheckNowAsync(int proxyGroupId, int timeoutMs = 8000, CancellationToken ct = default)
        {
            var pg = await _db.ProxyGroups.FirstOrDefaultAsync(x => x.Id == proxyGroupId, ct)
                     ?? throw new ArgumentException("ProxyGroup not found");

            // ✅ dùng endpoint mới thay ProxyUrl
            if (string.IsNullOrWhiteSpace(pg.Protocol) ||
                string.IsNullOrWhiteSpace(pg.Host) ||
                pg.Port is null)
                throw new InvalidOperationException("Proxy endpoint is incomplete (protocol/host/port)");

            // Nếu proxy bị disabled thì chặn check (tuỳ policy bạn; nếu vẫn muốn check thì bỏ đoạn này)
            if (string.Equals(pg.Status, ProxyStatus.Disabled, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Proxy is disabled");

            var resultHealthy = false;
            int? latency = null;
            string lastStatus = "INIT";

            try
            {
                var (handler, client) = BuildHttpClient(pg, timeoutMs, _crypto);
                using (handler)
                using (client)
                {
                    var sw = Stopwatch.StartNew();

                    // HEAD nhanh tới facebook (có thể đổi sang endpoint nhẹ hơn nếu cần)
                    using var req = new HttpRequestMessage(HttpMethod.Head, "https://www.facebook.com/");
                    var resp = await client.SendAsync(req, ct);

                    sw.Stop();
                    latency = (int)sw.ElapsedMilliseconds;

                    resultHealthy = resp.IsSuccessStatusCode;
                    lastStatus = $"HTTP {(int)resp.StatusCode}";
                }
            }
            catch (TaskCanceledException)
            {
                latency = null;
                resultHealthy = false;
                lastStatus = "TIMEOUT";
            }
            catch (HttpRequestException ex)
            {
                lastStatus = "HTTP_ERROR: " + ex.Message;
                resultHealthy = false;
            }
            catch (Exception ex)
            {
                lastStatus = ex.GetType().Name;
                resultHealthy = false;
            }

            // ghi health row
            var now = DateTime.UtcNow;
            var row = new ProxyHealth
            {
                ProxyGroupId = proxyGroupId,
                Healthy = resultHealthy,
                LatencyMs = latency,
                LastStatus = lastStatus,
                CheckedAt = now
            };
            _db.ProxyHealths.Add(row);

            // ✅ update stats + timestamps
            pg.LastChecked = now;
            if (resultHealthy)
            {
                pg.LastOkAt = now;
                pg.SuccessCount = (pg.SuccessCount ?? 0) + 1;
            }
            else
            {
                pg.FailCount = (pg.FailCount ?? 0) + 1;
            }

            // ✅ status theo rule mới: active / degraded / disabled
            // Rule gợi ý:
            // - Healthy => active
            // - Unhealthy => degraded (không set error/checking nữa)
            // - disabled do user set tay
            if (!string.Equals(pg.Status, ProxyStatus.Disabled, StringComparison.OrdinalIgnoreCase))
            {
                pg.Status = resultHealthy ? ProxyStatus.Active : ProxyStatus.Degraded;
            }

            await _db.SaveChangesAsync(ct);

            return new ProxyHealthDto(
                row.Id,
                proxyGroupId,
                resultHealthy,
                latency,
                lastStatus,
                (DateTimeOffset)now
            );
        }

        private static (HttpMessageHandler handler, HttpClient client) BuildHttpClient(
            Infrastructure.Entities.ProxyGroup pg,
            int timeoutMs,
            ICookieCryptoService crypto)
        {
            // Build proxy URI: protocol://host:port
            // NOTE: WebProxy chỉ hỗ trợ http/https proxy tốt nhất.
            // socks4/socks5 không native trong HttpClientHandler => nếu bạn dùng socks, cần lib khác (SharpSocks / SocksSharp / proxied socket).
            var scheme = pg.Protocol!.Trim().ToLowerInvariant();

            if (scheme is "socks4" or "socks5")
                throw new NotSupportedException("HttpClientHandler does not support SOCKS proxies natively. Use an alternative SOCKS implementation.");

            var proxyUri = new Uri($"{scheme}://{pg.Host}:{pg.Port}");

            var webProxy = new WebProxy(proxyUri);

            // Nếu có auth => set credential
            if (!string.IsNullOrWhiteSpace(pg.AuthUsername) && !string.IsNullOrWhiteSpace(pg.AuthPasswordEnc))
            {
                var passPlain = crypto.Decrypt(pg.AuthPasswordEnc);
                webProxy.Credentials = new NetworkCredential(pg.AuthUsername, passPlain);
            }

            var handler = new HttpClientHandler
            {
                Proxy = webProxy,
                UseProxy = true,
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(timeoutMs)
            };

            return (handler, client);
        }
    }
}
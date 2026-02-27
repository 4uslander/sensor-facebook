using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using SensorFacebook.Application.Services.AccountServices.Security;
using SensorFacebook.Application.Services.BrowserPool;
using SensorFacebook.Infrastructure.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SensorFacebook.Browser
{
    public sealed class PlaywrightBrowserPool : IBrowserPool, IAsyncDisposable
    {
        private readonly PlaywrightOptions _opt;
        private readonly ILogger<PlaywrightBrowserPool> _log;
        private readonly SensorDbContext _db;
        private readonly ICookieCryptoService _cookieCrypto;

        private IPlaywright? _pw;

        private static readonly JsonSerializerOptions _cookieJson = new(JsonSerializerDefaults.Web);

        private readonly ConcurrentDictionary<Guid, IBrowserContext> _contexts = new();
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

        // cache browsers theo proxy key
        private readonly ConcurrentDictionary<string, IBrowser> _browsers = new();

        // gates theo proxyGroup
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _pgGates = new();

        public PlaywrightBrowserPool(
            IOptions<PlaywrightOptions> opt,
            ILogger<PlaywrightBrowserPool> log,
            SensorDbContext db,
            ICookieCryptoService cookieCrypto)
        {
            _opt = opt.Value;
            _log = log;
            _db = db;
            _cookieCrypto = cookieCrypto;
        }

        public async Task<IBrowserLease> AcquireAsync(Guid? accountId, int? proxyGroupId, CancellationToken ct = default)
        {
            await EnsurePlaywrightAsync();

            if (accountId is null)
            {
                // Anonymous
                var pgGate = await EnterProxyGroupGateAsync(proxyGroupId, ct);
                var ctx = await CreateContextCoreAsync(null, proxyGroupId, ct);
                return new Lease(this, ctx, null, proxyGroupId, gateHeld: null, pgGateHeld: pgGate);
            }

            var id = accountId.Value;
            var gate = _locks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(ct);

            SemaphoreSlim? pgGate2 = null;
            try
            {
                if (!_contexts.TryGetValue(id, out var ctx))
                {
                    pgGate2 = await EnterProxyGroupGateAsync(proxyGroupId, ct);
                    ctx = await CreateContextCoreAsync(id, proxyGroupId, ct);
                    _contexts[id] = ctx;
                }

                // context đã tồn tại => không enter pg gate nữa (slot đã giữ từ trước)
                return new Lease(this, ctx, id, proxyGroupId, gateHeld: gate, pgGateHeld: pgGate2);
            }
            catch
            {
                gate.Release();
                pgGate2?.Release();
                throw;
            }
        }

        private async Task EnsurePlaywrightAsync()
        {
            if (_pw is null)
                _pw = await Microsoft.Playwright.Playwright.CreateAsync();
        }

        private async Task<IBrowserContext> CreateContextCoreAsync(Guid? accountId, int? proxyGroupId, CancellationToken ct)
        {
            var (browserKey, proxyOptions) = await BuildProxyForGroupAsync(proxyGroupId, ct);
            var browser = await GetOrCreateBrowserForProxyAsync(browserKey, proxyOptions, ct);

            var ctx = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                BypassCSP = true,
                IgnoreHTTPSErrors = true,
                ViewportSize = new() { Width = 1280, Height = 800 }
            });

            ctx.SetDefaultTimeout(_opt.ContextTimeoutMs);

            if (accountId is not null)
                await LoadCookiesAsync(ctx, accountId.Value, ct);

            return ctx;
        }

        // NOTE: hàm này hiện không còn được gọi (bạn đã dùng CreateContextCoreAsync),
        // nhưng để lại nếu muốn dùng lại flow enter gate trong tương lai.
        private async Task<IBrowserContext> CreateContextAsync(Guid? accountId, int? proxyGroupId, CancellationToken ct)
        {
            var pgGate = await EnterProxyGroupGateAsync(proxyGroupId, ct);

            try
            {
                var (browserKey, proxyOptions) = await BuildProxyForGroupAsync(proxyGroupId, ct);
                var browser = await GetOrCreateBrowserForProxyAsync(browserKey, proxyOptions, ct);

                var ctx = await browser.NewContextAsync(new BrowserNewContextOptions
                {
                    BypassCSP = true,
                    IgnoreHTTPSErrors = true,
                    ViewportSize = new() { Width = 1280, Height = 800 }
                });
                ctx.SetDefaultTimeout(_opt.ContextTimeoutMs);

                if (accountId is not null)
                    await LoadCookiesAsync(ctx, accountId.Value, ct);

                return ctx;
            }
            catch
            {
                pgGate?.Release();
                throw;
            }
        }

        private async Task<(string key, Proxy? proxy)> BuildProxyForGroupAsync(int? proxyGroupId, CancellationToken ct)
        {
            if (proxyGroupId is null)
                return ("__NOPROXY__", null);

            var pg = await _db.ProxyGroups
                .AsNoTracking()
                .Where(x => x.Id == proxyGroupId.Value)
                .Select(x => new
                {
                    x.Id,
                    x.Protocol,
                    x.Host,
                    x.Port,
                    x.AuthUsername,
                    x.AuthPasswordEnc
                })
                .FirstOrDefaultAsync(ct);

            if (pg is null || string.IsNullOrWhiteSpace(pg.Protocol) || string.IsNullOrWhiteSpace(pg.Host) || pg.Port is null)
                return ($"PG:{proxyGroupId.Value}:INVALID", null);

            var server = $"{pg.Protocol}://{pg.Host}:{pg.Port}";
            string? username = null;
            string? password = null;

            if (!string.IsNullOrWhiteSpace(pg.AuthUsername) && !string.IsNullOrWhiteSpace(pg.AuthPasswordEnc))
            {
                username = pg.AuthUsername.Trim();
                password = _cookieCrypto.Decrypt(pg.AuthPasswordEnc);
            }

            var proxy = new Proxy
            {
                Server = server,
                Username = string.IsNullOrWhiteSpace(username) ? null : username,
                Password = string.IsNullOrWhiteSpace(password) ? null : password
            };

            var hasAuth = (proxy.Username is not null && proxy.Password is not null) ? "auth" : "noauth";
            var key = $"PG:{pg.Id}:{server}:{hasAuth}";
            return (key, proxy);
        }

        private async Task<int> GetMaxConcurrencyAsync(int proxyGroupId, CancellationToken ct)
        {
            var max = await _db.ProxyGroups.AsNoTracking()
                .Where(x => x.Id == proxyGroupId)
                .Select(x => (int?)x.MaxConcurrency)
                .FirstOrDefaultAsync(ct);

            return (max is null || max <= 0) ? 3 : max.Value;
        }

        private async Task<SemaphoreSlim?> EnterProxyGroupGateAsync(int? proxyGroupId, CancellationToken ct)
        {
            if (proxyGroupId is null) return null;

            var pgId = proxyGroupId.Value;
            var max = await GetMaxConcurrencyAsync(pgId, ct);

            var gate = _pgGates.GetOrAdd(pgId, _ => new SemaphoreSlim(max, max));
            await gate.WaitAsync(ct);
            return gate;
        }

        private async Task<IBrowser> GetOrCreateBrowserForProxyAsync(string browserKey, Proxy? proxy, CancellationToken ct)
        {
            await EnsurePlaywrightAsync();

            if (_browsers.TryGetValue(browserKey, out var existed))
                return existed;

            var attempts = 0;
            while (true)
            {
                attempts++;
                try
                {
                    var launch = new BrowserTypeLaunchOptions
                    {
                        Headless = _opt.Headless
                    };

                    if (!string.IsNullOrWhiteSpace(_opt.ExecutablePath))
                        launch.ExecutablePath = _opt.ExecutablePath;

                    if (proxy is not null)
                        launch.Proxy = proxy;

                    var browser = await _pw!.Chromium.LaunchAsync(launch);

                    browser.Disconnected += (_, __) =>
                    {
                        _browsers.TryRemove(browserKey, out IBrowser? removed);
                    };

                    _browsers[browserKey] = browser;
                    return browser;
                }
                catch when (attempts < 3)
                {
                    await Task.Delay(500 * attempts, ct);
                }
            }
        }

        private async Task LoadCookiesAsync(IBrowserContext ctx, Guid accountId, CancellationToken ct)
        {
            var acc = await _db.FbAccounts.AsNoTracking()
                .Where(a => a.Id == accountId)
                .Select(a => new { a.EncryptedCookie })
                .FirstOrDefaultAsync(ct);

            if (acc is null || string.IsNullOrWhiteSpace(acc.EncryptedCookie))
            {
                _log.LogWarning("LoadCookies: no cookie in DB. account={AccountId}", accountId);
                return;
            }

            string cookieJson;
            try
            {
                cookieJson = _cookieCrypto.Decrypt(acc.EncryptedCookie!);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "LoadCookies: decrypt failed. account={AccountId}", accountId);
                return;
            }

            if (string.IsNullOrWhiteSpace(cookieJson))
            {
                _log.LogWarning("LoadCookies: decrypted cookie empty. account={AccountId}", accountId);
                return;
            }

            List<CookieDto> dto;
            try
            {
                dto = JsonSerializer.Deserialize<List<CookieDto>>(cookieJson, _cookieJson) ?? new();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "LoadCookies: invalid JSON (not array). account={AccountId}", accountId);
                return;
            }

            var ok = new List<Microsoft.Playwright.Cookie>(dto.Count);
            var bad = 0;

            foreach (var c in dto)
            {
                var name = c.Name?.Trim();
                var value = c.Value;
                var domain = c.Domain?.Trim();

                if (string.IsNullOrWhiteSpace(name) || value is null || string.IsNullOrWhiteSpace(domain))
                {
                    bad++;
                    continue;
                }

                var path = string.IsNullOrWhiteSpace(c.Path) ? "/" : c.Path!.Trim();

                ok.Add(new Microsoft.Playwright.Cookie
                {
                    Name = name,
                    Value = value,
                    Domain = domain,
                    Path = path,
                    Expires = c.Expires,
                    HttpOnly = c.HttpOnly ?? false,
                    Secure = c.Secure ?? true,
                    SameSite = c.SameSite
                });
            }

            _log.LogInformation("LoadCookies: account={AccountId} total={Total} ok={Ok} bad={Bad}",
                accountId, dto.Count, ok.Count, bad);

            if (ok.Count == 0)
            {
                _log.LogError("LoadCookies: all cookies invalid. account={AccountId}", accountId);
                return;
            }

            await ctx.AddCookiesAsync(ok);
        }

        private sealed class Lease : IBrowserLease
        {
            private readonly SemaphoreSlim? _gate;
            private readonly SemaphoreSlim? _pgGate;

            public IBrowserContext Context { get; }
            public Guid? AccountId { get; }
            public int? ProxyGroupId { get; }

            public Lease(
                PlaywrightBrowserPool owner,
                IBrowserContext ctx,
                Guid? accountId,
                int? proxyGroupId,
                SemaphoreSlim? gateHeld = null,
                SemaphoreSlim? pgGateHeld = null)
            {
                Context = ctx;
                AccountId = accountId;
                ProxyGroupId = proxyGroupId;
                _gate = gateHeld;
                _pgGate = pgGateHeld;
            }

            public async Task<object> NewPageAsync(CancellationToken ct = default)
                => await Context.NewPageAsync();

            public async ValueTask DisposeAsync()
            {
                if (AccountId is null)
                {
                    try { await Context.CloseAsync(); } catch { }
                    try { await Context.DisposeAsync(); } catch { }
                }

                _gate?.Release();
                _pgGate?.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var kv in _contexts)
            {
                try { await kv.Value.CloseAsync(); } catch { }
                try { await kv.Value.DisposeAsync(); } catch { }
            }
            _contexts.Clear();

            foreach (var b in _browsers.Values)
            {
                try { await b.CloseAsync(); } catch { }
            }
            _browsers.Clear();

            _pw?.Dispose();
        }

        private sealed class CookieDto
        {
            public string? Name { get; set; }
            public string? Value { get; set; }
            public string? Domain { get; set; }
            public string? Path { get; set; }
            public float? Expires { get; set; }
            public bool? HttpOnly { get; set; }
            public bool? Secure { get; set; }
            public Microsoft.Playwright.SameSiteAttribute? SameSite { get; set; }
        }
    }
}
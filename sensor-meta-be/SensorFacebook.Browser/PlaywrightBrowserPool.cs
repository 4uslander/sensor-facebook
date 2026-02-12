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
        private IBrowser? _browser;

        private readonly ConcurrentDictionary<Guid, IBrowserContext> _contexts = new();
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();
        private readonly ConcurrentDictionary<string, IBrowser> _browsers = new();

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
            await EnsureBrowserAsync(ct);

            if (accountId is null)
            {
                // Anonymous
                // ✱ Lưu ý: CreateContextAsync giờ đã Enter PG gate rồi, cần nhận gate để release.
                var pgGate = await EnterProxyGroupGateAsync(proxyGroupId, ct);
                var ctx = await CreateContextCoreAsync(null, proxyGroupId, ct); // tách core không enter gate lần 2
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
                    // Enter PG gate trước khi tạo context cho account
                    pgGate2 = await EnterProxyGroupGateAsync(proxyGroupId, ct);
                    ctx = await CreateContextCoreAsync(id, proxyGroupId, ct);
                    _contexts[id] = ctx;
                }
                // Nếu context đã tồn tại, không Enter gate nữa (đã chiếm slot từ trước)
                return new Lease(this, ctx, id, proxyGroupId, gateHeld: gate, pgGateHeld: pgGate2);
            }
            catch
            {
                gate.Release();
                // Nếu thất bại sau khi Acquire gate PG, nhớ release
                pgGate2?.Release();
                throw;
            }
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

        private async Task EnsureBrowserAsync(CancellationToken ct)
        {
            if (_browser is not null) return;

            _pw ??= await Microsoft.Playwright.Playwright.CreateAsync();
            var launchOpts = new BrowserTypeLaunchOptions
            {
                Headless = _opt.Headless
            };
            if (!string.IsNullOrWhiteSpace(_opt.ExecutablePath))
                launchOpts.ExecutablePath = _opt.ExecutablePath;

            // Lưu ý: proxy per-context không hỗ trợ khi Launch thường.
            // Nếu cần proxy riêng theo ProxyGroup, bạn sẽ cần 1 browser/nhóm proxy (hoặc persistent context).
            _browser = await _pw.Chromium.LaunchAsync(launchOpts);
        }

        private async Task<IBrowserContext> CreateContextAsync(Guid? accountId, int? proxyGroupId, CancellationToken ct)
        {
            // ❶ Vào “cổng” của PG để giới hạn concurency
            var pgGate = await EnterProxyGroupGateAsync(proxyGroupId, ct);

            // ❷ Build proxy cho PG + lấy browser theo key
            var (browserKey, proxyOptions) = await BuildProxyForGroupAsync(proxyGroupId, ct);
            var browser = await GetOrCreateBrowserForProxyAsync(browserKey, proxyOptions, ct);

            // ❸ Tạo context
            var ctx = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                BypassCSP = true,
                IgnoreHTTPSErrors = true,
                ViewportSize = new() { Width = 1280, Height = 800 }
            });
            ctx.SetDefaultTimeout(_opt.ContextTimeoutMs);

            // ❹ Nạp cookie nếu có
            if (accountId is not null)
                await LoadCookiesAsync(ctx, accountId.Value, ct);

            // ❺ Đính gate vào context để Lease có thể release
            // Cách nhẹ: dùng BrowserContext.StorageState để “treo” thông tin thì không hợp lý.
            // Ta sẽ truyền gate qua ctor của Lease khi tạo Lease ở AcquireAsync (xem bước 3).

            return ctx;
        }

        private async Task<(string key, Proxy? proxy)> BuildProxyForGroupAsync(int? proxyGroupId, CancellationToken ct)
        {
            if (proxyGroupId is null)
            {
                // Không proxy
                return ("__NOPROXY__", null);
            }

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
            {
                // PG không hợp lệ => coi như no-proxy, nhưng key gắn PG để tránh trộn lẫn
                return ($"PG:{proxyGroupId.Value}:INVALID", null);
            }

            var server = $"{pg.Protocol}://{pg.Host}:{pg.Port}";
            string? username = null;
            string? password = null;

            if (!string.IsNullOrWhiteSpace(pg.AuthUsername) && !string.IsNullOrWhiteSpace(pg.AuthPasswordEnc))
            {
                username = pg.AuthUsername.Trim();
                // giải mã AES-GCM tái dụng từ cookie service
                password = _cookieCrypto.Decrypt(pg.AuthPasswordEnc);
            }

            var proxy = new Proxy
            {
                Server = server,
                Username = string.IsNullOrWhiteSpace(username) ? null : username,
                Password = string.IsNullOrWhiteSpace(password) ? null : password
            };

            // Key cache browser: gắn PG + endpoint + có/không auth
            var hasAuth = (proxy.Username is not null && proxy.Password is not null) ? "auth" : "noauth";
            var key = $"PG:{pg.Id}:{server}:{hasAuth}";
            return (key, proxy);
        }
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _pgGates = new();

        private async Task<int> GetMaxConcurrencyAsync(int proxyGroupId, CancellationToken ct)
        {
            var max = await _db.ProxyGroups.AsNoTracking()
                .Where(x => x.Id == proxyGroupId)
                .Select(x => (int?)x.MaxConcurrency)
                .FirstOrDefaultAsync(ct);

            // fallback an toàn
            return (max is null || max <= 0) ? 3 : max.Value;
        }

        private async Task<SemaphoreSlim?> EnterProxyGroupGateAsync(int? proxyGroupId, CancellationToken ct)
        {
            if (proxyGroupId is null) return null;

            var pgId = proxyGroupId.Value;
            var max = await GetMaxConcurrencyAsync(pgId, ct);

            var gate = _pgGates.GetOrAdd(pgId, _ => new SemaphoreSlim(max, max));

            // Nếu MaxConcurrency vừa được đổi ở DB (ví dụ từ 3 -> 5),
            // lần tới nên “nới” capacity. SemaphoreSlim không đổi capacity động,
            // nên cách đơn giản: nếu max > CurrentCount + số đang giữ, tạm bỏ qua.
            // (Giải pháp chuẩn: rebuild semaphore khi phát hiện thay đổi lớn.)
            await gate.WaitAsync(ct);
            return gate;
        }

        private async Task LoadCookiesAsync(IBrowserContext ctx, Guid accountId, CancellationToken ct)
        {
            var acc = await _db.FbAccounts.AsNoTracking()
                .Where(a => a.Id == accountId)
                .Select(a => new { a.EncryptedCookie })
                .FirstOrDefaultAsync(ct);

            if (acc is null || string.IsNullOrWhiteSpace(acc.EncryptedCookie)) return;

            var cookieJson = _cookieCrypto.Decrypt(acc.EncryptedCookie!);
            var dto = JsonSerializer.Deserialize<List<CookieDto>>(cookieJson) ?? new();

            var pwCookies = dto.Select(c => new Microsoft.Playwright.Cookie
            {
                Name = c.Name,
                Value = c.Value,
                Domain = c.Domain,
                Path = string.IsNullOrEmpty(c.Path) ? "/" : c.Path,
                Expires = c.Expires,
                HttpOnly = c.HttpOnly,
                Secure = c.Secure,
                SameSite = c.SameSite
            });

            await ctx.AddCookiesAsync(pwCookies);
        }

        private async Task EnsurePlaywrightAsync()
        {
            if (_pw is null) _pw = await Microsoft.Playwright.Playwright.CreateAsync();
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
                        launch.Proxy = proxy; // đã gồm Server/Username/Password

                    var browser = await _pw!.Chromium.LaunchAsync(launch);
                    browser.Disconnected += (_, __) => _browsers.TryRemove(browserKey, out IBrowser _);
                    _browsers[browserKey] = browser;
                    return browser;
                }
                catch when (attempts < 3)
                {
                    await Task.Delay(500 * attempts, ct);
                }
            }
        }

        private sealed class Lease : IBrowserLease
        {
            private readonly PlaywrightBrowserPool _owner;
            private readonly SemaphoreSlim? _gate;     // per-account
            private readonly SemaphoreSlim? _pgGate;   // per-proxy-group
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
                _owner = owner;
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
                // Anonymous => đóng context
                if (AccountId is null)
                {
                    try { await Context.CloseAsync(); } catch { }
                    Context.DisposeAsync();
                }

                // Release locks
                _gate?.Release();
                _pgGate?.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            // đóng contexts reuse
            foreach (var kv in _contexts)
            {
                try { await kv.Value.CloseAsync(); } catch { }
                kv.Value.DisposeAsync();
            }
            _contexts.Clear();

            // đóng browsers theo PG
            foreach (var b in _browsers.Values)
            {
                try { await b.CloseAsync(); } catch { }
            }
            _browsers.Clear();

            // playwright
            _pw?.Dispose();

            // _pgGates để mặc — GC sẽ thu. Không cần Release “bơm” thêm permit.
        }

        // DTO cookie để deserialize JSON xuất từ trình duyệt
        private sealed class CookieDto
        {
            public string Name { get; set; } = default!;
            public string Value { get; set; } = default!;
            public string Domain { get; set; } = default!;
            public string? Path { get; set; }
            public float? Expires { get; set; }
            public bool HttpOnly { get; set; }
            public bool Secure { get; set; }
            public Microsoft.Playwright.SameSiteAttribute? SameSite { get; set; }
        }
    }
}

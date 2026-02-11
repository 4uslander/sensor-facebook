using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using SensorFacebook.Application.Services.AccountServices;
using SensorFacebook.Application.Services.AccountServices.Models;
using SensorFacebook.Application.Services.AccountServices.Security;
using SensorFacebook.Application.Services.BrowserPool;
using SensorFacebook.Domain.Enums;
using SensorFacebook.Infrastructure.Models;
using SensorFacebook.Shared.Messaging;
using SensorFacebook.Worker.Messaging;
using System.Text.Json;

namespace SensorFacebook.Worker.Handlers
{
    public sealed class AccountLoginHandler : IMessageHandler<AccountLoginMsg>
    {
        private readonly IBrowserPool _pool;
        private readonly IAccountSelector _selector;
        private readonly SensorDbContext _db;
        private readonly ICookieCryptoService _crypto;
        private readonly ILogger<AccountLoginHandler> _log;

        public AccountLoginHandler(
            IBrowserPool pool,
            IAccountSelector selector,
            SensorDbContext db,
            ICookieCryptoService crypto,
            ILogger<AccountLoginHandler> log)
        {
            _pool = pool; _selector = selector; _db = db; _crypto = crypto; _log = log;
        }

        public async Task HandleAsync(AccountLoginMsg msg, CancellationToken ct)
        {
            AccountLease? lease = null;
            PeriodicTimer? hbTimer = null;
            CancellationTokenSource? hbCts = null;

            try
            {

                lease = await _selector.AcquireByAccountAsync(
                    accountId: msg.AccountId,
                    proxyGroupId: (int)msg.ProxyGroupId,
                    consumerKey: "worker.login",
                    ttl: TimeSpan.FromMinutes(10),
                    ct: ct);

                if (lease is null) return;

                // Heartbeat: gia hạn mỗi 5 phút trong quá trình login (nếu cần)
                hbTimer = new PeriodicTimer(TimeSpan.FromMinutes(5));
                hbCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (await hbTimer.WaitForNextTickAsync(hbCts.Token))
                        {
                            await _selector.RenewAsync(lease.SessionId, null, hbCts.Token);
                        }
                    }
                    catch { /* cancelled */ }
                }, hbCts.Token);

                // Mượn trình duyệt theo account + proxy đã khóa bởi selector
                await using var brLease = await _pool.AcquireAsync(lease.AccountId, lease.ProxyGroupId, ct);
                var page = (IPage)await brLease.NewPageAsync(ct);

                await page.GotoAsync("https://www.facebook.com/login", new() { Timeout = 45000 });

                // TODO: điền form / 2FA / checkpoint flow…
                // Nếu bạn đã có cookie hợp lệ thì có thể bỏ qua login form.

                // Lưu cookie sau khi đăng nhập thành công
                var cookies = await page.Context.CookiesAsync(new[] { "https://www.facebook.com" });
                var json = JsonSerializer.Serialize(cookies);

                var acc = await _db.FbAccounts.FirstAsync(a => a.Id == msg.AccountId, ct);
                acc.EncryptedCookie = _crypto.Encrypt(json);
                acc.Status = AccountStatus.Active;
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Account login failed for {AccountId}", msg.AccountId);
                // Nếu phát hiện checkpoint ở đây, có thể release với checkpoint:true để bật cooldown:
                // if (IsCheckpoint(ex)) await _selector.ReleaseAsync(lease!.SessionId, checkpoint: true, note: "checkpoint", ct);
            }
            finally
            {
                try { hbCts?.Cancel(); hbTimer?.Dispose(); } catch { }

                if (lease is not null)
                {
                    // bình thường: checkpoint=false; nếu có checkpoint thì đặt true ở catch
                    try { await _selector.ReleaseAsync(lease.SessionId, checkpoint: false, note: "login-done", ct); }
                    catch { /* ignore */ }
                }
            }
        }
    }
}


using SensorFacebook.Application.Services.AccountServices.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.AccountServices
{
    public interface IAccountSelector
    {
        // fairness + capacity + cooldown + “dính” proxy
        Task<AccountLease> AcquireAsync(
            int requiredProxyGroupId,
            LeasePriority priority,
            string consumerKey,                    // tên worker/queue/consumer (idempotency)
            TimeSpan? ttl = null,
            CancellationToken ct = default);

        // Idempotency: mượn đúng account nếu đang cầm bởi chính consumer này → renew
        Task<AccountLease> AcquireByAccountAsync(
            Guid accountId,
            int proxyGroupId,
            string consumerKey,
            TimeSpan? ttl = null,
            CancellationToken ct = default);

        Task<bool> RenewAsync(Guid sessionId, TimeSpan? extendBy = null, CancellationToken ct = default);

        Task<bool> ReleaseAsync(Guid sessionId, bool checkpoint = false, string? note = null, CancellationToken ct = default);
    }
}

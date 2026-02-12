using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.BrowserPool
{
    public interface IBrowserPool
    {
        Task<IBrowserLease> AcquireAsync(
            Guid? accountId,
            int? proxyGroupId,
            CancellationToken ct = default);
    }
}

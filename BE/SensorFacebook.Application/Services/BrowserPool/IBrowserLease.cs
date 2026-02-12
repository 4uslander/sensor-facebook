using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.BrowserPool
{
    public interface IBrowserLease : IAsyncDisposable
    {
        Guid? AccountId { get; }
        int? ProxyGroupId { get; }

        Task<object> NewPageAsync(CancellationToken ct = default);
    }
}

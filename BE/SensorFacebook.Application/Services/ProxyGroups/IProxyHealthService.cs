using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.ProxyGroups
{
    public interface IProxyHealthService
    {
        Task<ProxyHealthDto?> GetLatestAsync(int proxyGroupId, CancellationToken ct = default);
        Task<ProxyHealthDto> CheckNowAsync(int proxyGroupId, int timeoutMs = 8000, CancellationToken ct = default);
    }
}

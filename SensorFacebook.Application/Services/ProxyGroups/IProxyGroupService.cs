using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.ProxyGroups
{
    public interface IProxyGroupService
    {
        Task<(IReadOnlyList<ProxyGroupDto> items, int total)> ListAsync(
            int page, int pageSize, string? q, string? status, string? region, CancellationToken ct = default);

        Task<ProxyGroupDto?> GetAsync(int id, CancellationToken ct = default);
        Task<int> CreateAsync(CreateProxyGroupRequest req, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, UpdateProxyGroupRequest req, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}

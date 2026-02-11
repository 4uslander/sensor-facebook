using SensorFacebook.Application.Services.AccountServices.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.AccountServices
{
    public interface IAccountService
    {
        Task<(IReadOnlyList<FbAccountDto> items, int total)> ListAsync(
            int page, int pageSize, string? q, string? status, string? region, CancellationToken ct = default);

        Task<FbAccountDto?> GetAsync(Guid id, CancellationToken ct = default);

        Task<Guid> CreateOrUpdateAsync(CreateOrUpdateAccountRequest req, Guid? currentUserId, CancellationToken ct = default);

        Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default);

        Task<bool> LockAsync(Guid id, CancellationToken ct = default);

        Task<bool> UnlockAsync(Guid id, CancellationToken ct = default);

        Task<IReadOnlyList<AccountEventDto>> GetEventsAsync(Guid id, int page, int pageSize, CancellationToken ct = default);
    }
}

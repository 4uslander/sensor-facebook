using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.UserServices
{
    public interface IUserService
    {
        Task<object?> GetProfileAsync(Guid userId, CancellationToken ct = default);

        Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);

        Task<bool> UpdateEmailAsync(Guid userId, string newEmail, CancellationToken ct = default);

        // Admin-only
        Task<bool> SetRoleAsync(Guid userId, string roleName, CancellationToken ct = default);
        Task<(IReadOnlyList<object> items, int total)> ListAsync(int page, int pageSize, string? q, CancellationToken ct = default);
    }
}

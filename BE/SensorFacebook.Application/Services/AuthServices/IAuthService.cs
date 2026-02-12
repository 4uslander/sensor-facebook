using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.AuthServices
{
    public interface IAuthService
    {
        Task<(bool ok, string? error)> RegisterAsync(string email, string password, string role = "user", CancellationToken ct = default);

        Task<(bool ok, string? error, Guid userId, string email, string role)>
            ValidateUserAsync(string email, string password, CancellationToken ct);

        Task<(string accessToken, DateTimeOffset accessExp, string refreshToken, DateTimeOffset refreshExp)>
            IssueTokensAsync(Guid userId, string email, string role, string? deviceInfo, string? ip, CancellationToken ct);

        Task<bool> RevokeRefreshAsync(Guid userId, string refreshToken, CancellationToken ct);

        Task<(bool ok, string? error, string newAccess, DateTimeOffset exp, string newRefresh, DateTimeOffset refreshExp)>
            RotateRefreshAsync(string refreshToken, CancellationToken ct);
    }
}

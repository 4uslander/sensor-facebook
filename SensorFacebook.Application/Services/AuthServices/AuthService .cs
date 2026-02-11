using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SensorFacebook.Application.Services.PasswordServices;
using SensorFacebook.Application.Services.TokenServices;
using SensorFacebook.Infrastructure.Entities;
using SensorFacebook.Infrastructure.Models;


namespace SensorFacebook.Application.Services.AuthServices
{
    public sealed class AuthService : IAuthService
    {
        private readonly SensorDbContext _db;
        private readonly IJwtTokenService _jwt;
        private readonly IPasswordHasher _hasher;

        public AuthService(SensorDbContext db, IJwtTokenService jwt, IPasswordHasher hasher)
        {
            _db = db; _jwt = jwt; _hasher = hasher;
        }

        private static string HashRefreshToken(string token)
            => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        public async Task<(bool ok, string? error)> RegisterAsync(string email, string password, string role = "user", CancellationToken ct = default)
        {
            email = email.Trim().ToLowerInvariant();
            if (await _db.Users.AnyAsync(x => x.Email == email, ct))
                return (false, "Email đã tồn tại");

            var roleName = string.IsNullOrWhiteSpace(role) ? "user" : role.Trim().ToLowerInvariant();
            var roleId = await _db.Roles
                .Where(r => r.Name == roleName)
                .Select(r => r.Id)
                .FirstOrDefaultAsync(ct);

            if (roleId == 0)
            {
                var newRole = new Role { Name = roleName, Description = "auto-created" };
                _db.Roles.Add(newRole);
                await _db.SaveChangesAsync(ct);
                roleId = newRole.Id;
            }

            var user = new User
            {
                Email = email,
                PasswordHash = _hasher.Hash(password),
                RoleId = roleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        public async Task<(bool ok, string? error, Guid userId, string email, string role)>
            ValidateUserAsync(string email, string password, CancellationToken ct)
        {
            var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email, ct);
            if (u == null || u.IsActive != true) return (false, "Tài khoản không tồn tại hoặc bị khóa", Guid.Empty, "", "");
            if (!_hasher.Verify(u.PasswordHash, password)) return (false, "Sai mật khẩu", Guid.Empty, "", "");

            // Nếu model có Role navigation + RoleId, cần lấy roleName:
            var roleName = u.Role?.Name ?? "user";        // nếu có navigation
            // hoặc nếu bạn lưu thêm trường text RoleName trên Users, hãy thay bằng u.RoleName

            return (true, null, u.Id, u.Email, roleName);
        }

        public async Task<(string accessToken, DateTimeOffset accessExp, string refreshToken, DateTimeOffset refreshExp)>
            IssueTokensAsync(Guid userId, string email, string role, string? deviceInfo, string? ip, CancellationToken ct)
        {
            var (access, accessExp, jti) = _jwt.CreateAccessToken(userId, email, role);
            var (refresh, refreshExp) = _jwt.CreateRefreshToken();

            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                _db.AuthRefreshTokens.Add(new AuthRefreshToken
                {
                    UserId = userId,
                    TokenHash = HashRefreshToken(refresh),
                    JwtId = jti,
                    DeviceInfo = deviceInfo,
                    IpAddress = ip,
                    ExpiresAt = refreshExp.UtcDateTime,   // nếu property là DateTime; dùng refreshExp nếu là DateTimeOffset
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }

            return (access, accessExp, refresh, refreshExp);
        }

        public async Task<bool> RevokeRefreshAsync(Guid userId, string refreshToken, CancellationToken ct)
        {
            var hash = HashRefreshToken(refreshToken);
            var row = await _db.AuthRefreshTokens
                .FirstOrDefaultAsync(x => x.UserId == userId && x.TokenHash == hash && x.RevokedAt == null, ct);
            if (row is null) return false;
            row.RevokedAt = DateTime.UtcNow;              
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<(bool ok, string? error, string newAccess, DateTimeOffset exp, string newRefresh, DateTimeOffset refreshExp)>
            RotateRefreshAsync(string refreshToken, CancellationToken ct)
        {
            var hash = HashRefreshToken(refreshToken);
            var row = await _db.AuthRefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.TokenHash == hash && x.RevokedAt == null, ct);

            if (row == null) return (false, "Refresh token không hợp lệ", "", default, "", default);

            // Nếu ExpiresAt là DateTimeOffset:
            if (row.ExpiresAt <= DateTimeOffset.UtcNow) return (false, "Refresh token đã hết hạn", "", default, "", default);

            // Nếu User.IsActive là bool?:
            if (row.User == null || row.User.IsActive != true) return (false, "Tài khoản không hợp lệ", "", default, "", default);

            row.RevokedAt = DateTime.UtcNow;

            var roleName = row.User.Role?.Name ?? "user"; // nếu navigation
            var (access, accessExp, jti) = _jwt.CreateAccessToken(row.User.Id, row.User.Email, roleName);
            var (newRefresh, newRefreshExp) = _jwt.CreateRefreshToken();

            _db.AuthRefreshTokens.Add(new AuthRefreshToken
            {
                UserId = row.User.Id,
                TokenHash = HashRefreshToken(newRefresh),
                JwtId = jti,
                ExpiresAt = newRefreshExp.UtcDateTime,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
            return (true, null, access, accessExp, newRefresh, newRefreshExp);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using SensorFacebook.Application.Services.PasswordServices;
using SensorFacebook.Infrastructure.Entities;
using SensorFacebook.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.UserServices
{
    public sealed class UserService : IUserService
    {
        private readonly SensorDbContext _db;
        private readonly IPasswordHasher _hasher;

        public UserService(SensorDbContext db, IPasswordHasher hasher)
        {
            _db = db; _hasher = hasher;
        }

        public async Task<object?> GetProfileAsync(Guid userId, CancellationToken ct = default)
        {
            var u = await _db.Users
                .AsNoTracking()
                .Include(x => x.Role)          // nếu có navigation
                .FirstOrDefaultAsync(x => x.Id == userId, ct);

            if (u is null) return null;

            var roleName = u.Role?.Name ?? "user"; // nếu không có navigation, thay bằng u.RoleName (string)
            return new
            {
                u.Id,
                u.Email,
                Role = roleName,
                IsActive = u.IsActive == true,
                u.CreatedAt
            };
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default)
        {
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
            if (u is null || u.IsActive != true) return false;

            if (!_hasher.Verify(u.PasswordHash, currentPassword)) return false;

            u.PasswordHash = _hasher.Hash(newPassword);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> UpdateEmailAsync(Guid userId, string newEmail, CancellationToken ct = default)
        {
            newEmail = newEmail.Trim().ToLowerInvariant();

            if (await _db.Users.AnyAsync(x => x.Email == newEmail && x.Id != userId, ct))
                return false;

            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
            if (u is null || u.IsActive != true) return false;

            u.Email = newEmail;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> SetRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
        {
            roleName = roleName.Trim().ToLowerInvariant();

            // Model phổ biến: Users có RoleId (int) + navigation Role
            var roleId = await _db.Roles.Where(r => r.Name == roleName).Select(r => r.Id).FirstOrDefaultAsync(ct);
            if (roleId == 0)
            {
                var r = new Role { Name = roleName, Description = "created-by-admin" };
                _db.Roles.Add(r);
                await _db.SaveChangesAsync(ct);
                roleId = r.Id;
            }

            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
            if (u is null) return false;

            u.RoleId = roleId;               // nếu bạn lưu string RoleName, đổi thành: u.RoleName = roleName;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<(System.Collections.Generic.IReadOnlyList<object> items, int total)> ListAsync(int page, int pageSize, string? q, CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

            var query = _db.Users.AsNoTracking().Include(x => x.Role).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLowerInvariant();
                query = query.Where(x => x.Email.ToLower().Contains(q));
            }

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new {
                    x.Id,
                    x.Email,
                    Role = x.Role!.Name,
                    IsActive = x.IsActive == true,
                    x.CreatedAt
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}

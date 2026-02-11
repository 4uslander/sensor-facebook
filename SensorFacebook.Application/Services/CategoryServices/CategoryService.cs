using Microsoft.EntityFrameworkCore;
using SensorFacebook.Infrastructure.Entities;
using SensorFacebook.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SensorFacebook.Application.Services.CategoryServices
{
    public sealed class CategoryService : ICategoryService
    {
        private readonly SensorDbContext _db;

        public CategoryService(SensorDbContext db)
        {
            _db = db;
        }

        public async Task<(IReadOnlyList<CategoryDto> items, int total)> ListAsync(
            int page, int pageSize, string? q, bool? active, CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

            var query = _db.Categories.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var k = q.Trim().ToLowerInvariant();
                query = query.Where(x =>
                    x.Name.ToLower().Contains(k) ||
                    (x.Description != null && x.Description.ToLower().Contains(k)));
            }

            if (active is not null)
                query = query.Where(x => x.Active == active.Value);

            var total = await query.CountAsync(ct);

            var list = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CategoryDto(
                    x.Id,
                    x.Name,
                    x.Description,
                    x.OwnerId,
                    x.Active ?? true,
                    (DateTimeOffset)x.CreatedAt
                ))
                .ToListAsync(ct);

            return (list, total);
        }

        public async Task<CategoryDto?> GetAsync(int id, CancellationToken ct = default)
        {
            var c = await _db.Categories.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            return c is null
                ? null
                : new CategoryDto(c.Id, c.Name, c.Description, c.OwnerId, c.Active ?? true, (DateTimeOffset)c.CreatedAt);
        }

        public async Task<int> CreateAsync(CreateCategoryRequest req, Guid? ownerId, CancellationToken ct = default)
        {
            var name = (req.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required");

            var exists = await _db.Categories
                .AnyAsync(x => x.Name.ToLower() == name.ToLower(), ct);

            if (exists)
                throw new InvalidOperationException("Category name already exists");

            var entity = new Category
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description!.Trim(),
                OwnerId = ownerId,
                Active = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Categories.Add(entity);
            await _db.SaveChangesAsync(ct);

            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, UpdateCategoryRequest req, CancellationToken ct = default)
        {
            var entity = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return false;

            if (!string.IsNullOrWhiteSpace(req.Name))
            {
                var name = req.Name.Trim();
                var dup = await _db.Categories
                    .AnyAsync(x => x.Id != id && x.Name.ToLower() == name.ToLower(), ct);

                if (dup)
                    throw new InvalidOperationException("Category name already exists");

                entity.Name = name;
            }

            if (req.Description is not null)
                entity.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();

            if (req.Active is not null)
                entity.Active = req.Active.Value;

            return await _db.SaveChangesAsync(ct) > 0;
        }

        public async Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return false;

            if (entity.Active == false) return true;

            entity.Active = false;
            return await _db.SaveChangesAsync(ct) > 0;
        }

        public async Task<bool> RestoreAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return false;

            if (entity.Active == true) return true;

            entity.Active = true;
            return await _db.SaveChangesAsync(ct) > 0;
        }
    }
}

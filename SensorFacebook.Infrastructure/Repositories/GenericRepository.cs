using Microsoft.EntityFrameworkCore;
using SensorFacebook.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SensorFacebook.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly SensorDbContext _db;
        protected readonly DbSet<T> _set;

        public GenericRepository(SensorDbContext dbContext)
        {
            _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _set = _db.Set<T>();
        }

        public IQueryable<T> AsQueryable(bool asNoTracking = true)
        {
            var q = _set.AsQueryable();
            return asNoTracking ? q.AsNoTracking() : q;
        }

        public async Task<T?> GetByIdAsync(CancellationToken ct = default, params object[] keyValues)
        {
            if (keyValues is null || keyValues.Length == 0)
                throw new ArgumentException("Must provide key values", nameof(keyValues));

            // FindAsync hỗ trợ composite key theo thứ tự khai báo key trong EF.
            return await _set.FindAsync(keyValues, ct);
        }

        public async Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            bool asNoTracking = true,
            CancellationToken ct = default,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> q = _set;

            if (includes is { Length: > 0 })
                q = includes.Aggregate(q, (current, include) => current.Include(include));

            if (asNoTracking) q = q.AsNoTracking();

            return await q.FirstOrDefaultAsync(predicate, ct);
        }

        public async Task<List<T>> ListAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            bool asNoTracking = true,
            int? skip = null,
            int? take = null,
            CancellationToken ct = default,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> q = _set;

            if (includes is { Length: > 0 })
                q = includes.Aggregate(q, (current, include) => current.Include(include));

            if (predicate != null)
                q = q.Where(predicate);

            if (orderBy != null)
                q = orderBy(q);

            if (asNoTracking)
                q = q.AsNoTracking();

            if (skip.HasValue && skip.Value > 0)
                q = q.Skip(skip.Value);

            if (take.HasValue && take.Value > 0)
                q = q.Take(take.Value);

            return await q.ToListAsync(ct);
        }

        public Task<bool> AnyAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
            => predicate is null
                ? _set.AnyAsync(ct)
                : _set.AnyAsync(predicate, ct);

        public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
            => predicate is null
                ? _set.CountAsync(ct)
                : _set.CountAsync(predicate, ct);

        public async Task AddAsync(T entity, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await _set.AddAsync(entity, ct);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entities);
            await _set.AddRangeAsync(entities, ct);
        }

        public void Update(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _set.Update(entity);
        }

        public void UpdateRange(IEnumerable<T> entities)
        {
            ArgumentNullException.ThrowIfNull(entities);
            _set.UpdateRange(entities);
        }

        public void Remove(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _set.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            ArgumentNullException.ThrowIfNull(entities);
            _set.RemoveRange(entities);
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => _db.SaveChangesAsync(ct);
    }
}

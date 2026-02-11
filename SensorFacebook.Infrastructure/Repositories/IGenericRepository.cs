using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SensorFacebook.Infrastructure.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        /// <summary>Trả về IQueryable để bạn tự build query (có thể kèm AsNoTracking).</summary>
        IQueryable<T> AsQueryable(bool asNoTracking = true);

        /// <summary>Tìm theo key (hỗ trợ composite key qua params).</summary>
        Task<T?> GetByIdAsync(CancellationToken ct = default, params object[] keyValues);

        /// <summary>Trả về bản ghi đầu tiên thỏa predicate (có thể Include).</summary>
        Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            bool asNoTracking = true,
            CancellationToken ct = default,
            params Expression<Func<T, object>>[] includes);

        /// <summary>Danh sách theo điều kiện + sắp xếp + phân trang (có thể Include).</summary>
        Task<List<T>> ListAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            bool asNoTracking = true,
            int? skip = null,
            int? take = null,
            CancellationToken ct = default,
            params Expression<Func<T, object>>[] includes);

        /// <summary>Kiểm tra tồn tại.</summary>
        Task<bool> AnyAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);

        /// <summary>Đếm số lượng.</summary>
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);

        /// <summary>Thêm 1 bản ghi.</summary>
        Task AddAsync(T entity, CancellationToken ct = default);

        /// <summary>Thêm nhiều bản ghi.</summary>
        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

        /// <summary>Cập nhật 1 bản ghi (tracking).</summary>
        void Update(T entity);

        /// <summary>Cập nhật nhiều bản ghi (tracking).</summary>
        void UpdateRange(IEnumerable<T> entities);

        /// <summary>Xóa 1 bản ghi.</summary>
        void Remove(T entity);

        /// <summary>Xóa nhiều bản ghi.</summary>
        void RemoveRange(IEnumerable<T> entities);

        /// <summary>Lưu thay đổi (nếu bạn dùng repository như UoW mỏng).</summary>
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}

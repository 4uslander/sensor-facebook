using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.CategoryServices
{
    public interface ICategoryService
    {
        Task<(IReadOnlyList<CategoryDto> items, int total)> ListAsync(int page, int pageSize, string? q, bool? active, CancellationToken ct = default);
        Task<CategoryDto?> GetAsync(int id, CancellationToken ct = default);
        Task<int> CreateAsync(CreateCategoryRequest req, Guid? ownerId, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, UpdateCategoryRequest req, CancellationToken ct = default);
        Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default);
        Task<bool> RestoreAsync(int id, CancellationToken ct = default);
    }

    public sealed record CategoryDto(int Id, string Name, string? Description, Guid? OwnerId, bool Active, DateTimeOffset CreatedAt);
    public sealed record CreateCategoryRequest(string Name, string? Description);
    public sealed record UpdateCategoryRequest(string Name, string? Description, bool? Active);
}

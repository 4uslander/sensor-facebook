using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SensorFacebook.Application.Services.KeywordServices;

public interface IKeywordService
{
    Task<(IReadOnlyList<KeywordDto> items, int total)> ListAsync(
            int page,
            int pageSize,
            string? q,
            int? categoryId,
            bool? active,
            string? sortBy = null,                 // relevance | distance_asc | date_desc | price_asc | price_desc
            IEnumerable<string>? conditions = null, // new | like_new | good | fair
            string? listedTime = null,             // all | 24h | 7d | 30d
            string? availability = null,           // available | sold
            CancellationToken ct = default);
    Task<KeywordDto?> GetAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(CreateKeywordRequest req, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, UpdateKeywordRequest req, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

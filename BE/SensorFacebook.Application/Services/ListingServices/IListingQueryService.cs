using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.ListingServices
{
    public interface IListingQueryService
    {
        Task<(IReadOnlyList<ListingListItemDto> items, int total)> ListAsync(
            int? keywordId, string? q, bool? isActive,
            DateTimeOffset? from, DateTimeOffset? to,
            int page, int pageSize, CancellationToken ct = default);

        Task<ListingDetailDto?> GetAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<ListingChangeDto>> GetChangesAsync(Guid id, CancellationToken ct = default);
    }

    public sealed record ListingListItemDto(
        Guid Id,
        string? Title,
        decimal? Price,
        string? Currency,
        string? Location,
        bool IsActive,
        DateTimeOffset FirstSeen,
        DateTimeOffset LastSeen,
        string? Link
    );

    public sealed record ListingDetailDto(
        Guid Id,
        string ExternalId,
        string? Title,
        decimal? Price,
        string? Currency,
        string? Location,
        string? Condition,
        bool IsActive,
        DateTimeOffset FirstSeen,
        DateTimeOffset LastSeen
    );
    public sealed record ListingChangeDto(Guid Id, string ChangeType, string? OldValue, string? NewValue, DateTimeOffset OccurredAt);
}

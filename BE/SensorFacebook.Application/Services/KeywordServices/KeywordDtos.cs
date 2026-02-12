using System;

namespace SensorFacebook.Application.Services.KeywordServices;

public sealed record KeywordDto(
    int Id,
    string Text,
    int? CategoryId,
    int Priority,
    bool Active,
    decimal? LocationLat,
    decimal? LocationLon,
    int? RadiusKm,
    string RadiusPolicy,
    string SortBy,
    string[]? Conditions,
    string ListedTime,
    string Availability,
    DateTime? CreatedAt
);

public sealed record CreateKeywordRequest(
    string Text,
    int? CategoryId,
    int? Priority,
    bool? Active,
    decimal? LocationLat,
    decimal? LocationLon,
    int? RadiusKm,
    string? RadiusPolicy,
    string? SortBy,
    IEnumerable<string>? Conditions,
    string? ListedTime,
    string? Availability
);

public sealed record UpdateKeywordRequest(
    string? Text,
    int? CategoryId,
    int? Priority,
    bool? Active,
    decimal? LocationLat,
    decimal? LocationLon,
    int? RadiusKm,
    string? RadiusPolicy,
    string? SortBy,
    IEnumerable<string>? Conditions,
    string? ListedTime,
    string? Availability
);

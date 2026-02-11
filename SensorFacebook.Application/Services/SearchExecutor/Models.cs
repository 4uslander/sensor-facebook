using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.SearchExecutor
{
    public sealed record KeywordConfig(
        string Q,
        double? LocationLat,
        double? LocationLon,
        double? RadiusKm,         
        string SortBy,            // relevance|distance_asc|date_desc|price_asc|price_desc
        IReadOnlyList<string> Conditions,
        string ListedTime,        // all|24h|7d|30d
        string Availability       // available|sold
    );

    public sealed record SearchItem(
        string ExternalId,
        string? Title,
        decimal? Price,
        string? Currency,
        string? LocationText,
        string? Condition,
        DateTimeOffset? PostedTime,
        bool? IsSold,
        string PayloadJson // raw json (ảnh, seller, raw blocks…)
    );

    public sealed record SearchResult(
        int Total,
        List<SearchItem> Items
    );
}

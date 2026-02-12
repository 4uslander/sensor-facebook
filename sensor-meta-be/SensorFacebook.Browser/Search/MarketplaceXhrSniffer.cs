using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using SensorFacebook.Application.Services.SearchExecutor;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SensorFacebook.Browser.Search;

public sealed class MarketplaceXhrSniffer : IAsyncDisposable
{
    private readonly ILogger _log;
    private readonly List<SearchItem> _items = new();
    private readonly List<string> _rawJsonSamples = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    private IPage? _page;
    private bool _attached;

    public MarketplaceXhrSniffer(ILogger log)
    {
        _log = log;
    }

    public IReadOnlyList<SearchItem> Items => _items;
    public IReadOnlyList<string> RawJsonSamples => _rawJsonSamples;

    public void Attach(IPage page)
    {
        if (_attached) return;
        _page = page;
        _attached = true;

        page.Response += OnResponse;
    }

    public void Detach()
    {
        if (!_attached || _page is null) return;
        _page.Response -= OnResponse;
        _attached = false;
        _page = null;
    }

    private bool IsMarketplaceDataResponse(IResponse resp)
    {
        var url = resp.Url ?? "";
        if (url.Contains("graphql", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("/api/graphql", StringComparison.OrdinalIgnoreCase))
            return true;

        // fallback: đôi khi marketplace có endpoint khác
        if (url.Contains("/marketplace", StringComparison.OrdinalIgnoreCase) &&
            url.Contains("query", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private async void OnResponse(object? sender, IResponse resp)
    {
        try
        {
            if (!IsMarketplaceDataResponse(resp)) return;

            // chỉ bắt JSON
            var ct = resp.Headers.TryGetValue("content-type", out var ctype) ? ctype : "";
            if (!ct.Contains("application/json", StringComparison.OrdinalIgnoreCase) &&
                !ct.Contains("text/javascript", StringComparison.OrdinalIgnoreCase))
            {
                // GraphQL đôi khi vẫn là application/json; nếu không phải thì bỏ
                // (muốn debug sâu hơn thì bỏ check này)
            }

            // status OK
            if (resp.Status < 200 || resp.Status >= 300) return;

            var text = await resp.TextAsync();
            if (string.IsNullOrWhiteSpace(text)) return;

            // lưu sample để debug (giới hạn)
            await _lock.WaitAsync();
            try
            {
                if (_rawJsonSamples.Count < 5)
                    _rawJsonSamples.Add(text);
            }
            finally { _lock.Release(); }

            // parse và extract listing
            ExtractListingsFromJson(text);
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "XHR sniff failed");
        }
    }

    private void ExtractListingsFromJson(string json)
    {
        // Heuristic: tìm mọi URL dạng /marketplace/item/<digits>
        // Đây là “điểm bám” ổn nhất để ra ExternalId, rồi sau đó bạn refine schema sau.
        var matches = Regex.Matches(json, @"\/marketplace\/item\/(\d+)");
        if (matches.Count == 0) return;

        // Dedupe theo ExternalId
        var ids = matches.Select(m => m.Groups[1].Value).Distinct().ToList();
        if (ids.Count == 0) return;

        lock (_items)
        {
            foreach (var id in ids)
            {
                if (_items.Any(x => x.ExternalId == id)) continue;

                // hiện tại chưa chắc parse được title/price từ JSON chung,
                // nên nhồi payload raw để enrich sau.
                _items.Add(new SearchItem(
                    ExternalId: id,
                    Title: null,
                    Price: null,
                    Currency: "USD",
                    LocationText: null,
                    Condition: null,
                    PostedTime: null,
                    IsSold: null,
                    PayloadJson: null
                ));
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        Detach();
        _lock.Dispose();
        return ValueTask.CompletedTask;
    }
}

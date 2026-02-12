using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using SensorFacebook.Application.Services.SearchExecutor;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SensorFacebook.Browser.Search
{
    public sealed class FacebookMarketplaceSearchExecutor : ISearchExecutor
    {
        private readonly ILogger<FacebookMarketplaceSearchExecutor> _log;

        public FacebookMarketplaceSearchExecutor(ILogger<FacebookMarketplaceSearchExecutor> log)
        {
            _log = log;
        }

        public async Task<SearchResult> ExecuteAsync(IPage page, KeywordConfig cfg, CancellationToken ct)
        {
            await page.GotoAsync("https://www.facebook.com/marketplace", new()
            {
                Timeout = 45000,
                WaitUntil = WaitUntilState.NetworkIdle
            });

            // 0) Attach sniffer ngay từ đầu
            await using var sniffer = new MarketplaceXhrSniffer(_log);
            sniffer.Attach(page);

            // 1) Nhập từ khoá + filters (những thao tác này sẽ trigger XHR)
            await TypeQueryAsync(page, cfg.Q, ct);
            await ApplySortAsync(page, cfg.SortBy, ct);
            await ApplyListedTimeAsync(page, cfg.ListedTime, ct);

            if (cfg.LocationLat is not null && cfg.LocationLon is not null && cfg.RadiusKm is not null)
                await TryApplyLocationAsync(page, cfg.LocationLat.Value, cfg.LocationLon.Value, cfg.RadiusKm.Value, ct);

            await ApplyAvailabilityAsync(page, cfg.Availability, ct);
            await ApplyConditionsAsync(page, cfg.Conditions, ct);

            // 2) Chờ XHR settle thêm 1 nhịp (FB không phải lúc nào cũng network-idle đúng)
            await page.WaitForTimeoutAsync(1500);

            // 3) Lấy items từ XHR
            var xhrItems = sniffer.Items.ToList();

            // Nếu XHR không bắt được gì, fallback DOM (tạm thời)
            List<SearchItem> items;
            if (xhrItems.Count > 0)
            {
                items = xhrItems;
            }
            else
            {
                _log.LogWarning("XHR items empty, fallback DOM parsing");
                items = await ParseSearchListAsync(page, ct);
            }

            // 4) nếu muốn debug schema: log sample (chỉ khi cần)
            if (xhrItems.Count == 0 && sniffer.RawJsonSamples.Count > 0)
            {
                _log.LogInformation("Got {n} raw JSON samples (first len={len})",
                    sniffer.RawJsonSamples.Count,
                    sniffer.RawJsonSamples[0].Length);
            }

            return new SearchResult(items.Count, items);
        }

        // ====== Helpers ======
        private static class Sel
        {
            // Thay bằng selector thực tế khi dev (demo placeholders)
            public const string SearchInput = "input[placeholder*='Search Marketplace'],input[aria-label*='Search Marketplace']";
            public const string SortDropdown = "[role=button][aria-label*='Sort by'],[aria-label*='Sort by'][role=combobox]";
            public const string SortMenu = "div[role=listbox]";
            public const string FilterButton = "div[role=button]:has-text('Filters'),[aria-label='Filters']";
            public const string ListedAny = "text=Any time,text=All";
            public const string Listed24h = "text=Past 24 hours";
            public const string Listed7d = "text=Past week";
            public const string Listed30d = "text=Past month";
            public const string AvailabilityAvailable = "text=Available";
            public const string AvailabilitySold = "text=Sold";

            // Result cards (placeholder – cần sửa theo DOM thực)
            public const string Card = "a[href*='/marketplace/item/']";
            public const string CardTitle = "div[role=main] a[href*='/marketplace/item/'] div:below(:text('Sponsored'))";
            public const string CardPrice = "[dir='auto']:has-text('$')"; // sẽ refine
            public const string CardLocation = "div[dir='auto']:below(:text('$'))";
        }

        private async Task TypeQueryAsync(IPage page, string q, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(q)) return;

            var input = await page.QuerySelectorAsync(Sel.SearchInput);
            if (input is not null)
            {
                await input.FillAsync(q);
                await page.Keyboard.PressAsync("Enter");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
        }

        private async Task ApplySortAsync(IPage page, string sortBy, CancellationToken ct)
        {
            // distance_asc | date_desc | price_asc | price_desc | relevance
            try
            {
                var button = await page.QuerySelectorAsync(Sel.SortDropdown);
                if (button is null) return;

                await button.ClickAsync();
                await page.WaitForSelectorAsync(Sel.SortMenu);

                string label = sortBy switch
                {
                    "distance_asc" => "Distance: Nearest first",
                    "date_desc" => "Date listed: Newest first",
                    "price_asc" => "Price: Lowest first",
                    "price_desc" => "Price: Highest first",
                    _ => "Relevance"
                };

                var opt = await page.GetByRole(AriaRole.Option, new() { Name = label }).First.OrNullAsync();
                if (opt is not null)
                {
                    await opt.ClickAsync();
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                }
            }
            catch { /* non-fatal */ }
        }

        private async Task ApplyListedTimeAsync(IPage page, string listedTime, CancellationToken ct)
        {
            try
            {
                // tuỳ UI: nhiều khi listed time nằm trong Filters
                await OpenFiltersIfAny(page);

                string selector = listedTime switch
                {
                    "24h" => Sel.Listed24h,
                    "7d" => Sel.Listed7d,
                    "30d" => Sel.Listed30d,
                    _ => Sel.ListedAny
                };

                var el = await page.QuerySelectorAsync(selector);
                if (el is not null)
                {
                    await el.ClickAsync();
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                }
            }
            catch { /* ignore */ }
        }

        private async Task ApplyAvailabilityAsync(IPage page, string availability, CancellationToken ct)
        {
            try
            {
                await OpenFiltersIfAny(page);
                string selector = availability == "sold" ? Sel.AvailabilitySold : Sel.AvailabilityAvailable;
                var el = await page.QuerySelectorAsync(selector);
                if (el is not null)
                {
                    await el.ClickAsync();
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                }
            }
            catch { /* ignore */ }
        }

        private async Task ApplyConditionsAsync(IPage page, IReadOnlyList<string> conds, CancellationToken ct)
        {
            if (conds is null || conds.Count == 0) return;
            try
            {
                await OpenFiltersIfAny(page);
                // ví dụ map:
                foreach (var c in conds)
                {
                    string label = c switch
                    {
                        "new" => "New",
                        "like_new" => "Used – Like New",
                        "good" => "Used – Good",
                        "fair" => "Used – Fair",
                        _ => ""
                    };
                    if (string.IsNullOrEmpty(label)) continue;
                    var el = await page.GetByLabel(label).First.OrNullAsync();
                    if (el is not null) await el.ClickAsync();
                }
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
            catch { /* ignore */ }
        }

        private async Task TryApplyLocationAsync(IPage page, double lat, double lon, double radiusKm, CancellationToken ct)
        {
            // Thực tế FB set location qua popup, hoặc thông qua URL có query "latitude/longitude/radius".
            // Ở đây demo 2 hướng:
            // (1) Nếu có URL pattern, thử điều hướng lại:
            //      https://www.facebook.com/marketplace/?latitude=..&longitude=..&radius_km=..
            try
            {
                var url = $"https://www.facebook.com/marketplace/?latitude={lat.ToString(CultureInfo.InvariantCulture)}&longitude={lon.ToString(CultureInfo.InvariantCulture)}&radius_km={radiusKm.ToString(CultureInfo.InvariantCulture)}";
                await page.GotoAsync(url, new() { Timeout = 30000, WaitUntil = WaitUntilState.NetworkIdle });
                return;
            }
            catch
            {
                // (2) fallback UI thao tác: mở location chooser, nhập city/coords (cần selector thật)
                // Placeholder – để bạn bổ sung khi dev thực tế:
                // await page.ClickAsync("button:has-text('Choose location')");
                // await page.FillAsync("input[aria-label='City, State']", "..."); …
            }
        }

        private async Task OpenFiltersIfAny(IPage page)
        {
            try
            {
                var btn = await page.QuerySelectorAsync(Sel.FilterButton);
                if (btn is not null) await btn.ClickAsync();
            }
            catch { }
        }

        private async Task<List<SearchItem>> ParseSearchListAsync(IPage page, CancellationToken ct)
        {
            var items = new List<SearchItem>();

            // Lấy tất cả card link /marketplace/item/<id>
            var links = await page.QuerySelectorAllAsync(Sel.Card);
            foreach (var a in links)
            {
                try
                {
                    var href = await a.GetAttributeAsync("href");
                    if (string.IsNullOrWhiteSpace(href)) continue;

                    var m = Regex.Match(href, @"\/marketplace\/item\/(\d+)");
                    if (!m.Success) continue;
                    var id = m.Groups[1].Value;

                    // Title (thường nằm trong cây con của thẻ a)
                    string? title = await a.InnerTextAsync();
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        title = title.Trim();
                    }

                    // Parse price (rất phụ thuộc UI; demo đơn giản)
                    decimal? price = null; string? currency = null;
                    var text = (await a.InnerTextAsync()) ?? "";
                    var priceMatch = Regex.Match(text, @"(\$|US\$)\s?([\d,]+)");
                    if (priceMatch.Success)
                    {
                        currency = "USD";
                        if (decimal.TryParse(priceMatch.Groups[2].Value.Replace(",", ""), out var p))
                            price = p;
                    }

                    // Sold?
                    bool? sold = text.Contains("Sold", StringComparison.OrdinalIgnoreCase) ? true : (bool?)null;

                    // raw payload để lưu thêm
                    var payload = JsonSerializer.Serialize(new { href });

                    items.Add(new SearchItem(
                        ExternalId: id,
                        Title: title,
                        Price: price,
                        Currency: currency,
                        LocationText: null,
                        Condition: null,
                        PostedTime: null,
                        IsSold: sold,
                        PayloadJson: payload
                    ));
                }
                catch { /* skip bad card */ }
            }

            return items;
        }
    }

    // small helper: allow OrNull for locators
    internal static class PlaywrightExt
    {
        public static async Task<ILocator?> OrNullAsync(this ILocator locator)
        {
            try { var count = await locator.CountAsync(); return count > 0 ? locator : null; }
            catch { return null; }
        }
    }
}

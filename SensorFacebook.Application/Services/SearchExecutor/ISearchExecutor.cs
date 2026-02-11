using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.SearchExecutor
{
    public interface ISearchExecutor
    {
        Task<SearchResult> ExecuteAsync(IPage page, KeywordConfig cfg, CancellationToken ct);
    }
}

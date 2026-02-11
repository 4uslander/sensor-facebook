using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SensorFacebook.Application.Services.SearchJobServices;

[ApiController]
[Route("api/jobs/search")]
//[Authorize(Roles = "admin")]
public sealed class SearchJobsController : ControllerBase
{
    private readonly ISearchJobService _svc;
    public SearchJobsController(ISearchJobService svc) => _svc = svc;

    [HttpPost("run-now/{keywordId:int}")]
    public async Task<IActionResult> RunNow([FromRoute] int keywordId, [FromQuery] string priority = "high", CancellationToken ct = default)
    {
        var id = await _svc.RunNowAsync(keywordId, priority, ct);
        return Ok(new { jobId = id, priority });
    }

    [HttpGet("{jobId:guid}")]
    public async Task<IActionResult> Get([FromRoute] Guid jobId, CancellationToken ct)
    {
        var job = await _svc.GetAsync(jobId, ct);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status, [FromQuery] int? keywordId,
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var (items, total) = await _svc.ListAsync(status, keywordId, from, to, page, pageSize, ct);
        return Ok(new { total, page, pageSize, items });
    }

    [HttpPost("{jobId:guid}/retry")]
    public async Task<IActionResult> Retry([FromRoute] Guid jobId, CancellationToken ct)
    {
        var ok = await _svc.RetryAsync(jobId, ct);
        return ok ? Ok(new { ok = true }) : NotFound(new { ok = false });
    }

    [HttpPost("{jobId:guid}/cancel")]
    public async Task<IActionResult> Cancel([FromRoute] Guid jobId, CancellationToken ct)
    {
        var ok = await _svc.CancelAsync(jobId, ct);
        return ok ? Ok(new { ok = true }) : NotFound(new { ok = false });
    }

    // tiện ích: xem job failed
    [HttpGet("failed")]
    public async Task<IActionResult> Failed([FromQuery] int? keywordId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var (items, total) = await _svc.ListFailedAsync(keywordId, page, pageSize, ct);
        return Ok(new { total, page, pageSize, items });
    }
}

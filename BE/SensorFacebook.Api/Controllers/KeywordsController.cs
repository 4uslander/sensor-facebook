using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SensorFacebook.Application.Services.KeywordServices;

namespace SensorFacebook.Api.Controllers;

[ApiController]
[Route("api/keywords")]
public sealed class KeywordsController : ControllerBase
{
    private readonly IKeywordService _svc;
    public KeywordsController(IKeywordService svc) => _svc = svc;

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? q = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] bool? active = null,
            [FromQuery] string? sortBy = null,
            [FromQuery(Name = "conditions")] string[]? conditions = null,
            [FromQuery] string? listedTime = null,
            [FromQuery] string? availability = null,
            CancellationToken ct = default)
    {
        var (items, total) = await _svc.ListAsync(
            page, pageSize, q, categoryId, active,
            sortBy, conditions, listedTime, availability, ct);

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Get([FromRoute] int id, CancellationToken ct)
    {
        var item = await _svc.GetAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    //[Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreateKeywordRequest req, CancellationToken ct)
    {
        var id = await _svc.CreateAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    //[Authorize(Roles = "admin")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateKeywordRequest req, CancellationToken ct)
    {
        var ok = await _svc.UpdateAsync(id, req, ct);
        return ok ? Ok(new { ok = true }) : NotFound(new { ok = false });
    }

    [HttpDelete("{id:int}")]
    //[Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(id, ct);
        return ok ? Ok(new { ok = true }) : NotFound(new { ok = false });
    }
}

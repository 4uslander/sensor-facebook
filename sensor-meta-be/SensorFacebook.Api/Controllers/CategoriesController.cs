using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SensorFacebook.Application.Services.CategoryServices;

namespace SensorFacebook.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryService _svc;
    public CategoriesController(ICategoryService svc) => _svc = svc;

    // GET /api/categories?page=1&pageSize=20&q=amply&active=true
    [HttpGet]
    [Authorize] // nếu muốn public, có thể bỏ Authorize
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? q = null, [FromQuery] bool? active = null, CancellationToken ct = default)
    {
        var (items, total) = await _svc.ListAsync(page, pageSize, q, active, ct);
        return Ok(new { total, page, pageSize, items });
    }

    // GET /api/categories/5
    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Get([FromRoute] int id, CancellationToken ct)
    {
        var item = await _svc.GetAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    // POST /api/categories
    [HttpPost]
    //[Authorize(Roles = "admin")] // chỉ admin được tạo
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Name is required" });

        Guid? ownerId = null;
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(sub, out var uid)) ownerId = uid;

        try
        {
            var id = await _svc.CreateAsync(req, ownerId, ct);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    // PUT /api/categories/5
    [HttpPut("{id:int}")]
    //[Authorize(Roles = "admin")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateCategoryRequest req, CancellationToken ct)
    {
        try
        {
            var ok = await _svc.UpdateAsync(id, req, ct);
            return ok ? Ok(new { ok = true }) : NotFound(new { ok = false, error = "Not found" });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    // DELETE /api/categories/5  (soft delete)
    [HttpDelete("{id:int}")]
    //[Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var ok = await _svc.SoftDeleteAsync(id, ct);
        return ok ? Ok(new { ok = true }) : NotFound(new { ok = false, error = "Not found" });
    }

    // POST /api/categories/5/restore
    [HttpPost("{id:int}/restore")]
    //[Authorize(Roles = "admin")]
    public async Task<IActionResult> Restore([FromRoute] int id, CancellationToken ct)
    {
        var ok = await _svc.RestoreAsync(id, ct);
        return ok ? Ok(new { ok = true }) : NotFound(new { ok = false, error = "Not found" });
    }
}

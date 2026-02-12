using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SensorFacebook.Application.Services.ListingServices;

namespace SensorFacebook.Api.Controllers
{
    [ApiController]
    [Route("api/listings")]
    [Authorize] 
    public sealed class ListingsController : ControllerBase
    {
        private readonly IListingQueryService _svc;
        public ListingsController(IListingQueryService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int? keywordId, [FromQuery] string? q,
            [FromQuery] bool? active, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            var (items, total) = await _svc.ListAsync(keywordId, q, active, from, to, page, pageSize, ct);
            return Ok(new { total, page, pageSize, items });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct)
        {
            var x = await _svc.GetAsync(id, ct);
            return x is null ? NotFound() : Ok(x);
        }

        [HttpGet("{id:guid}/changes")]
        public async Task<IActionResult> Changes([FromRoute] Guid id, CancellationToken ct)
        {
            var list = await _svc.GetChangesAsync(id, ct);
            return Ok(list);
        }
    }
}

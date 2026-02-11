using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SensorFacebook.Application.Services.AccountServices.Security; 
using SensorFacebook.Application.Services.ProxyGroups;

namespace SensorFacebook.Api.Controllers
{
    [ApiController]
    [Route("api/proxy-groups")]
    public sealed class ProxyGroupsController : ControllerBase
    {
        private readonly IProxyGroupService _svc;
        private readonly IProxyHealthService _health;
        private readonly ICookieCryptoService _crypto; 

        public ProxyGroupsController(
            IProxyGroupService svc,
            IProxyHealthService health,
            ICookieCryptoService crypto)
        {
            _svc = svc;
            _health = health;
            _crypto = crypto;
        }

        // GET: /api/proxy-groups?page=1&pageSize=20&q=&status=&region=
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> List(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? q = null,
            [FromQuery] string? status = null,
            [FromQuery] string? region = null,
            CancellationToken ct = default)
        {
            var (items, total) = await _svc.ListAsync(page, pageSize, q, status, region, ct);
            return Ok(new { total, page, pageSize, items });
        }

        // GET: /api/proxy-groups/12
        [HttpGet("{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(ProxyGroupDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get([FromRoute] int id, CancellationToken ct)
        {
            var data = await _svc.GetAsync(id, ct);
            return data is null ? NotFound() : Ok(data);
        }

        // POST: /api/proxy-groups
        // body: CreateProxyGroupRequest (protocol/host/port + auth + policy)
        [HttpPost]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateProxyGroupRequest req, CancellationToken ct)
        {
            // Service đã validate protocol/host/port; để chuẩn, bạn có thể thêm ModelState.IsValid ở đây nếu có data annotations
            var id = await _svc.CreateAsync(req, ct);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }

        // PUT: /api/proxy-groups/12
        // body: UpdateProxyGroupRequest
        [HttpPut("{id:int}")]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateProxyGroupRequest req, CancellationToken ct)
        {
            var ok = await _svc.UpdateAsync(id, req, ct);
            return ok ? Ok(new { ok = true }) : NotFound(new { ok = false });
        }

        // DELETE: /api/proxy-groups/12
        [HttpDelete("{id:int}")]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
        {
            var ok = await _svc.DeleteAsync(id, ct);
            return ok ? Ok(new { ok = true }) : NotFound(new { ok = false });
        }

        // GET: /api/proxy-groups/12/health/latest
        [HttpGet("{id:int}/health/latest")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> LatestHealth([FromRoute] int id, CancellationToken ct)
            => Ok(await _health.GetLatestAsync(id, ct));

        // POST: /api/proxy-groups/12/health/check?timeoutMs=8000
        [HttpPost("{id:int}/health/check")]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckNow([FromRoute] int id, [FromQuery] int timeoutMs = 8000, CancellationToken ct = default)
            => Ok(await _health.CheckNowAsync(id, timeoutMs, ct));

        // -----------------------------
        
        // 1) Xem endpoint chuẩn hoá (không kèm auth)
        // GET: /api/proxy-groups/12/endpoint
        [HttpGet("{id:int}/endpoint")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEndpoint([FromRoute] int id, CancellationToken ct)
        {
            var dto = await _svc.GetAsync(id, ct);
            if (dto is null) return NotFound();
            return Ok(new { endpoint = dto.Endpoint });
        }

        // 2) Build full proxy URL (tùy chọn kèm auth) — chỉ admin
        // GET: /api/proxy-groups/12/endpoint/full?includeAuth=true&mask=true
        // includeAuth=false -> luôn chỉ trả endpoint không auth
        // mask=true -> user:****@host:port (không giải mã password)
        [HttpGet("{id:int}/endpoint/full")]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFullEndpoint(
            [FromRoute] int id,
            [FromQuery] bool includeAuth = true,
            [FromQuery] bool mask = true,
            CancellationToken ct = default)
        {
            var dto = await _svc.GetAsync(id, ct);
            if (dto is null) return NotFound();

            // Không có endpoint chuẩn => trả null
            if (string.IsNullOrWhiteSpace(dto.Endpoint))
                return Ok(new { endpoint = (string?)null });

            if (!includeAuth || !dto.HasAuth)
                return Ok(new { endpoint = dto.Endpoint });

            return Ok(new { endpoint = dto.Endpoint, note = "Auth not included in this build. Add a service method to decrypt if needed." });
        }
    }
}
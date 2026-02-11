using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SensorFacebook.Application.Services.AccountServices;
using SensorFacebook.Application.Services.AccountServices.Models;

namespace SensorFacebook.Api.Controllers
{
    [ApiController]
    [Route("api/accounts")]
    //[Authorize(Roles = "admin")]
    public sealed class AccountsController : ControllerBase
    {
        private readonly IAccountService _svc;

        public AccountsController(IAccountService svc)
        {
            _svc = svc;
        }

        // GET /api/accounts?status=&region=&q=&page=&pageSize=
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? status,
            [FromQuery] string? region,
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var (items, total) = await _svc.ListAsync(page, pageSize, q, status, region, ct);
            return Ok(new { total, page, pageSize, items });
        }

        // GET /api/accounts/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct = default)
        {
            var acc = await _svc.GetAsync(id, ct);
            return acc is null ? NotFound() : Ok(acc);
        }

        // POST /api/accounts (create or update)
        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate([FromBody] CreateOrUpdateAccountRequest req, CancellationToken ct = default)
        {
            // Bạn có thể lấy current user id từ JWT nếu cần:
            Guid? userId = null;
            var sub = User.FindFirst("sub")?.Value;
            if (Guid.TryParse(sub, out var parsed)) userId = parsed;

            var id = await _svc.CreateOrUpdateAsync(req, userId, ct);
            return Ok(new { id });
        }

        // PUT /api/accounts/{id} (đổi status)
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateStatus([FromRoute] Guid id, [FromQuery] string status, CancellationToken ct = default)
        {
            var ok = await _svc.UpdateStatusAsync(id, status, ct);
            return ok ? Ok(new { ok = true }) : NotFound(new { ok = false });
        }

        // POST /api/accounts/{id}/lock
        [HttpPost("{id:guid}/lock")]
        public async Task<IActionResult> Lock([FromRoute] Guid id, CancellationToken ct = default)
        {
            var ok = await _svc.LockAsync(id, ct);
            return ok ? Ok(new { ok = true }) : NotFound(new { ok = false });
        }

        // POST /api/accounts/{id}/unlock
        [HttpPost("{id:guid}/unlock")]
        public async Task<IActionResult> Unlock([FromRoute] Guid id, CancellationToken ct = default)
        {
            var ok = await _svc.UnlockAsync(id, ct);
            return ok ? Ok(new { ok = true }) : NotFound(new { ok = false });
        }

        // GET /api/accounts/{id}/events?page=&pageSize=
        [HttpGet("{id:guid}/events")]
        public async Task<IActionResult> Events([FromRoute] Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        {
            var items = await _svc.GetEventsAsync(id, page, pageSize, ct);
            return Ok(new { total = items.Count, page, pageSize, items });
        }
    }
}

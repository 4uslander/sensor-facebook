using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SensorFacebook.Application.Services.AccountServices;
using SensorFacebook.Application.Services.AccountServices.Models;

namespace SensorFacebook.Api.Controllers
{
    [ApiController]
    [Route("api/accounts/select")]
    //[Authorize(Roles = "admin")] // hoặc giữ [Authorize] tùy policy của bạn
    public sealed class AccountSelectorController : ControllerBase
    {
        private readonly IAccountSelector _selector;

        public AccountSelectorController(IAccountSelector selector)
        {
            _selector = selector;
        }

        // ==== DTOs (nằm trong cùng file cho nhanh, có thể tách ra thư mục Dtos nếu thích) ====
        public sealed record AcquireRequest(int ProxyGroupId, int TtlSeconds = 900, LeasePriority Priority = LeasePriority.Normal, string ConsumerKey = "api.select");
        public sealed record AcquireByAccountRequest(Guid AccountId, int ProxyGroupId, int TtlSeconds = 900, string ConsumerKey = "api.select");
        public sealed record ReleaseRequest(bool Checkpoint = false, string? Note = null);

        public sealed record AccountLeaseResponse(
            Guid SessionId,
            Guid AccountId,         
            int? ProxyGroupId,
            string Email,
            string? DisplayName,
            DateTimeOffset ExpiresAt // client biết TTL hiện hành (sau renew)
        );

        /// <summary>
        /// Acquire 1 account rảnh cho ProxyGroup (ưu tiên preferred_proxy_group_id)
        /// </summary>
        [HttpPost("acquire")]
        public async Task<IActionResult> Acquire([FromBody] AcquireRequest req, CancellationToken ct)
        {
            if (req.TtlSeconds <= 0) return BadRequest("TtlSeconds must be > 0");

            var lease = await _selector.AcquireAsync(
                requiredProxyGroupId: req.ProxyGroupId,
                priority: req.Priority,
                consumerKey: req.ConsumerKey,
                ttl: TimeSpan.FromSeconds(req.TtlSeconds),
                ct: ct);

            return Ok(new
            {
                lease.SessionId,
                lease.AccountId,
                lease.ProxyGroupId,
                lease.ExpiresAt
            });
        }

        /// <summary>
        /// Acquire theo AccountId cố định (khi muốn ép chạy đúng account)
        /// </summary>
        [HttpPost("acquire-by-account")]
        public async Task<IActionResult> AcquireByAccount([FromBody] AcquireByAccountRequest req, CancellationToken ct)
        {
            if (req.TtlSeconds <= 0) return BadRequest("TtlSeconds must be > 0");

            var lease = await _selector.AcquireByAccountAsync(
                accountId: req.AccountId,
                proxyGroupId: req.ProxyGroupId,
                consumerKey: req.ConsumerKey,
                ttl: TimeSpan.FromSeconds(req.TtlSeconds),
                ct: ct);

            return Ok(new
            {
                lease.SessionId,
                lease.AccountId,
                lease.ProxyGroupId,
                lease.ExpiresAt
            });
        }

        /// <summary>
        /// Gia hạn 1 session (TTL mới)
        /// </summary>
        [HttpPost("renew/{sessionId:guid}")]
        public async Task<IActionResult> Renew([FromRoute] Guid sessionId, [FromQuery] int ttlSeconds = 900, CancellationToken ct = default)
        {
            if (ttlSeconds <= 0) return BadRequest("ttlSeconds must be > 0");
            var ok = await _selector.RenewAsync(sessionId, TimeSpan.FromSeconds(ttlSeconds), ct);
            if (!ok) return NotFound(new { message = "Session not found or ended." });

            return Ok(new
            {
                ok = true,
                sessionId,
                expiresAt = DateTimeOffset.UtcNow.AddSeconds(ttlSeconds)
            });
        }

        /// <summary>
        /// Kết thúc 1 session (giải phóng account). Checkpoint=true sẽ set trạng thái và cooldown acc.
        /// </summary>
        [HttpPost("release/{sessionId:guid}")]
        public async Task<IActionResult> Release([FromRoute] Guid sessionId, [FromBody] ReleaseRequest req, CancellationToken ct)
        {
            await _selector.ReleaseAsync(sessionId, checkpoint: req.Checkpoint, note: req.Note, ct);
            return Ok(new { ok = true, sessionId });
        }
    }
}

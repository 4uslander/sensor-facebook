using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SensorFacebook.Application.Services.UserServices;
using System.Security.Claims;

namespace SensorFacebook.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    public sealed class UsersController : ControllerBase
    {
        private readonly IUserService _users;
        public UsersController(IUserService users) => _users = users;

        // ========= DTOs =========
        public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
        public sealed record UpdateEmailRequest(string NewEmail);
        public sealed record SetRoleRequest(string RoleName);

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me(CancellationToken ct)
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (sub is null) return Unauthorized();

            var id = Guid.Parse(sub);
            var profile = await _users.GetProfileAsync(id, ct);
            return profile is null ? NotFound() : Ok(profile);
        }

        [HttpPut("me/email")]
        [Authorize]
        public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.NewEmail)) return BadRequest(new { error = "NewEmail is required" });

            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (sub is null) return Unauthorized();

            var ok = await _users.UpdateEmailAsync(Guid.Parse(sub), req.NewEmail, ct);
            return ok ? Ok(new { ok = true }) : BadRequest(new { ok = false, error = "Email đã tồn tại hoặc tài khoản không hợp lệ" });
        }

        [HttpPut("me/password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest(new { error = "Missing password" });

            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (sub is null) return Unauthorized();

            var ok = await _users.ChangePasswordAsync(Guid.Parse(sub), req.CurrentPassword, req.NewPassword, ct);
            return ok ? Ok(new { ok = true }) : BadRequest(new { ok = false, error = "Mật khẩu hiện tại không đúng hoặc tài khoản không hợp lệ" });
        }

        // ====== Admin-only ======
        [HttpPut("{id:guid}/role")]
        //[Authorize(Roles = "admin")]
        public async Task<IActionResult> SetRole([FromRoute] Guid id, [FromBody] SetRoleRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.RoleName)) return BadRequest(new { error = "RoleName is required" });

            var ok = await _users.SetRoleAsync(id, req.RoleName, ct);
            return ok ? Ok(new { ok = true }) : NotFound(new { ok = false, error = "User không tồn tại" });
        }

        [HttpGet]
        //[Authorize(Roles = "admin")]
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? q = null, CancellationToken ct = default)
        {
            var (items, total) = await _users.ListAsync(page, pageSize, q, ct);
            return Ok(new { total, page, pageSize, items });
        }
    }
}

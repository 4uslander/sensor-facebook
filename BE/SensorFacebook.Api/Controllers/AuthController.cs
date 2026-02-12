using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SensorFacebook.Application.Services.AuthServices;
using SensorFacebook.Application.Services.UserServices;
using SensorFacebook.Infrastructure.Models;
using System.Security.Claims;

namespace SensorFacebook.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly IUserService _users;
        private readonly SensorDbContext _db;

        public AuthController(IAuthService auth, IUserService users, SensorDbContext db)
        {
            _auth = auth; _users = users; _db = db;
        }

        // ==== DTOs ====
        public sealed record RegisterRequest(string Email, string Password, string? Role);
        public sealed record LoginRequest(string Email, string Password, string? DeviceInfo);
        public sealed record TokenResponse(string AccessToken, DateTimeOffset AccessExpires,
                                           string RefreshToken, DateTimeOffset RefreshExpires);
        public sealed record RefreshRequest(string RefreshToken);
        public sealed record LogoutRequest(string RefreshToken);

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var (ok, err) = await _auth.RegisterAsync(req.Email, req.Password, req.Role ?? "user", ct);
            return ok ? Ok(new { ok = true }) : BadRequest(new { ok = false, error = err });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var (ok, err, userId, email, role) = await _auth.ValidateUserAsync(req.Email, req.Password, ct);
            if (!ok) return Unauthorized(new { ok = false, error = err });

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (access, accessExp, refresh, refreshExp) =
                await _auth.IssueTokensAsync(userId, email, role, req.DeviceInfo, ip, ct);

            return Ok(new TokenResponse(access, accessExp, refresh, refreshExp));
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.RefreshToken))
                return BadRequest(new { ok = false, error = "RefreshToken is required" });

            var (ok, err, newAccess, exp, newRefresh, refreshExp) =
                await _auth.RotateRefreshAsync(req.RefreshToken, ct);

            return ok
                ? Ok(new TokenResponse(newAccess, exp, newRefresh, refreshExp))
                : Unauthorized(new { ok = false, error = err });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.RefreshToken))
                return BadRequest(new { ok = false, error = "RefreshToken is required" });

            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (sub is null) return Unauthorized();

            var userId = Guid.Parse(sub);
            var ok = await _auth.RevokeRefreshAsync(userId, req.RefreshToken, ct);
            return ok ? Ok(new { ok = true }) : BadRequest(new { ok = false, error = "Not found or already revoked" });
        }

        [HttpGet("protected")]
        [Authorize]
        public IActionResult Protected()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Ok(new { ok = true, userId = sub, role });
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> Profile(CancellationToken ct)
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (sub is null) return Unauthorized();

            var id = Guid.Parse(sub);
            var profile = await _users.GetProfileAsync(id, ct);
            return profile is null ? NotFound() : Ok(profile);
        }
    }
}

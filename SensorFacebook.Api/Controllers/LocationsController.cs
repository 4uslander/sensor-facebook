using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SensorFacebook.Application.Services.LocationServices;
using SensorFacebook.Infrastructure.Models;

namespace SensorFacebook.Api.Controllers;

[ApiController]
[Route("api/locations")]
public sealed class LocationsController : ControllerBase
{
    private readonly SensorDbContext _db;
    private readonly IRadiusNormalizer _normalizer;

    public LocationsController(SensorDbContext db, IRadiusNormalizer normalizer)
    {
        _db = db; _normalizer = normalizer;
    }

    public sealed record NormalizeRequest(int? RadiusKm, string? Policy);
    public sealed record NormalizeResponse(int? RequestedKm, int? NormalizedKm, string PolicyUsed);

    [HttpGet("radius-options")]
    [Authorize]
    public async Task<IActionResult> RadiusOptions([FromQuery] string platform = "facebook_marketplace", CancellationToken ct = default)
    {
        var rows = await _db.PlatformRadiusOptions
            .AsNoTracking()
            .Where(x => x.Platform == platform && (x.Active == true))
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Value)
            .Select(x => new { x.Unit, x.Value })
            .ToListAsync(ct);

        return Ok(new { platform, options = rows });
    }

    [HttpPost("normalize")]
    [Authorize]
    public async Task<IActionResult> Normalize([FromBody] NormalizeRequest req, CancellationToken ct)
    {
        var policy = (req.Policy ?? "platform").Trim().ToLowerInvariant();
        var norm = await _normalizer.NormalizeForFacebookAsync(req.RadiusKm, policy, ct);
        return Ok(new NormalizeResponse(req.RadiusKm, norm, policy));
    }
}

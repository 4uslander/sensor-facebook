using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SensorFacebook.Infrastructure.Models;

namespace SensorFacebook.Application.Services.LocationServices;

public sealed class RadiusNormalizer : IRadiusNormalizer
{
    private readonly SensorDbContext _db;
    public RadiusNormalizer(SensorDbContext db) => _db = db;

    private static int MiToKm(int mi) => (int)Math.Round(mi * 1.609344);

    public async Task<int?> NormalizeForFacebookAsync(int? requestedKm, string? policy, CancellationToken ct = default)
    {
        if (requestedKm is null) return null;

        var p = (policy ?? "platform").Trim().ToLowerInvariant();
        if (p == "fixed") return requestedKm;

        var steps = await _db.PlatformRadiusOptions
            .AsNoTracking()
            .Where(x => x.Platform == "facebook_marketplace" && (x.Active ?? false))
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Value)
            .Select(x => new { x.Unit, x.Value })
            .ToListAsync(ct);

        if (steps.Count == 0)
        {
            var fallbackMi = new[] { 5, 10, 20, 40, 60, 100, 150, 200, 250, 300, 500 };
            steps = fallbackMi.Select(v => new { Unit = "mi", Value = v }).ToList();
        }

        var stepsKm = steps.Select(s => s.Unit == "mi" ? MiToKm(s.Value) : s.Value).OrderBy(v => v).ToArray();
        var best = stepsKm.OrderBy(v => Math.Abs(v - requestedKm.Value)).First();
        return best;
    }
}

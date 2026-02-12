using System.Threading;
using System.Threading.Tasks;

namespace SensorFacebook.Application.Services.LocationServices;

public interface IRadiusNormalizer
{
    Task<int?> NormalizeForFacebookAsync(int? requestedKm, string? policy, CancellationToken ct = default);
} 
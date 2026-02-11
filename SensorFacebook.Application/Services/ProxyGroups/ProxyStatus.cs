using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.ProxyGroups
{
    public static class ProxyStatus
    {
        public const string Active = "active";
        public const string Degraded = "degraded";
        public const string Disabled = "disabled";

        public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            Active, Degraded, Disabled
        };

        public static string NormalizeOrDefault(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return Active;
            var s = status.Trim().ToLowerInvariant();
            if (!Allowed.Contains(s))
                throw new ArgumentException("Status must be active|degraded|disabled");
            return s;
        }

        public static string NormalizeOrKeep(string? status)
        {
            if (status is null) return null!;
            var s = status.Trim().ToLowerInvariant();
            if (!Allowed.Contains(s))
                throw new ArgumentException("Status must be active|degraded|disabled");
            return s;
        }
    }
}

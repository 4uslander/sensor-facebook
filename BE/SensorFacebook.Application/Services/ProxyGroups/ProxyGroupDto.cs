using System.Text.Json;

namespace SensorFacebook.Application.Services.ProxyGroups
{
    public sealed record ProxyGroupDto(
        int Id,
        string Name,
        string? Region,
        string Status,
        string? Protocol,
        string? Host,
        int? Port,

        bool HasAuth,                 // true nếu có user+pass (đã mã hoá)
        string? AuthUsername,         // ✅ trả username để UI hiển thị (không trả password enc)

        string? Provider,
        bool? IsRotating,
        int? MaxConcurrency,
        int? RateLimitRpm,

        JsonElement? MetadataJson,    // ✅ trả metadata dạng JSON cho UI

        DateTimeOffset? LastChecked,
        DateTimeOffset? LastOkAt,
        int? SuccessCount,
        int? FailCount,
        int? LastLatencyMs,
        string? LastStatus,
        string? Endpoint
    );

    // CREATE: bắt buộc protocol/host/port
    public sealed class CreateProxyGroupRequest
    {
        public string Name { get; set; } = default!;
        public string? Region { get; set; }
        public string? Status { get; set; }               // default = "active"
        public string Protocol { get; set; } = default!;  // http|https|socks4|socks5
        public string Host { get; set; } = default!;
        public int Port { get; set; }

        public string? AuthUsername { get; set; }
        public string? AuthPasswordPlain { get; set; }    // chỉ input

        public string? Provider { get; set; }
        public bool? IsRotating { get; set; }
        public int? MaxConcurrency { get; set; }
        public int? RateLimitRpm { get; set; }

        public JsonElement? MetadataJson { get; set; }         // jsonb
    }

    // UPDATE: mọi trường đều optional; password chỉ update khi có giá trị
    public sealed class UpdateProxyGroupRequest
    {
        public string? Name { get; set; }
        public string? Region { get; set; }
        public string? Status { get; set; }               // active|disabled|error|checking

        public string? Protocol { get; set; }
        public string? Host { get; set; }
        public int? Port { get; set; }

        public string? AuthUsername { get; set; }
        public string? AuthPasswordPlain { get; set; }    // nếu null => giữ nguyên

        public string? Provider { get; set; }
        public bool? IsRotating { get; set; }
        public int? MaxConcurrency { get; set; }
        public int? RateLimitRpm { get; set; }

        public JsonElement? MetadataJson { get; set; }         // jsonb
    }
}

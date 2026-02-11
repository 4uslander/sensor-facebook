using System;
using System.Collections.Generic;

namespace SensorFacebook.Infrastructure.Entities;

public partial class ProxyGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    // DEPRECATED: để lại cho tương thích, sẽ không dùng nữa trong code mới
    public string? ProxyUrl { get; set; }

    public string? Region { get; set; }
    public DateTime? LastChecked { get; set; }

    // JSON mở rộng – vẫn giữ (ví dụ: nhãn, tag, ghi chú)
    public string? Metadata { get; set; }

    public string? Status { get; set; } // active|degraded|disabled ...

    // --- Endpoint chuẩn hoá ---
    public string? Protocol { get; set; }     // http|https|socks4|socks5
    public string? Host { get; set; }
    public int? Port { get; set; }

    // --- Auth ---
    public string? AuthUsername { get; set; }
    public string? AuthPasswordEnc { get; set; } // đã mã hoá (AES-GCM)

    // --- Policy/limit/health ---
    public string? Provider { get; set; }
    public bool? IsRotating { get; set; }    // true nếu là proxy xoay IP
    public int? MaxConcurrency { get; set; } // giới hạn context song song
    public int? RateLimitRpm { get; set; }   // limit/phút
    public DateTime? LastOkAt { get; set; }
    public int? SuccessCount { get; set; }
    public int? FailCount { get; set; }

    public virtual ICollection<FbAccount> FbAccounts { get; set; } = new List<FbAccount>();
    public virtual ICollection<FbAccount> PreferredFbAccounts { get; set; } = new List<FbAccount>();

    public virtual ICollection<ProxyHealth> ProxyHealths { get; set; } = new List<ProxyHealth>();

    public virtual ICollection<SearchJob> SearchJobs { get; set; } = new List<SearchJob>();
}

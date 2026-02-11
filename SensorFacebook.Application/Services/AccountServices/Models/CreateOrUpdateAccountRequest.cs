using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.AccountServices.Models
{
    public sealed class CreateOrUpdateAccountRequest
    {
        public Guid? Id { get; set; }                   // null = create, !null = update
        public string Email { get; set; } = default!;
        public string? DisplayName { get; set; }
        public int? ProxyGroupId { get; set; }
        public string? ProfileDir { get; set; }

        // Nếu truyền CookiePlain => sẽ được mã hoá AES-GCM và lưu vào encrypted_cookie
        public string? CookiePlain { get; set; }

        // Optional khi create/update
        public string? Status { get; set; }             // active | locked | banned | checkpointed ...
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Shared.Messaging
{
    public sealed record ListingEnrichMsg(
    Guid ListingId,          // listing cần enrich
    Guid? AccountId = null,  // account fb dùng để mở trang (nếu null: anonymous)
    int? ProxyGroupId = null,// proxy group (nếu null: lấy theo account hoặc để trống)
    string? UrlOverride = null // cho phép override URL nếu DB chưa có
    );
}

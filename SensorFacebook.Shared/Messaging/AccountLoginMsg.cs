using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Shared.Messaging
{
    public sealed record AccountLoginMsg(
    Guid AccountId,          // id tài khoản fb cần đăng nhập/refresh cookie
    int? ProxyGroupId = null,// proxy group áp dụng (nếu null sẽ lấy từ account trong DB)
    string? Note = null      // tuỳ chọn: ghi chú/debug
    );
}

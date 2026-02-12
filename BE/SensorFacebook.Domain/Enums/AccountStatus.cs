using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Domain.Enums
{
    public enum AccountStatus
    {
        Active,
        Suspended,
        Checkpointed,
        Disabled
    }

    public static class AccountStatusExt
    {
        public const string Active = "active";
        public const string Suspended = "suspended";
        public const string Checkpointed = "checkpointed";
        public const string Disabled = "disabled";

        public static string ToDb(this AccountStatus s) => s switch
        {
            AccountStatus.Active => Active,
            AccountStatus.Suspended => Suspended,
            AccountStatus.Checkpointed => Checkpointed,
            AccountStatus.Disabled => Disabled,
            _ => Active
        };

        public static AccountStatus FromDb(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return AccountStatus.Active;
            return s.Trim().ToLowerInvariant() switch
            {
                Active => AccountStatus.Active,
                Suspended => AccountStatus.Suspended,
                Checkpointed => AccountStatus.Checkpointed,
                Disabled => AccountStatus.Disabled,
                _ => throw new ArgumentException($"Invalid account status in DB: {s}")
            };
        }

        // Dùng cho input user
        public static AccountStatus ParseOrDefault(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return AccountStatus.Active;
            return FromDb(s); // reuse rule
        }
    }
}

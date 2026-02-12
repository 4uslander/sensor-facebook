using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.AccountServices.Models
{
    public sealed class AccountSelectorOptions
    {
        public int DefaultLeaseTtlSeconds { get; set; } = 900;           // 15'
        public int HeartbeatRenewSeconds { get; set; } = 300;            // 5'
        public int CooldownHoursAfterCheckpoint { get; set; } = 12;
        public int MaxConcurrentPerProxyGroup { get; set; } = 3;         // 0 = unlimited
    }

    public enum LeasePriority { High = 3, Normal = 2, Low = 1 }
}

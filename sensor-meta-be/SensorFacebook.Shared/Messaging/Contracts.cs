using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Shared.Messaging
{
    public static class Queues
    {
        // Queue names
        public const string SearchLow = "jobs.search.low";
        public const string SearchHigh = "jobs.search.high";
        public const string ProxyHealth = "jobs.proxy.health";
        public const string Notify = "jobs.notify";
        public const string Poison = "jobs.poison";

        // Exchanges
        public const string Exchange = "sensor.jobs";
        public const string DeadLetterExchange = "sensor.dlx";

        // Routing keys (main)
        public const string RK_SearchLow = "search.low";
        public const string RK_SearchHigh = "search.high";
        public const string RK_ProxyHealth = "proxy.health";
        public const string RK_Notify = "notify";
        public const string RK_Poison = "poison";

        // Routing keys (retry)
        public const string RK_SearchLow_Retry_1m = "search.low.retry.1m";
        public const string RK_SearchLow_Retry_5m = "search.low.retry.5m";
        public const string RK_SearchLow_Retry_15m = "search.low.retry.15m";
        public const string RK_SearchLow_Retry_30m = "search.low.retry.30m";

        public const string RK_SearchHigh_Retry_1m = "search.high.retry.1m";
        public const string RK_SearchHigh_Retry_5m = "search.high.retry.5m";
        public const string RK_SearchHigh_Retry_15m = "search.high.retry.15m";
        public const string RK_SearchHigh_Retry_30m = "search.high.retry.30m";
    }

    public record SearchJobMsg(
        Guid JobId,
        int KeywordId,
        Guid? AccountId,
        int? ProxyGroupId,
        string Priority,
        DateTimeOffset ScheduledAt,
        string? CorrelationId
    );

    public record ProxyHealthMsg(
        int ProxyGroupId,
        DateTimeOffset EnqueuedAt
    );

    public record NotifyMsg(
        Guid ListingId,
        Guid UserId,
        string Channel,
        DateTimeOffset EnqueuedAt
    );

}

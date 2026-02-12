using Microsoft.EntityFrameworkCore;
using SensorFacebook.Domain.Enums;
using SensorFacebook.Infrastructure.Entities;

namespace SensorFacebook.Infrastructure.Models;

public partial class SensorDbContext : DbContext
{
    public SensorDbContext()
    {
    }

    public SensorDbContext(DbContextOptions<SensorDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccountEvent> AccountEvents { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<AuthRefreshToken> AuthRefreshTokens { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<DeveloperApp> DeveloperApps { get; set; }

    public virtual DbSet<FbAccount> FbAccounts { get; set; }

    public virtual DbSet<Keyword> Keywords { get; set; }

    public virtual DbSet<Listing> Listings { get; set; }

    public virtual DbSet<ListingChange> ListingChanges { get; set; }

    public virtual DbSet<MetricsAgg> MetricsAggs { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PlatformRadiusOption> PlatformRadiusOptions { get; set; }

    public virtual DbSet<ProxyGroup> ProxyGroups { get; set; }

    public virtual DbSet<ProxyHealth> ProxyHealths { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SearchJob> SearchJobs { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    public virtual DbSet<User> Users { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=SensorFacebook;Username=postgres;Password=phong1230;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            //.HasPostgresEnum("account_status_enum", new[] { "active", "suspended", "checkpointed", "disabled" })
            //.HasPostgresEnum("job_status_enum", new[] { "queued", "running", "done", "failed" })
            //.HasPostgresEnum("mp_availability_enum", new[] { "available", "sold" })
            //.HasPostgresEnum("mp_condition_enum", new[] { "new", "like_new", "good", "fair" })
            //.HasPostgresEnum("mp_listed_time_enum", new[] { "all", "24h", "7d", "30d" })
            //.HasPostgresEnum("mp_sort_enum", new[] { "relevance", "distance_asc", "date_desc", "price_asc", "price_desc" })
            .HasPostgresEnum("notification_status_enum", new[] { "pending", "sent", "failed", "throttled" })
            //.HasPostgresEnum("proxy_status_enum", new[] { "active", "degraded", "disabled" })
            //.HasPostgresEnum("radius_policy_enum", new[] { "auto", "platform", "fixed" })
            .HasPostgresExtension("pg_trgm")
            .HasPostgresExtension("pgcrypto")
            .HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<AccountEvent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("account_events_pkey");

            entity.ToTable("account_events");

            entity.HasIndex(e => new { e.AccountId, e.OccurredAt }, "idx_account_events_acc_time").IsDescending(false, true);

            entity.HasIndex(e => e.EventType, "idx_account_events_type");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.EventType).HasColumnName("event_type");
            entity.Property(e => e.OccurredAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("occurred_at");
            entity.Property(e => e.Payload)
                .HasColumnType("jsonb")
                .HasColumnName("payload");

            entity.HasOne(d => d.Account).WithMany(p => p.AccountEvents)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("account_events_account_id_fkey");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_logs_pkey");

            entity.ToTable("audit_logs");

            entity.HasIndex(e => e.CreatedAt, "idx_audit_logs_created_at").IsDescending();

            entity.HasIndex(e => e.UserId, "idx_audit_logs_user");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Action).HasColumnName("action");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Details)
                .HasColumnType("jsonb")
                .HasColumnName("details");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("audit_logs_user_id_fkey");
        });

        modelBuilder.Entity<AuthRefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("auth_refresh_tokens_pkey");

            entity.ToTable("auth_refresh_tokens");

            entity.HasIndex(e => e.UserId, "idx_refresh_user");

            entity.HasIndex(e => new { e.UserId, e.TokenHash }, "uq_refresh_unique").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceInfo).HasColumnName("device_info");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.JwtId).HasColumnName("jwt_id");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AuthRefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("auth_refresh_tokens_user_id_fkey");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("categories_pkey");

            entity.ToTable("categories");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");

            entity.HasOne(d => d.Owner).WithMany(p => p.Categories)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("categories_owner_id_fkey");
        });

        modelBuilder.Entity<DeveloperApp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("developer_apps_pkey");

            entity.ToTable("developer_apps");

            entity.HasIndex(e => e.AppId, "uq_developer_apps_app_id").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.AppId).HasColumnName("app_id");
            entity.Property(e => e.AppName).HasColumnName("app_name");
            entity.Property(e => e.CertId).HasColumnName("cert_id");
            entity.Property(e => e.LastChecked).HasColumnName("last_checked");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'active'::text")
                .HasColumnName("status");
            entity.Property(e => e.TokenEncrypted).HasColumnName("token_encrypted");
            entity.Property(e => e.Weight)
                .HasDefaultValue(1)
                .HasColumnName("weight");

            entity.HasOne(d => d.Owner).WithMany(p => p.DeveloperApps)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("developer_apps_owner_id_fkey");
        });

        modelBuilder.Entity<FbAccount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("fb_accounts_pkey");
            entity.ToTable("fb_accounts");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");

            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");

            entity.Property(e => e.ProxyGroupId).HasColumnName("proxy_group_id");
            entity.Property(e => e.PreferredProxyGroupId).HasColumnName("preferred_proxy_group_id");

            entity.Property(e => e.ProfileDir).HasColumnName("profile_dir");
            entity.Property(e => e.EncryptedCookie).HasColumnName("encrypted_cookie");

            entity.Property(e => e.CheckpointCount).HasDefaultValue(0).HasColumnName("checkpoint_count");

            entity.Property(e => e.LastCheckpoint).HasColumnName("last_checkpoint");
            entity.Property(e => e.CooldownUntil).HasColumnName("cooldown_until");
            entity.Property(e => e.LastUsedAt).HasColumnName("last_used_at");

            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasColumnType("text")
                .HasDefaultValue(AccountStatus.Active)
                .HasConversion(
                        v => v.ToDb(),               // enum -> string
                        v => AccountStatusExt.FromDb(v) // string -> enum
                 );

            // 🔹 Quan hệ 1: FbAccount.ProxyGroupId ↔ ProxyGroup.FbAccounts
            entity.HasOne(d => d.ProxyGroup)
                .WithMany(p => p.FbAccounts)
                .HasForeignKey(d => d.ProxyGroupId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fb_accounts_proxy_group_id_fkey");

            // 🔹 Quan hệ 2: FbAccount.PreferredProxyGroupId ↔ ProxyGroup.PreferredFbAccounts
            entity.HasOne(d => d.PreferredProxyGroup)
                .WithMany(p => p.PreferredFbAccounts)
                .HasForeignKey(d => d.PreferredProxyGroupId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fb_accounts_preferred_proxy_group_id_fkey");

            entity.HasOne(e => e.CreatedByNavigation)
                .WithMany(u => u.FbAccountsCreated)
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fb_accounts_created_by_fkey");

            entity.HasIndex(e => new { e.Status, e.ProxyGroupId }, "ix_fb_accounts_status_proxy");
            entity.HasIndex(e => e.PreferredProxyGroupId, "ix_fb_accounts_preferred_pg");
            entity.HasIndex(e => e.CooldownUntil, "ix_fb_accounts_cooldown");
            entity.HasIndex(e => e.LastUsedAt, "ix_fb_accounts_last_used_at");
        });


        modelBuilder.Entity<Keyword>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("keywords_pkey");

            entity.ToTable("keywords");

            entity.HasIndex(e => new { e.LocationLat, e.LocationLon }, "idx_keywords_location");

            entity.HasIndex(e => e.RadiusKm, "idx_keywords_radius");

            entity.HasIndex(e => e.Text, "idx_keywords_text_trgm")
                .HasMethod("gin")
                .HasOperators(new[] { "gin_trgm_ops" });

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.LocationLat)
                .HasPrecision(10, 8)
                .HasColumnName("location_lat");
            entity.Property(e => e.LocationLon)
                .HasPrecision(11, 8)
                .HasColumnName("location_lon");
            entity.Property(e => e.NextRun)
                .HasDefaultValueSql("now()")
                .HasColumnName("next_run");
            entity.Property(e => e.Priority)
                .HasDefaultValue(1)
                .HasColumnName("priority");
            entity.Property(e => e.RadiusKm)
            .HasColumnName("radius_km");

            entity.Property(e => e.Text)
            .HasColumnName("text");

            entity.Property(e => e.RadiusPolicy)
                .HasColumnName("radius_policy");

            entity.Property(e => e.SortBy)
                .HasColumnName("sort_by");

            entity.Property(e => e.Conditions)
                .HasColumnName("conditions");

            entity.Property(e => e.ListedTime)
                .HasColumnName("listed_time")
                .HasDefaultValue("all");

            entity.Property(x => x.Availability)
                .HasColumnName("availability");

            entity.HasOne(d => d.Category).WithMany(p => p.Keywords)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("keywords_category_id_fkey");
        });

        modelBuilder.Entity<Listing>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("listings_pkey");

            entity.ToTable("listings");

            entity.HasIndex(e => e.IsActive, "idx_listings_active");

            entity.HasIndex(e => e.KeywordId, "idx_listings_keyword");

            entity.HasIndex(e => e.LastSeen, "idx_listings_last_seen").IsDescending();

            entity.HasIndex(e => e.Location, "idx_listings_location_trgm")
                .HasMethod("gin")
                .HasOperators(new[] { "gin_trgm_ops" });

            entity.HasIndex(e => e.Price, "idx_listings_price");

            entity.HasIndex(e => e.Title, "idx_listings_title_trgm")
                .HasMethod("gin")
                .HasOperators(new[] { "gin_trgm_ops" });

            entity.HasIndex(e => e.ExternalId, "listings_external_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Condition).HasColumnName("condition");
            entity.Property(e => e.Currency).HasColumnName("currency");
            entity.Property(e => e.ExternalId).HasColumnName("external_id");
            entity.Property(e => e.FirstSeen)
                .HasDefaultValueSql("now()")
                .HasColumnName("first_seen");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.KeywordId).HasColumnName("keyword_id");
            entity.Property(e => e.LastSeen)
                .HasDefaultValueSql("now()")
                .HasColumnName("last_seen");
            entity.Property(e => e.Location).HasColumnName("location");
            entity.Property(e => e.Payload)
                .HasColumnType("jsonb")
                .HasColumnName("payload");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Title).HasColumnName("title");

            entity.HasOne(d => d.Account).WithMany(p => p.Listings)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("listings_account_id_fkey");

            entity.HasOne(d => d.Keyword).WithMany(p => p.Listings)
                .HasForeignKey(d => d.KeywordId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("listings_keyword_id_fkey");
        });

        modelBuilder.Entity<ListingChange>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("listing_changes_pkey");

            entity.ToTable("listing_changes");

            entity.HasIndex(e => e.ChangeType, "idx_listing_changes_type");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.ChangeType).HasColumnName("change_type");
            entity.Property(e => e.DetectedByJob).HasColumnName("detected_by_job");
            entity.Property(e => e.ListingId).HasColumnName("listing_id");
            entity.Property(e => e.NewValue)
                .HasColumnType("jsonb")
                .HasColumnName("new_value");
            entity.Property(e => e.OccurredAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("occurred_at");
            entity.Property(e => e.OldValue)
                .HasColumnType("jsonb")
                .HasColumnName("old_value");

            entity.HasOne(d => d.DetectedByJobNavigation).WithMany(p => p.ListingChanges)
                .HasForeignKey(d => d.DetectedByJob)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("listing_changes_detected_by_job_fkey");

            entity.HasOne(d => d.Listing).WithMany(p => p.ListingChanges)
                .HasForeignKey(d => d.ListingId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("listing_changes_listing_id_fkey");
        });

        modelBuilder.Entity<MetricsAgg>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("metrics_agg_pkey");

            entity.ToTable("metrics_agg");

            entity.HasIndex(e => e.Labels, "idx_metrics_labels_gin").HasMethod("gin");

            entity.HasIndex(e => e.MetricName, "idx_metrics_name");

            entity.HasIndex(e => e.Timestamp, "idx_metrics_ts").IsDescending();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Labels)
                .HasColumnType("jsonb")
                .HasColumnName("labels");
            entity.Property(e => e.MetricName).HasColumnName("metric_name");
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("now()")
                .HasColumnName("timestamp");
            entity.Property(e => e.Value).HasColumnName("value");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.HasIndex(e => e.SentAt, "idx_notifications_sent_at");

            entity.HasIndex(e => e.UserId, "idx_notifications_user");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Channel).HasColumnName("channel");
            entity.Property(e => e.Context)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("context");
            entity.Property(e => e.ListingId).HasColumnName("listing_id");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.RuleName).HasColumnName("rule_name");
            entity.Property(e => e.SentAt).HasColumnName("sent_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Listing).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.ListingId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("notifications_listing_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<PlatformRadiusOption>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("platform_radius_options_pkey");

            entity.ToTable("platform_radius_options");

            entity.HasIndex(e => e.Platform, "idx_platform_radius_platform");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.Platform).HasColumnName("platform");
            entity.Property(e => e.SortOrder)
                .HasDefaultValue(0)
                .HasColumnName("sort_order");
            entity.Property(e => e.Unit)
                .HasDefaultValueSql("'mi'::text")
                .HasColumnName("unit");
            entity.Property(e => e.Value).HasColumnName("value");
        });

        modelBuilder.Entity<ProxyGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("proxy_groups_pkey");
            entity.ToTable("proxy_groups");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Region).HasColumnName("region");
            entity.Property(e => e.Status)
                  .HasColumnName("status")
                  .HasColumnType("text")
                  .HasDefaultValue("active");

            entity.Property(e => e.LastChecked).HasColumnName("last_checked");   // timestamptz
            entity.Property(e => e.Metadata).HasColumnType("jsonb").HasColumnName("metadata");

            // deprecated
            entity.Property(e => e.ProxyUrl).HasColumnName("proxy_url");

            // endpoint
            entity.Property(e => e.Protocol).HasColumnName("protocol");
            entity.Property(e => e.Host).HasColumnName("host");
            entity.Property(e => e.Port).HasColumnName("port");

            // auth
            entity.Property(e => e.AuthUsername).HasColumnName("auth_username");
            entity.Property(e => e.AuthPasswordEnc).HasColumnName("auth_password_enc");

            // policy/health
            entity.Property(e => e.Provider).HasColumnName("provider");
            entity.Property(e => e.IsRotating).HasColumnName("is_rotating").HasDefaultValue(false);
            entity.Property(e => e.MaxConcurrency).HasColumnName("max_concurrency").HasDefaultValue(3);
            entity.Property(e => e.RateLimitRpm).HasColumnName("rate_limit_rpm");
            entity.Property(e => e.LastOkAt).HasColumnName("last_ok_at");       // timestamptz
            entity.Property(e => e.SuccessCount).HasColumnName("success_count").HasDefaultValue(0);
            entity.Property(e => e.FailCount).HasColumnName("fail_count").HasDefaultValue(0);

            // indexes
            entity.HasIndex(e => new { e.Host, e.Port, e.Protocol }, "ix_proxy_groups_endpoint");
            entity.HasIndex(e => e.Status, "ix_proxy_groups_status");
            entity.HasIndex(e => e.LastOkAt, "ix_proxy_groups_last_ok");
        });

        modelBuilder.Entity<ProxyHealth>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("proxy_health_pkey");

            entity.ToTable("proxy_health");

            entity.HasIndex(e => e.CheckedAt, "idx_proxy_health_checked_at").IsDescending();

            entity.HasIndex(e => e.ProxyGroupId, "idx_proxy_health_group");

            entity.HasIndex(e => new { e.ProxyGroupId, e.CheckedAt }, "idx_proxy_health_group_time").IsDescending(false, true);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CheckedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("checked_at");
            entity.Property(e => e.Healthy)
                .HasDefaultValue(true)
                .HasColumnName("healthy");
            entity.Property(e => e.LastStatus).HasColumnName("last_status");
            entity.Property(e => e.LatencyMs).HasColumnName("latency_ms");
            entity.Property(e => e.ProxyGroupId).HasColumnName("proxy_group_id");

            entity.HasOne(d => d.ProxyGroup).WithMany(p => p.ProxyHealths)
                .HasForeignKey(d => d.ProxyGroupId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("proxy_health_proxy_group_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.Name, "roles_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<SearchJob>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("search_jobs_pkey");

            entity.ToTable("search_jobs");

            entity.HasIndex(e => new { e.KeywordId, e.TimeBucket180s }, "uq_jobs_keyword_180s_partial")
                .IsUnique()
                .HasFilter("(status = ANY (ARRAY['queued'::job_status_enum, 'running'::job_status_enum]))");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Attempts)
                .HasDefaultValue(0)
                .HasColumnName("attempts");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.FinishedAt).HasColumnName("finished_at");
            entity.Property(e => e.KeywordId).HasColumnName("keyword_id");
            entity.Property(e => e.LastErrorAt).HasColumnName("last_error_at");
            entity.Property(e => e.ProxyGroupId).HasColumnName("proxy_group_id");
            entity.Property(e => e.ResultCount)
                .HasDefaultValue(0)
                .HasColumnName("result_count");
            entity.Property(e => e.ScheduledAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("scheduled_at");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.TimeBucket180s).HasColumnName("time_bucket_180s");

            entity.Property(e => e.Status)
    .HasColumnName("status")
    .HasColumnType("text")
    .HasDefaultValue(JobStatus.queued)
    .HasConversion(
        v => v.ToString(),                              // enum -> "queued"
        v => Enum.Parse<JobStatus>(v, ignoreCase: true) // "queued" -> enum
    );

            entity.HasOne(d => d.Account).WithMany(p => p.SearchJobs)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("search_jobs_account_id_fkey");

            entity.HasOne(d => d.Keyword).WithMany(p => p.SearchJobs)
                .HasForeignKey(d => d.KeywordId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("search_jobs_keyword_id_fkey");

            entity.HasOne(d => d.ProxyGroup).WithMany(p => p.SearchJobs)
                .HasForeignKey(d => d.ProxyGroupId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("search_jobs_proxy_group_id_fkey");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sessions_pkey");
            entity.ToTable("sessions");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");

            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.ProxyGroupId).HasColumnName("proxy_group_id");

            entity.Property(e => e.ConsumerKey)
                .HasColumnName("consumer_key");

            entity.Property(e => e.StartedAt)
                .HasColumnName("started_at")
                .HasDefaultValueSql("now()");

            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.EndedAt).HasColumnName("ended_at");
            entity.Property(e => e.Note).HasColumnName("note");

            entity.HasOne(d => d.Account).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("sessions_account_id_fkey");
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("subscriptions_pkey");

            entity.ToTable("subscriptions");

            entity.HasIndex(e => new { e.UserId, e.Active }, "idx_subscriptions_user_active");

            entity.HasIndex(e => new { e.UserId, e.KeywordId }, "uq_subscriptions_user_keyword").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.KeywordId).HasColumnName("keyword_id");
            entity.Property(e => e.NotifyChannel).HasColumnName("notify_channel");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Keyword).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.KeywordId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("subscriptions_keyword_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("subscriptions_user_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("users_role_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

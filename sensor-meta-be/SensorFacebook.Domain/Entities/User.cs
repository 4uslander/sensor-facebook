using System;
using System.Collections.Generic;

namespace SensorFacebook.Infrastructure.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<AuthRefreshToken> AuthRefreshTokens { get; set; } = new List<AuthRefreshToken>();

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual ICollection<DeveloperApp> DeveloperApps { get; set; } = new List<DeveloperApp>();

    public virtual ICollection<FbAccount> FbAccountsCreated { get; set; } = new List<FbAccount>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}

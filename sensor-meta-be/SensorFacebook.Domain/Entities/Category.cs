using System;
using System.Collections.Generic;

namespace SensorFacebook.Infrastructure.Entities;

public partial class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? OwnerId { get; set; }

    public bool? Active { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Keyword> Keywords { get; set; } = new List<Keyword>();

    public virtual User? Owner { get; set; }
}

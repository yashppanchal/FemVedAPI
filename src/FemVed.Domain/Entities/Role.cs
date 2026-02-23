namespace FemVed.Domain.Entities;

/// <summary>Platform role — seed data: Admin(1), Expert(2), User(3).</summary>
public class Role
{
    /// <summary>Role identifier (smallint, matches seed values 1/2/3).</summary>
    public short Id { get; set; }

    /// <summary>Human-readable role name, e.g. "Admin".</summary>
    public string Name { get; set; } = string.Empty;

    // Navigation
    /// <summary>Users assigned this role.</summary>
    public ICollection<User> Users { get; set; } = new List<User>();
}

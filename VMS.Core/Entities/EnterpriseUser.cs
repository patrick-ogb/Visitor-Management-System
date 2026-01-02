namespace VMS.Core.Entities;

public class EnterpriseUser
{
    public int EnterpriseUserId { get; set; }
    public int OnePortalUserId { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? RoleName { get; set; }
    public bool IsActive { get; set; }
    public int OnePortalEnterpriseId { get; set; }
    public int? EnterpriseId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public virtual Enterprise? Enterprise { get; set; }
}












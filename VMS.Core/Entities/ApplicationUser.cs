using Microsoft.AspNetCore.Identity;

namespace VMS.Core.Entities;

public class ApplicationUser : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? EnterpriseId { get; set; }
    
    // Navigation property
    public Enterprise? Enterprise { get; set; }
}

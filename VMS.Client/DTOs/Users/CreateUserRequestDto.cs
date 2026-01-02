using System.ComponentModel.DataAnnotations;

namespace VMS.Client.DTOs.Users;

public class CreateUserRequestDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    [Required(ErrorMessage = "Role is required")]
    public string Role { get; set; } = string.Empty;

    public int? EnterpriseId { get; set; }
}


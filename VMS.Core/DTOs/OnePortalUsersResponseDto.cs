using System.Text.Json.Serialization;

namespace VMS.Core.DTOs;

public class OnePortalUsersResponseDto
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }
    
    [JsonPropertyName("data")]
    public List<OnePortalUserDto>? Data { get; set; }
    
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class OnePortalUserDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("emailAddress")]
    public string EmailAddress { get; set; } = string.Empty;
    
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;
    
    [JsonPropertyName("roleName")]
    public string? RoleName { get; set; }
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}


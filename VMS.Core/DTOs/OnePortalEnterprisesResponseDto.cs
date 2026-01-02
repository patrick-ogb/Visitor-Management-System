using System.Text.Json.Serialization;

namespace VMS.Core.DTOs;

public class OnePortalEnterprisesResponseDto
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }
    
    [JsonPropertyName("data")]
    public List<OnePortalEnterpriseDto>? Data { get; set; }
    
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class OnePortalEnterpriseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}


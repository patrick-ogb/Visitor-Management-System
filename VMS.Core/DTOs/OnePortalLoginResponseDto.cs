namespace VMS.Core.DTOs;

public class OnePortalLoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class OnePortalResponseDto
{
    public bool Ok { get; set; }
    public OnePortalAuthDataDto? Data { get; set; }
    public string? Error { get; set; }
}

public class OnePortalAuthDataDto
{
    public string TokenType { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public int RefreshExpiresIn { get; set; }
    public OnePortalMfaDto Mfa { get; set; } = new();
    public bool MustChangePassword { get; set; }
    public OnePortalTenantDto Tenant { get; set; } = new();
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public OnePortalNavigationDto Navigation { get; set; } = new();
}

public class OnePortalMfaDto
{
    public bool Required { get; set; }
    public string? Method { get; set; }
    public string? ChallengeId { get; set; }
}

public class OnePortalTenantDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? Image { get; set; }
}

public class OnePortalNavigationDto
{
    public List<OnePortalPortalDto> Portals { get; set; } = new();
}

public class OnePortalPortalDto
{
    public int PortalId { get; set; }
    public string PortalCode { get; set; } = string.Empty;
    public string PortalName { get; set; } = string.Empty;
    public string IconSvg { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public List<OnePortalSubMenuDto> SubMenus { get; set; } = new();
}

public class OnePortalSubMenuDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string IconSvg { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}














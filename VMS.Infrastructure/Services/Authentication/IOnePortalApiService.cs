using VMS.Core.DTOs;

namespace VMS.Infrastructure.Services.Authentication;

public interface IOnePortalApiService
{
    Task<OnePortalResponseDto> LoginAsync(string email, string password);
    Task<BaseResponseDto<List<EnterpriseDto>>> GetEnterprisesAsync(string onePortalToken);
    Task<BaseResponseDto<List<UserDto>>> GetUsersByEnterpriseAsync(int enterpriseId, string onePortalToken);
}


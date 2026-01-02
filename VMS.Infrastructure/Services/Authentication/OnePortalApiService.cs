using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VMS.Core.DTOs;

namespace VMS.Infrastructure.Services.Authentication;

public class OnePortalApiService : IOnePortalApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OnePortalApiService> _logger;
    private readonly string _apiUrl;

    public OnePortalApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OnePortalApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiUrl = configuration["OnePortal:ApiUrl"] 
            ?? throw new InvalidOperationException("OnePortal API URL is not configured");
        
        _httpClient.BaseAddress = new Uri(_apiUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<OnePortalResponseDto> LoginAsync(string email, string password)
    {
        try
        {
            var request = new OnePortalLoginRequestDto
            {
                Email = email,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
            
            // Read response content as string first (can only read stream once)
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Try to parse the response even if status code is not successful
            // OnePortal may return error responses with ok: false in JSON format
            OnePortalResponseDto? result = null;
            try
            {
                // Deserialize from the string content
                result = System.Text.Json.JsonSerializer.Deserialize<OnePortalResponseDto>(
                    responseContent, 
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception parseEx)
            {
                _logger.LogWarning(parseEx, "Failed to parse OnePortal response as JSON. Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, responseContent);
                
                // If status is not successful and we can't parse, throw HttpRequestException for fallback
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"OnePortal API returned status {response.StatusCode}: {responseContent}", parseEx);
                }
                
                // If status is successful but parsing failed, throw HttpRequestException for fallback
                throw new HttpRequestException($"OnePortal API returned invalid response: {responseContent}", parseEx);
            }

            if (result == null)
            {
                _logger.LogWarning("OnePortal API returned null response");
                throw new HttpRequestException("OnePortal API returned null response");
            }

            // If we successfully parsed the response, return it even if status code indicates error
            // The caller can check the 'ok' field to determine success
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OnePortal API returned non-success status {StatusCode} but valid response. Ok: {Ok}, Error: {Error}", 
                    response.StatusCode, result.Ok, result.Error);
            }
            
            return result;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "OnePortal API request timed out");
            throw new HttpRequestException("OnePortal API request timed out", ex);
        }
        catch (HttpRequestException)
        {
            // Re-throw HttpRequestException to allow fallback to Identity authentication
            throw;
        }
        catch (InvalidOperationException ex)
        {
            // Wrap InvalidOperationException in HttpRequestException to allow fallback
            _logger.LogError(ex, "OnePortal API operation failed");
            throw new HttpRequestException($"OnePortal API error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling OnePortal API");
            // Wrap in HttpRequestException to allow fallback to Identity authentication
            throw new HttpRequestException($"OnePortal API error: {ex.Message}", ex);
        }
    }

    public async Task<BaseResponseDto<List<EnterpriseDto>>> GetEnterprisesAsync(string onePortalToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/enterprises");
            
            if (!string.IsNullOrWhiteSpace(onePortalToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", onePortalToken);
            }

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("OnePortal API GetEnterprises failed with status {StatusCode}: {Error}", 
                    response.StatusCode, errorContent);
                
                return new BaseResponseDto<List<EnterpriseDto>>
                {
                    Success = false,
                    Message = $"OnePortal API returned status {response.StatusCode}",
                    Errors = new List<string> { errorContent }
                };
            }

            // Deserialize OnePortal response structure
            var onePortalResponse = await response.Content.ReadFromJsonAsync<OnePortalEnterprisesResponseDto>();
            
            if (onePortalResponse == null || !onePortalResponse.Ok || onePortalResponse.Data == null)
            {
                _logger.LogWarning("OnePortal API GetEnterprises returned invalid response. Ok: {Ok}, Error: {Error}", 
                    onePortalResponse?.Ok ?? false, onePortalResponse?.Error);
                
                return new BaseResponseDto<List<EnterpriseDto>>
                {
                    Success = false,
                    Message = onePortalResponse?.Error ?? "OnePortal API returned invalid response",
                    Errors = new List<string> { onePortalResponse?.Error ?? "Invalid response" }
                };
            }

            // Map OnePortal DTOs to our internal DTOs
            var enterprises = onePortalResponse.Data.Select(e => new EnterpriseDto
            {
                OnePortalId = e.Id,
                Code = e.Code,
                Name = e.Name,
                Address = null, // Not provided by OnePortal
                ContactEmail = null, // Not provided by OnePortal
                ContactPhone = null, // Not provided by OnePortal
                IsActive = e.IsActive
            }).ToList();

            return new BaseResponseDto<List<EnterpriseDto>>
            {
                Success = true,
                Data = enterprises
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "OnePortal API GetEnterprises request timed out");
            return new BaseResponseDto<List<EnterpriseDto>>
            {
                Success = false,
                Message = "OnePortal API request timed out",
                Errors = new List<string> { ex.Message }
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OnePortal API GetEnterprises request failed");
            return new BaseResponseDto<List<EnterpriseDto>>
            {
                Success = false,
                Message = "OnePortal API request failed",
                Errors = new List<string> { ex.Message }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling OnePortal API GetEnterprises");
            return new BaseResponseDto<List<EnterpriseDto>>
            {
                Success = false,
                Message = "Unexpected error calling OnePortal API",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<BaseResponseDto<List<UserDto>>> GetUsersByEnterpriseAsync(int enterpriseId, string onePortalToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/users/by-enterprise/{enterpriseId}");
            
            if (!string.IsNullOrWhiteSpace(onePortalToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", onePortalToken);
            }

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("OnePortal API GetUsersByEnterprise failed with status {StatusCode}: {Error}", 
                    response.StatusCode, errorContent);
                
                return new BaseResponseDto<List<UserDto>>
                {
                    Success = false,
                    Message = $"OnePortal API returned status {response.StatusCode}",
                    Errors = new List<string> { errorContent }
                };
            }

            // Deserialize OnePortal response structure
            var onePortalResponse = await response.Content.ReadFromJsonAsync<OnePortalUsersResponseDto>();
            
            if (onePortalResponse == null || !onePortalResponse.Ok || onePortalResponse.Data == null)
            {
                _logger.LogWarning("OnePortal API GetUsersByEnterprise returned invalid response. Ok: {Ok}, Error: {Error}", 
                    onePortalResponse?.Ok ?? false, onePortalResponse?.Error);
                
                return new BaseResponseDto<List<UserDto>>
                {
                    Success = false,
                    Message = onePortalResponse?.Error ?? "OnePortal API returned invalid response",
                    Errors = new List<string> { onePortalResponse?.Error ?? "Invalid response" }
                };
            }

            // Map OnePortal DTOs to our internal DTOs
            var users = onePortalResponse.Data.Select(u =>
            {
                // Parse fullName into FirstName and LastName
                var nameParts = u.FullName.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                var firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
                var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

                // Convert roleName to Roles list
                var roles = new List<string>();
                if (!string.IsNullOrWhiteSpace(u.RoleName))
                {
                    roles.Add(u.RoleName);
                }

                return new UserDto
                {
                    UserId = u.Id,
                    Email = u.EmailAddress,
                    FirstName = firstName,
                    LastName = lastName,
                    Phone = null, // Not provided by OnePortal
                    Roles = roles,
                    EnterpriseId = null, // Not provided by OnePortal
                    EnterpriseName = null, // Not provided by OnePortal
                    IsActive = u.IsActive,
                    CreatedAt = DateTime.UtcNow // Default since not provided by OnePortal
                };
            }).ToList();

            return new BaseResponseDto<List<UserDto>>
            {
                Success = true,
                Data = users
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "OnePortal API GetUsersByEnterprise request timed out");
            return new BaseResponseDto<List<UserDto>>
            {
                Success = false,
                Message = "OnePortal API request timed out",
                Errors = new List<string> { ex.Message }
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OnePortal API GetUsersByEnterprise request failed");
            return new BaseResponseDto<List<UserDto>>
            {
                Success = false,
                Message = "OnePortal API request failed",
                Errors = new List<string> { ex.Message }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling OnePortal API GetUsersByEnterprise");
            return new BaseResponseDto<List<UserDto>>
            {
                Success = false,
                Message = "Unexpected error calling OnePortal API",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}


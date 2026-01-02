using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMS.API.Common.Models;
using VMS.Core.DTOs;
using VMS.Core.Entities;
using VMS.Infrastructure.Data;
using VMS.Infrastructure.Services.Authentication;

namespace VMS.API.Commands.EnterpriseUsers;

public class SyncEnterpriseUsersCommandHandler : IRequestHandler<SyncEnterpriseUsersCommand, BaseResponse<SyncEnterpriseUsersResponseDto>>
{
    private readonly IOnePortalApiService _onePortalApiService;
    private readonly VmsDbContext _context;
    private readonly ILogger<SyncEnterpriseUsersCommandHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SyncEnterpriseUsersCommandHandler(
        IOnePortalApiService onePortalApiService,
        VmsDbContext context,
        ILogger<SyncEnterpriseUsersCommandHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _onePortalApiService = onePortalApiService;
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<BaseResponse<SyncEnterpriseUsersResponseDto>> Handle(SyncEnterpriseUsersCommand request, CancellationToken cancellationToken)
    {
        var response = new SyncEnterpriseUsersResponseDto();

        try
        {
            // Extract OnePortal token from JWT claims
            var user = _httpContextAccessor.HttpContext?.User;
            var onePortalToken = user?.FindFirst("onePortalToken")?.Value;

            if (string.IsNullOrWhiteSpace(onePortalToken))
            {
                return BaseResponse<SyncEnterpriseUsersResponseDto>.ErrorResponse(
                    "OnePortal token not found. User must be logged in via OnePortal to sync enterprise users.");
            }

            // Find local enterprise by OnePortalId
            var localEnterprise = await _context.Enterprises
                .FirstOrDefaultAsync(e => e.OnePortalId == request.OnePortalEnterpriseId, cancellationToken);

            // Get users from OnePortal
            var onePortalResult = await _onePortalApiService.GetUsersByEnterpriseAsync(request.OnePortalEnterpriseId, onePortalToken);

            if (!onePortalResult.Success || onePortalResult.Data == null)
            {
                return BaseResponse<SyncEnterpriseUsersResponseDto>.ErrorResponse(
                    onePortalResult.Message ?? "Failed to retrieve users from OnePortal",
                    onePortalResult.Errors);
            }

            var onePortalUsers = onePortalResult.Data;
            response.Total = onePortalUsers.Count;

            // Get existing enterprise users from local database, keyed by OnePortalUserId
            // (filtered by OnePortalEnterpriseId, so OnePortalUserId is unique within this enterprise)
            var existingUsers = await _context.EnterpriseUsers
                .Where(eu => eu.OnePortalEnterpriseId == request.OnePortalEnterpriseId)
                .ToDictionaryAsync(
                    eu => eu.OnePortalUserId,
                    cancellationToken);

            foreach (var onePortalUser in onePortalUsers)
            {
                try
                {
                    if (existingUsers.TryGetValue(onePortalUser.UserId, out var existingUser))
                    {
                        // Update existing user
                        existingUser.EmailAddress = onePortalUser.Email;
                        existingUser.FullName = $"{onePortalUser.FirstName} {onePortalUser.LastName}".Trim();
                        existingUser.RoleName = onePortalUser.Roles?.FirstOrDefault();
                        existingUser.IsActive = onePortalUser.IsActive;
                        existingUser.UpdatedAt = DateTime.UtcNow;
                        
                        // Update EnterpriseId if local enterprise found
                        if (localEnterprise != null)
                        {
                            existingUser.EnterpriseId = localEnterprise.EnterpriseId;
                        }
                        
                        response.Updated++;
                    }
                    else
                    {
                        // Create new enterprise user
                        var newEnterpriseUser = new EnterpriseUser
                        {
                            OnePortalUserId = onePortalUser.UserId,
                            EmailAddress = onePortalUser.Email,
                            FullName = $"{onePortalUser.FirstName} {onePortalUser.LastName}".Trim(),
                            RoleName = onePortalUser.Roles?.FirstOrDefault(),
                            IsActive = onePortalUser.IsActive,
                            OnePortalEnterpriseId = request.OnePortalEnterpriseId,
                            EnterpriseId = localEnterprise?.EnterpriseId,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.EnterpriseUsers.Add(newEnterpriseUser);
                        response.Created++;
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Error syncing user {onePortalUser.UserId} ({onePortalUser.Email}): {ex.Message}";
                    _logger.LogError(ex, errorMsg);
                    response.Errors.Add(errorMsg);
                }
            }

            // Save changes
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Enterprise users sync completed. Created: {Created}, Updated: {Updated}, Total: {Total}",
                response.Created, response.Updated, response.Total);

            return BaseResponse<SyncEnterpriseUsersResponseDto>.SuccessResponse(
                response,
                $"Successfully synced {response.Total} users. Created: {response.Created}, Updated: {response.Updated}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during enterprise users sync");
            return BaseResponse<SyncEnterpriseUsersResponseDto>.ErrorResponse(
                $"An error occurred during enterprise users sync: {ex.Message}",
                new List<string> { ex.Message });
        }
    }
}


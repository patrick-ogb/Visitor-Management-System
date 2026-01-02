using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMS.API.Common.Models;
using VMS.Core.DTOs;
using VMS.Core.Entities;
using VMS.Infrastructure.Data;
using VMS.Infrastructure.Services.Authentication;

namespace VMS.API.Commands.Enterprises;

public class SyncEnterprisesCommandHandler : IRequestHandler<SyncEnterprisesCommand, BaseResponse<SyncEnterprisesResponseDto>>
{
    private readonly IOnePortalApiService _onePortalApiService;
    private readonly VmsDbContext _context;
    private readonly ILogger<SyncEnterprisesCommandHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SyncEnterprisesCommandHandler(
        IOnePortalApiService onePortalApiService,
        VmsDbContext context,
        ILogger<SyncEnterprisesCommandHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _onePortalApiService = onePortalApiService;
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<BaseResponse<SyncEnterprisesResponseDto>> Handle(SyncEnterprisesCommand request, CancellationToken cancellationToken)
    {
        var response = new SyncEnterprisesResponseDto();

        try
        {
            // Extract OnePortal token from JWT claims
            var user = _httpContextAccessor.HttpContext?.User;
            var onePortalToken = user?.FindFirst("onePortalToken")?.Value;

            if (string.IsNullOrWhiteSpace(onePortalToken))
            {
                return BaseResponse<SyncEnterprisesResponseDto>.ErrorResponse(
                    "OnePortal token not found. User must be logged in via OnePortal to sync enterprises.");
            }

            // Get enterprises from OnePortal
            var onePortalResult = await _onePortalApiService.GetEnterprisesAsync(onePortalToken);

            if (!onePortalResult.Success || onePortalResult.Data == null)
            {
                return BaseResponse<SyncEnterprisesResponseDto>.ErrorResponse(
                    onePortalResult.Message ?? "Failed to retrieve enterprises from OnePortal",
                    onePortalResult.Errors);
            }

            var onePortalEnterprises = onePortalResult.Data;
            response.Total = onePortalEnterprises.Count;

            // Get existing enterprises from local database, keyed by OnePortalId
            var existingEnterprises = await _context.Enterprises
                .Where(e => e.OnePortalId.HasValue)
                .ToDictionaryAsync(e => e.OnePortalId!.Value, cancellationToken);

            foreach (var onePortalEnterprise in onePortalEnterprises)
            {
                try
                {
                    if (!onePortalEnterprise.OnePortalId.HasValue)
                    {
                        var errorMsg = $"OnePortal enterprise missing OnePortalId: {onePortalEnterprise.Name}";
                        _logger.LogWarning(errorMsg);
                        response.Errors.Add(errorMsg);
                        continue;
                    }

                    if (existingEnterprises.TryGetValue(onePortalEnterprise.OnePortalId.Value, out var existingEnterprise))
                    {
                        // Update existing enterprise
                        existingEnterprise.Code = onePortalEnterprise.Code;
                        existingEnterprise.Name = onePortalEnterprise.Name;
                        existingEnterprise.IsActive = onePortalEnterprise.IsActive;
                        existingEnterprise.UpdatedAt = DateTime.UtcNow;
                        response.Updated++;
                    }
                    else
                    {
                        // Create new enterprise
                        var newEnterprise = new Enterprise
                        {
                            OnePortalId = onePortalEnterprise.OnePortalId,
                            Code = onePortalEnterprise.Code,
                            Name = onePortalEnterprise.Name,
                            IsActive = onePortalEnterprise.IsActive,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Enterprises.Add(newEnterprise);
                        response.Created++;
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Error syncing enterprise {onePortalEnterprise.OnePortalId} ({onePortalEnterprise.Name}): {ex.Message}";
                    _logger.LogError(ex, errorMsg);
                    response.Errors.Add(errorMsg);
                }
            }

            // Save changes
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Enterprise sync completed. Created: {Created}, Updated: {Updated}, Total: {Total}",
                response.Created, response.Updated, response.Total);

            return BaseResponse<SyncEnterprisesResponseDto>.SuccessResponse(
                response,
                $"Successfully synced {response.Total} enterprises. Created: {response.Created}, Updated: {response.Updated}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during enterprise sync");
            return BaseResponse<SyncEnterprisesResponseDto>.ErrorResponse(
                $"An error occurred during enterprise sync: {ex.Message}",
                new List<string> { ex.Message });
        }
    }
}


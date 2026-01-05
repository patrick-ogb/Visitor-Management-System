using MediatR;
using Microsoft.EntityFrameworkCore;
using VMS.API.Common.Models;
using VMS.Core.Enums;
using VMS.Infrastructure.Data;

namespace VMS.API.Queries.Invitations;

public class GetInvitationsQueryHandler : IRequestHandler<GetInvitationsQuery, BaseResponse<PagedResponse<InvitationDto>>>
{
    private readonly VmsDbContext _context;

    public GetInvitationsQueryHandler(VmsDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<PagedResponse<InvitationDto>>> Handle(GetInvitationsQuery request, CancellationToken cancellationToken)
    {
       
        try
        {
            var query = _context.GuestInvitations
           .Include(i => i.Guest)
           .Include(i => i.Vehicles)
           .Include(i => i.Enterprise)
           .AsQueryable();

            // Apply filters
            if (request.HostId.HasValue)
            {
                query = query.Where(i => i.HostId == request.HostId.Value);
            }

            if (request.EnterpriseId.HasValue)
            {
                query = query.Where(i => i.EnterpriseId == request.EnterpriseId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (Enum.TryParse<InvitationStatus>(request.Status, out var status))
                {
                    query = query.Where(i => i.Status == status);
                    
                    // For Approved status, also filter out already checked-in invitations
                    // This ensures GateAdmin only sees approved invitations awaiting check-in
                    if (status == InvitationStatus.Approved)
                    {
                        query = query.Where(i => i.CheckedInAt == null);
                    }
                }
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(i => i.ExpectedArrival >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(i => i.ExpectedArrival <= request.ToDate.Value);
            }

            // Apply search filter for GuestName or InvitationNo
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.Trim().ToLower();
                query = query.Where(i => 
                    (i.Guest != null && i.Guest.Name != null && i.Guest.Name.ToLower().Contains(searchTerm)) ||
                    (i.InvitationNo != null && i.InvitationNo.ToLower().Contains(searchTerm))
                );
            }

            // Note: We use null-conditional operators in the mapping below to handle null Guest navigation properties
            // This prevents NullReferenceException when accessing Guest properties for orphaned records
            // Filtering i.Guest != null here could cause SQL translation issues if database schema is not up to date

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination and ordering
            var invitations = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var invitationDtos = invitations.Select(i => new InvitationDto
            {
                GuestInvitationId = i.GuestInvitationId,
                InvitationNo = i.InvitationNo,
                GuestId = i.GuestId,
                GuestName = i.Guest?.Name ?? string.Empty,
                GuestPhone = i.Guest?.PhoneNumber ?? string.Empty,
                GuestEmail = i.Guest?.Email,
                HostName = i.HostName,
                HostEmail = i.HostEmail,
                EnterpriseName = i.Enterprise?.Name,
                Status = i.Status.ToString(),
                ExpectedArrival = i.ExpectedArrival,
                ExpectedDeparture = i.ExpectedDeparture,
                NumberOfAdditionalGuests = i.NumberOfAdditionalGuests,
                IsProxyInvitation = i.IsProxyInvitation,
                InvitedOnBehalfOf = i.InvitedOnBehalfOf,
                PurposeOfInvitation = i.PurposeOfInvitation,
                Vehicles = i.Vehicles.Select(v => new VehicleDto
                {
                    PlateNumber = v.PlateNumber,
                    Model = v.Model,
                    Color = v.Color
                }).ToList(),
                CreatedAt = i.CreatedAt,
                CheckedInAt = i.CheckedInAt
            }).ToList();

            var response = new PagedResponse<InvitationDto>
            {
                Items = invitationDtos,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };

            return BaseResponse<PagedResponse<InvitationDto>>.SuccessResponse(response);

        }
        catch (Exception ex)
        {

            throw;
        }
    }
}



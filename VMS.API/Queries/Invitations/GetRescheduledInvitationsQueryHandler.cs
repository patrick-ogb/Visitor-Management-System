using MediatR;
using Microsoft.EntityFrameworkCore;
using VMS.API.Common.Models;
using VMS.Core.Enums;
using VMS.Infrastructure.Data;

namespace VMS.API.Queries.Invitations;

public class GetRescheduledInvitationsQueryHandler : IRequestHandler<GetRescheduledInvitationsQuery, BaseResponse<PagedResponse<InvitationDto>>>
{
    private readonly VmsDbContext _context;

    public GetRescheduledInvitationsQueryHandler(VmsDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<PagedResponse<InvitationDto>>> Handle(GetRescheduledInvitationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.GuestInvitations
                .Include(i => i.Guest)
                .Include(i => i.Vehicles)
                .Include(i => i.Enterprise)
                .Where(i => i.IsRescheduled == true) // Only rescheduled invitations
                .AsQueryable();

            // Filter by HostId (required for rescheduled invitations)
            if (request.HostId.HasValue)
            {
                query = query.Where(i => i.HostId == request.HostId.Value);
            }

            // Filter by status if provided
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (Enum.TryParse<InvitationStatus>(request.Status, out var status))
                {
                    query = query.Where(i => i.Status == status);
                }
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

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination and ordering (order by RescheduledAt descending to show most recent first)
            var invitations = await query
                .OrderByDescending(i => i.RescheduledAt ?? i.CreatedAt)
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
                CheckedInAt = i.CheckedInAt,
                IsRescheduled = i.IsRescheduled,
                RescheduledById = i.RescheduledById,
                RescheduledAt = i.RescheduledAt
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
            return BaseResponse<PagedResponse<InvitationDto>>.ErrorResponse($"Error loading rescheduled invitations: {ex.Message}");
        }
    }
}


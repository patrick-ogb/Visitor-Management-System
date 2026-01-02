using MediatR;
using Microsoft.EntityFrameworkCore;
using VMS.API.Common.Models;
using VMS.Core.Enums;
using VMS.Infrastructure.Data;

namespace VMS.API.Queries.Gate;

public class SearchGuestQueryHandler : IRequestHandler<SearchGuestQuery, BaseResponse<SearchGuestResultDto>>
{
    private readonly VmsDbContext _context;

    public SearchGuestQueryHandler(VmsDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<SearchGuestResultDto>> Handle(SearchGuestQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        // Filter by date: show approved invitations that haven't been checked in and departure is today or future
        var query = _context.GuestInvitations
            .Include(i => i.Guest)
                .ThenInclude(g => g.Vehicles)
            .Include(i => i.Enterprise)
            .Where(i => i.Status == InvitationStatus.Approved &&
                       !i.CheckedInAt.HasValue &&
                       i.ExpectedDeparture.Date >= today)
            .AsQueryable();

        // Build OR conditions for flexible search
        // If a search term is provided, search across Name, InvitationNo, and VehiclePlateNumber
        var hasSearchTerm = !string.IsNullOrWhiteSpace(request.Name) || 
                           !string.IsNullOrWhiteSpace(request.InvitationNo) || 
                           !string.IsNullOrWhiteSpace(request.VehiclePlateNumber);

        if (hasSearchTerm)
        {
            query = query.Where(i =>
                // Search by name (guest name - can match firstname or lastname)
                (!string.IsNullOrWhiteSpace(request.Name) && i.Guest.Name.ToLower().Contains(request.Name.ToLower())) ||
                // Search by InvitationNo
                (!string.IsNullOrWhiteSpace(request.InvitationNo) && i.InvitationNo.ToLower().Contains(request.InvitationNo.ToLower())) ||
                // Search by vehicle plate number
                (!string.IsNullOrWhiteSpace(request.VehiclePlateNumber) && i.Guest.Vehicles.Any(v => 
                    v.PlateNumber.ToLower().Contains(request.VehiclePlateNumber.ToLower())))
            );
        }

        // Additional vehicle filters (model, color) - these are AND conditions
        if (!string.IsNullOrWhiteSpace(request.VehicleModel))
        {
            query = query.Where(i => i.Guest.Vehicles.Any(v => 
                v.Model != null && v.Model.ToLower().Contains(request.VehicleModel.ToLower())));
        }

        if (!string.IsNullOrWhiteSpace(request.VehicleColor))
        {
            query = query.Where(i => i.Guest.Vehicles.Any(v => 
                v.Color != null && v.Color.ToLower().Contains(request.VehicleColor.ToLower())));
        }

        var invitation = await query.FirstOrDefaultAsync(cancellationToken);

        if (invitation == null)
        {
            return BaseResponse<SearchGuestResultDto>.SuccessResponse(new SearchGuestResultDto
            {
                Found = false,
                IsApproved = false
            });
        }

        var isApproved = invitation.Status == InvitationStatus.Approved;

        var guestDto = new ApprovedGuestDto
        {
            GuestInvitationId = invitation.GuestInvitationId,
            GuestName = invitation.Guest.Name,
            PhoneNumber = invitation.Guest.PhoneNumber,
            Email = invitation.Guest.Email,
            HostName = invitation.HostName,
            EnterpriseName = invitation.Enterprise?.Name,
            ExpectedArrival = invitation.ExpectedArrival,
            ExpectedDeparture = invitation.ExpectedDeparture,
            NumberOfAdditionalGuests = invitation.NumberOfAdditionalGuests,
            PurposeOfInvitation = invitation.PurposeOfInvitation,
            Vehicles = invitation.Guest.Vehicles.Select(v => new VehicleDto
            {
                PlateNumber = v.PlateNumber,
                Model = v.Model,
                Color = v.Color
            }).ToList(),
            IsCheckedIn = invitation.CheckedInAt.HasValue,
            CheckedInAt = invitation.CheckedInAt
        };

        return BaseResponse<SearchGuestResultDto>.SuccessResponse(new SearchGuestResultDto
        {
            Found = true,
            IsApproved = isApproved,
            Guest = isApproved ? guestDto : null
        });
    }
}





using MediatR;
using Microsoft.EntityFrameworkCore;
using VMS.API.Common.Models;
using VMS.Core.Enums;
using VMS.Infrastructure.Data;

namespace VMS.API.Queries.Gate;

public class GetApprovedGuestsQueryHandler : IRequestHandler<GetApprovedGuestsQuery, BaseResponse<List<ApprovedGuestDto>>>
{
    private readonly VmsDbContext _context;

    public GetApprovedGuestsQueryHandler(VmsDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<List<ApprovedGuestDto>>> Handle(GetApprovedGuestsQuery request, CancellationToken cancellationToken)
    {
        var date = request.Date?.Date ?? DateTime.UtcNow.Date;

        // Show approved invitations that:
        // 1. Are approved
        // 2. Haven't been checked in yet (CheckedInAt is null)
        // 3. Expected departure is today or in the future (not past)
        var invitations = await _context.GuestInvitations
            .Include(i => i.Guest)
                .ThenInclude(g => g.Vehicles)
            .Include(i => i.Enterprise)
            .Where(i => i.Status == InvitationStatus.Approved &&
                       !i.CheckedInAt.HasValue &&
                       i.ExpectedDeparture.Date >= date)
            .OrderBy(i => i.ExpectedArrival)
            .ToListAsync(cancellationToken);

        var guests = invitations.Select(i => new ApprovedGuestDto
        {
            GuestInvitationId = i.GuestInvitationId,
            GuestName = i.Guest.Name,
            PhoneNumber = i.Guest.PhoneNumber,
            Email = i.Guest.Email,
            HostName = i.HostName,
            EnterpriseName = i.Enterprise?.Name,
            ExpectedArrival = i.ExpectedArrival,
            ExpectedDeparture = i.ExpectedDeparture,
            NumberOfAdditionalGuests = i.NumberOfAdditionalGuests,
            PurposeOfInvitation = i.PurposeOfInvitation,
            Vehicles = i.Guest.Vehicles.Select(v => new VehicleDto
            {
                PlateNumber = v.PlateNumber,
                Model = v.Model,
                Color = v.Color
            }).ToList(),
            IsCheckedIn = i.CheckedInAt.HasValue,
            CheckedInAt = i.CheckedInAt
        }).ToList();

        return BaseResponse<List<ApprovedGuestDto>>.SuccessResponse(guests);
    }
}





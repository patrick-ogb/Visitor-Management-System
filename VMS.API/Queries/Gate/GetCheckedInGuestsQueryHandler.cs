using MediatR;
using Microsoft.EntityFrameworkCore;
using VMS.API.Common.Models;
using VMS.Core.Enums;
using VMS.Infrastructure.Data;

namespace VMS.API.Queries.Gate;

public class GetCheckedInGuestsQueryHandler : IRequestHandler<GetCheckedInGuestsQuery, BaseResponse<List<CheckedInGuestDto>>>
{
    private readonly VmsDbContext _context;

    public GetCheckedInGuestsQueryHandler(VmsDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<List<CheckedInGuestDto>>> Handle(GetCheckedInGuestsQuery request, CancellationToken cancellationToken)
    {
        // Fetch guests that are checked in (CheckedInAt is not null)
        var invitations = await _context.GuestInvitations
            .Include(i => i.Guest)
                .ThenInclude(g => g.Vehicles)
            .Include(i => i.Enterprise)
            .Where(i => i.CheckedInAt.HasValue && i.Status == InvitationStatus.CheckedIn)
            .OrderByDescending(i => i.CheckedInAt)
            .ToListAsync(cancellationToken);

        var guests = invitations.Select(i => new CheckedInGuestDto
        {
            GuestInvitationId = i.GuestInvitationId,
            InvitationNo = i.InvitationNo,
            GuestName = i.Guest.Name,
            PhoneNumber = i.Guest.PhoneNumber,
            Email = i.Guest.Email,
            HostName = i.HostName,
            EnterpriseName = i.Enterprise?.Name,
            CheckedInAt = i.CheckedInAt!.Value,
            ExpectedArrival = i.ExpectedArrival,
            ExpectedDeparture = i.ExpectedDeparture,
            Vehicles = i.Guest.Vehicles.Select(v => new VehicleDto
            {
                PlateNumber = v.PlateNumber,
                Model = v.Model,
                Color = v.Color
            }).ToList()
        }).ToList();

        return BaseResponse<List<CheckedInGuestDto>>.SuccessResponse(guests);
    }
}


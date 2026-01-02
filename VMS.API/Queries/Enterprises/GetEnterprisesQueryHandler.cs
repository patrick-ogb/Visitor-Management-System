using MediatR;
using Microsoft.EntityFrameworkCore;
using VMS.API.Common.Models;
using VMS.Infrastructure.Data;

namespace VMS.API.Queries.Enterprises;

public class GetEnterprisesQueryHandler : IRequestHandler<GetEnterprisesQuery, BaseResponse<List<EnterpriseDto>>>
{
    private readonly VmsDbContext _context;

    public GetEnterprisesQueryHandler(VmsDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<List<EnterpriseDto>>> Handle(GetEnterprisesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Enterprises.AsQueryable();

        if (request.IsActive.HasValue)
        {
            query = query.Where(e => e.IsActive == request.IsActive.Value);
        }

        var enterprises = await query
            .OrderBy(e => e.OnePortalId)
            .Select(e => new EnterpriseDto
            {
                EnterpriseId = e.EnterpriseId,
                OnePortalId = e.OnePortalId,
                Code = e.Code,
                Name = e.Name,
                Address = e.Address,
                ContactEmail = e.ContactEmail,
                ContactPhone = e.ContactPhone,
                IsActive = e.IsActive
            })
            .ToListAsync(cancellationToken);

        return BaseResponse<List<EnterpriseDto>>.SuccessResponse(enterprises);
    }
}


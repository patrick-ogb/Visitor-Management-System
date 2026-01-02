using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Commands.Approvals;

public class ApproveRequestCommand : IRequest<BaseResponse<bool>>
{
    public string Type { get; set; } = string.Empty; // "Invitation" or "WalkIn"
    public int RequestId { get; set; }
    public bool IsApproved { get; set; }
    public string? Comments { get; set; }
    public int ApprovedBy { get; set; }
}





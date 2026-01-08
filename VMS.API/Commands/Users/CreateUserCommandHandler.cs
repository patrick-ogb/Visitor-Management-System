using MediatR;
using Microsoft.AspNetCore.Identity;
using VMS.API.Common.Models;
using VMS.Core.Entities;
using VMS.Infrastructure.Services;

namespace VMS.API.Commands.Users;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, BaseResponse<CreateUserResponseDto>>
{
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateUserCommandHandler(
        IUserService userService,
        IEmailService emailService,
        UserManager<ApplicationUser> userManager)
    {
        _userService = userService;
        _emailService = emailService;
        _userManager = userManager;
    }

    public async Task<BaseResponse<CreateUserResponseDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.CreateUserAsync(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.Phone,
                request.Role,
                request.EnterpriseId,
                request.CreatedBy);

            // Send welcome email
            await _emailService.SendWelcomeEmailAsync(user.Email!, user.FirstName, request.Password);

            var response = new CreateUserResponseDto
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = request.Role
            };

            return BaseResponse<CreateUserResponseDto>.SuccessResponse(response, "User created successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<CreateUserResponseDto>.ErrorResponse(ex.Message);
        }
    }
}




















using Account.Application.Features.Account.ChekEmailAvailability;
using Account.Application.Features.Account.SendEmailVerification;
using AccountApi.Authorization;
using AccountApi.Models.RequestModels;
using AccountApi.Models.ResponseModels;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AccountController(IMediator mediator) : ControllerBase
{

    [AuthorizeApiKeyOnly]
    //[AllowAnonymous]
    [HttpPost("check-email-availability")]
    public async Task<IActionResult> CheckEmailAvailability([FromBody] ChekEmailAvailabilityRequest model)
    {
        var res = await mediator.Send(new ChekEmailAvailabilityCommand(model.Email));
        if (!res.IsSuccess)
            return BadRequest(res.Errors);

        return Ok(new ChekEmailAvailabilityResponse { IsAvailable = res.Value });
    }

    [AllowAnonymous]
    //[Authorize]
    [HttpGet("send-email-verification-link")]
    public async Task<IActionResult> SendEmailVerification()
    {
        var email = User.FindFirst("email")?.Value;
#if DEBUG
        email = "user@example.com";

#endif
        var cmd = new SendEmailVerificationCommand(email);
        var res = await mediator.Send(cmd);
        if (!res.IsSuccess)
            return BadRequest(res.Errors);

        return Ok("Not implemented");
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest model)
    {
        //flow 
        //1 send to email otp code to user
        //2 check otp code
        //3 confirm email in db and keycloak
        throw new NotImplementedException();
    }
}
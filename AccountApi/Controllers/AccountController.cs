using Account.Application.Features.Account.ChekEmailAvailability;
using Account.Application.Features.Account.ConfirmEmail;
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

        return Ok("Check your email for verification link");
    }

    [AllowAnonymous]
    [HttpGet("verify-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
    {
        var cmd = new ConfirmEmailCommand(token);
        var res = await mediator.Send(cmd);
        if (!res.IsSuccess)
            return BadRequest(res.Errors);
        return Redirect(res.IsSuccess ? "/email-verified.html" : "/email-verification-failed.html");
    }
}
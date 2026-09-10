using Account.Application.Features.Account.AccountInfo;
using Account.Application.Features.Account.ChekEmailAvailability;
using Account.Application.Features.Account.ConfirmEmail;
using Account.Application.Features.Account.SendEmailVerification;
using Account.Application.Features.Account.Setup2Fa;
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
    [HttpPost("check-email-availability")]
    public async Task<IActionResult> CheckEmailAvailability([FromBody] ChekEmailAvailabilityRequest model)
    {
        var res = await mediator.Send(new ChekEmailAvailabilityCommand(model.Email));
        if (!res.IsSuccess)
            return NotFound(res.Errors);

        return Ok(new ChekEmailAvailabilityResponse { IsAvailable = res.Value });
    }

    [Authorize]
    [HttpGet("send-email-verification-link")]
    public async Task<IActionResult> SendEmailVerification()
    {
        var email = User.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(email))
            return NotFound("Email in credentials not found");

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

    [AuthorizeJWT]
    [HttpGet("get-account-info")]
    public async Task<IActionResult> GetAccountInfo()
    {
        var email = User.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(email))
            return NotFound("Email in credentials not found");

        var resInfo = await mediator.Send(new AccountInfoCommand(email));
        if (!resInfo.IsSuccess)
            return BadRequest(resInfo.Errors);
        
        return Ok(resInfo.Value);
    }
    
    [AuthorizeJWT]
    [HttpPost("2fa/enable-setup")]
    public async Task<IActionResult> Enable2FaSetup([FromBody] Enable2FaSetupRequest model)
    {
        var email = User.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(email))
            return NotFound("Email in credentials not found");

        var cmd = new Setup2FaCommand(email, model.IsEnableTwoFactor);
        var res = await mediator.Send(cmd);
        if (!res.IsSuccess)
            return BadRequest(res.Errors);

        return Ok();
    }
}
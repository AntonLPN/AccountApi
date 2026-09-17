using Account.Application.Features.Account.AccountInfo;
using Account.Application.Features.Account.ChekEmailAvailability;
using Account.Application.Features.Account.ConfirmEmail;
using Account.Application.Features.Account.IsEmailVerified;
using Account.Application.Features.Account.SendEmailVerification;
using Account.Application.Features.Account.Setup2Fa;
using AccountApi.Authorization;
using AccountApi.Models.RequestModels;
using AccountApi.Models.ResponseModels;
using Ardalis.Result;
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
    [ProducesResponseType<ChekEmailAvailabilityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("check-email-availability")]
    public async Task<IActionResult> CheckEmailAvailability([FromBody] ChekEmailAvailabilityRequest model)
    {
        var res = await mediator.Send(new ChekEmailAvailabilityCommand(model.Email));
        if (!res.IsSuccess)
            return BadRequest(res.Errors);

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
    [ProducesResponseType<IsEmailVerifiedResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("is-email-verified")]
    public async Task<IActionResult> IsEmailVerified(CancellationToken cancellationToken)
    {
        var email = User.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(email))
            return NotFound("Email in credentials not found");

        var res = await mediator.Send(new IsEmailVerifiedCommand(email), cancellationToken);
        if (res.Status == ResultStatus.NotFound)
            return NotFound(res.Errors);
        if (!res.IsSuccess)
            return BadRequest(res.Errors);

        return Ok(res.Value);
    }

    [AuthorizeJWT]
    [ProducesResponseType<AccountInfoResult>(StatusCodes.Status200OK)]
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPatch("2fa/setup")]
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
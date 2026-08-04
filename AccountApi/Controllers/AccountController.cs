using Account.Application.Features.Account.ChekEmailAvailability;
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
    [HttpGet("check-email-availability")]
    public async Task<IActionResult> ChekEmailAvailability([FromBody] ChekEmailAvailabilityRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var res = await mediator.Send(new ChekEmailAvailabilityCommand(model.Email));
        if (!res.IsSuccess)
            return BadRequest(res.Errors);

        return Ok(new ChekEmailAvailabilityResponse { IsAvailable = res.Value });
    }

    [AllowAnonymous]
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        //flow 
        //1 send to email otp code to user
        //2 check otp code
        //3 confirm email in db and keycloak
        throw new NotImplementedException();
    }
}
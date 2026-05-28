using Account.Application.Features.Account.Register;
using AccountApi.Models.RequestModels;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterModelRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var regCmd = new RegisterCommand(model.Email, model.Password);
        var res = await mediator.Send(regCmd);
        if(!res.IsSuccess)
            return BadRequest(res.Errors);
        
        return Ok(res.Value);
    }
}
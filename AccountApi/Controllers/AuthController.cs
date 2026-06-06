using Account.Application.Features.Account.Login;
using Account.Application.Features.Account.Logout;
using Account.Application.Features.Account.Register;
using AccountApi.Models.RequestModels;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountApi.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController(IMediator mediator) : ControllerBase
{
    
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

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginModelRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var loginCmd = new LoginCommand(model.Email, model.Password, ipAddress, userAgent);
        var res = await mediator.Send(loginCmd);

        if (res.Status == Ardalis.Result.ResultStatus.Unauthorized)
            return Unauthorized();
        if (!res.IsSuccess)
            return BadRequest(res.Errors);

        return Ok(res.Value);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutModelRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var logoutCmd = new LogoutCommand(model.Email, model.RefreshToken, ipAddress, userAgent);
        var res = await mediator.Send(logoutCmd);

        if (res.Status == Ardalis.Result.ResultStatus.Unauthorized)
            return Unauthorized();
        if (!res.IsSuccess)
            return BadRequest(res.Errors);

        return Ok();
    }
}
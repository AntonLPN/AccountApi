using System.Security.Claims;
using Account.Application.Features.Account.Login;
using Account.Application.Features.Account.Logout;
using Account.Application.Features.Account.Register;
using AccountApi.Helpers;
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

        var regCmd = new RegisterCommand(model.Email, model.Password, model.ReferralCode);
        var res = await mediator.Send(regCmd);
        if (!res.IsSuccess)
            return BadRequest(res.Errors);
        
        SetRefreshTokenCookie(res.Value.Token.RefreshToken);
        return Ok(res.Value);
    }

    [AllowAnonymous]
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

        SetRefreshTokenCookie(res.Value.Token.RefreshToken);
        return Ok(res.Value);
    }


    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutModelRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(emailClaim))
            return BadRequest("User not found");
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var logoutCmd = new LogoutCommand(emailClaim, model.RefreshToken, ipAddress, userAgent);
        var res = await mediator.Send(logoutCmd);

        if (res.Status == Ardalis.Result.ResultStatus.Unauthorized)
            return Unauthorized();
        if (!res.IsSuccess)
            return BadRequest(res.Errors);

        return Ok();
    }

    private void SetRefreshTokenCookie(string? refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return;

        Response.Cookies.Append("refreshToken", refreshToken, WebSettings.GetCookieOptions());
    }
}
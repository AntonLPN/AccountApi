using Account.Application.Features.Account.Authentication;
using AccountApi.Helpers;
using AccountApi.Models.RequestModels;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthenticationController(IMediator mediator) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthenticationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromBody] RefreshModelRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var cmd = new AuthenticationCommand(model.RefreshToken);
        var res = await mediator.Send(cmd);
        if (!res.IsSuccess)
            return BadRequest(res.Errors);
        
        SetRefreshTokenCookie(res.Value.Token?.RefreshToken);
        return Ok(res.Value);
    }
    [AllowAnonymous]
    [HttpPost("refresh-cookie")]
    public async Task<IActionResult> RefreshFromCookie()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized();

        var res = await mediator.Send(new AuthenticationCommand(refreshToken));

        if (res.Status == Ardalis.Result.ResultStatus.Unauthorized)
            return Unauthorized();

        if (!res.IsSuccess)
            return BadRequest(res.Errors);

        var token = res.Value.Token;
        if (token is null)
            return StatusCode(StatusCodes.Status502BadGateway);

        SetRefreshTokenCookie(token.RefreshToken);
        return Ok(new
        {
            accessToken = token.AccessToken,
            tokenType = token.TokenType,
            expiresIn = token.ExpiresIn
        });
    }
    
    private void SetRefreshTokenCookie(string? refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return;
        var cookieOptions = new CookieOptions
        {
            //HttpOnly — защита от XSS (JS в браузере не имеет доступа к cookie)
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            //Время жизни cookie (должно совпадать с временем жизни Refresh-токена)
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/api/Authentication", // Куки будут посылаться только на запросы к авторизации
            IsEssential = true
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}
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
    [HttpPost("refresh")]
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
    
    private void SetRefreshTokenCookie(string? refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return;

        Response.Cookies.Append("refreshToken", refreshToken, WebSettings.GetCookieOptions());
    }
}
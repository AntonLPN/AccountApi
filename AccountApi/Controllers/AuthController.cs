using Account.Application.Features.Account.Login;
using Account.Application.Features.Account.Logout;
using Account.Application.Features.Account.OtpCodeVerification;
using Account.Application.Features.Account.ProviderLogin;
using Account.Application.Features.Account.ProvidersRegister;
using Account.Application.Features.Account.Register;
using Account.Domain.Enums;
using AccountApi.Authorization;
using AccountApi.Helpers;
using AccountApi.Models.RequestModels;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AccountApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [AuthorizeApiKeyOnly]
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterModelRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var regCmd = new RegisterCommand(model.Email, model.Password, model.ReferralCode, ipAddress, userAgent);
        var res = await mediator.Send(regCmd);
        if (!res.IsSuccess)
            return BadRequest(res.Errors);

        SetRefreshTokenCookie(res.Value?.Token?.RefreshToken);
        return Ok(res.Value);
    }

    [AuthorizeApiKeyOnly]
    [HttpPost("google-register")]
    [ProducesResponseType(typeof(ProviderRegisterResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GoogleRegister([FromBody] GoogleRegisterRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var regCmd = new ProviderRegisterCommand(model.Token, model.ReferralCode, AuthProviders.Google, ipAddress,
            userAgent);
        var res = await mediator.Send(regCmd);
        if (!res.IsSuccess)
            return BadRequest(res.Errors);

        SetRefreshTokenCookie(res.Value?.Token?.RefreshToken);
        return Ok(res.Value);
    }

    [AuthorizeApiKeyOnly]
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

        if (!res.IsSuccess)
            return Unauthorized();

        if (!res.Value.IsMfaRequired)
            SetRefreshTokenCookie(res.Value?.Token?.RefreshToken);
        return Ok(res.Value);
    }

    [AuthorizeApiKeyOnly]
    [HttpPost("google-login")]
    [ProducesResponseType(typeof(ProviderLoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] GoogleLoginModelRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var loginCmd = new ProviderLoginCommand(model.Token, AuthProviders.Google, ipAddress, userAgent);
        var res = await mediator.Send(loginCmd);

        if (res.Status == Ardalis.Result.ResultStatus.Unauthorized)
            return Unauthorized();
        if (!res.IsSuccess)
            return BadRequest(res.Errors);

        SetRefreshTokenCookie(res.Value?.Token?.RefreshToken);
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
        var emailClaim = User.FindFirst("email")?.Value;
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

    [PreAuthOnly]
    [HttpPost("otp-code-verification")]
    [ProducesResponseType(typeof(OtpConfirmationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EnableRateLimiting(RateLimiterPolices.VerifyOtpPolicy)]
    public async Task<IActionResult> OtpCodeVerification([FromBody] OtpCodeVerificationRequestModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var emailClaim = User.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(emailClaim))
            return BadRequest("User not found");
        
        var cmd = new OtpCodeVerificationCommand(emailClaim, model.OtpCode);
        var res = await mediator.Send(cmd);
        if (!res.IsSuccess)
            return BadRequest(res.Errors);

        return Ok(res.Value);
    }
    //TODO : Add change password

    [AuthorizeApiKeyOnly]
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        //TODO send otp code to user
        //
        // var cmd = new ForgotPasswordCommand(model.Email);
        // var res = await mediator.Send(cmd);
        // if (!res.IsSuccess)
        //     return BadRequest(res.Errors);

        return Ok();
    }
    
    
    [PreAuthOnly]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var emailClaim = User.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(emailClaim))
            return BadRequest("User not found");

        // var cmd = new ChangePasswordCommand(emailClaim, model.NewPassword);
        // var res = await mediator.Send(cmd);
        // if (!res.IsSuccess)
        //     return BadRequest(res.Errors);

        return Ok();
    }
    
    //TODO : Add forgot password
    //TODO : Add reset password


    private void SetRefreshTokenCookie(string? refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return;

        Response.Cookies.Append("refreshToken", refreshToken, WebSettings.GetCookieOptions());
    }
}

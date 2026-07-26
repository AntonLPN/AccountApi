using Account.Domain.Interfaces;
using AccountApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountApi.Controllers.Test;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TestController(IAuthService authService, ILogger<TestController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("test")]
    public async Task<IActionResult> Index()
    {
        logger.LogInformation("Test endpoint called");
        return Ok();
    }
    
    [AuthorizeApiKeyOnly]
    [HttpGet("test-allow-only-api-key")]
    public IActionResult TestApiKey()
    {
        var claims = User.Claims;
        return Ok();
    }
    
    [AuthorizeApiKeyOnly]
    [HttpGet("test-allow")]
    public IActionResult TestApiKeyAndJwt()
    {
        var claims = User.Claims;
        return Ok();
    }
}
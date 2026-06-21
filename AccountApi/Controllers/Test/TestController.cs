using AccountApi.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountApi.Controllers.Test;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TestController : ControllerBase
{
    [AuthorizeMfaRequired]
    [HttpGet("test-acr-2")]
    public IActionResult TeestAcr2()
    {
        var claims = User.Claims;
        return Ok();
    }
    
    [AuthorizePreAuthOnly]
    [HttpGet("test-acr-1")]
    public IActionResult TestAcr1()
    {
        var claims = User.Claims;
        return Ok();
    }
}
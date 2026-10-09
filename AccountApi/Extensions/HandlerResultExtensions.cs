using Ardalis.Result;
using Microsoft.AspNetCore.Mvc;

namespace AccountApi.Extensions;

public static class HandlerResultExtensions
{
    public static IActionResult HandleResult<T>(
        this ControllerBase controller,
        Result<T> result)
    {
        return result.Status switch
        {
            ResultStatus.Ok => controller.Ok(result.Value),
            ResultStatus.Error => controller.BadRequest(result.Errors),
            ResultStatus.NotFound => controller.NotFound(result.Errors),
            ResultStatus.Unauthorized => controller.Unauthorized(result.Errors),
            ResultStatus.Forbidden => controller.Forbid(),
            ResultStatus.Conflict => controller.Conflict(result.Errors),
            _ => controller.StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.")
        };
    }
}
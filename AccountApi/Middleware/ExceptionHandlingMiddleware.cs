using System.Net;
using System.Text;
using FluentValidation;

namespace AccountApi.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception e)
        {
            await HandleExceptionAsync(context, e);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var (statusCode, message, errorCode) = DetermineErrorResponse(exception);

        var correlationId = context.TraceIdentifier;
        LogException(exception, statusCode, errorCode, correlationId);

        context.Response.StatusCode = (int)statusCode;

        var response = new ErrorResponse
        {
            ErrorCode = errorCode,
            Message = message,
            CorrelationId = correlationId,
            Details = GetExceptionDetails(exception, environment),
            Timestamp = DateTime.UtcNow
        };

        //Fluent Validation
        if (exception is ValidationException validationEx)
        {
            response.ValidationErrors = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
        }

        return context.Response.WriteAsJsonAsync(response);
    }

    private void LogException(
        Exception exception,
        HttpStatusCode statusCode,
        string errorCode,
        string correlationId)
    {
        var logLevel = statusCode >= HttpStatusCode.InternalServerError
            ? LogLevel.Error
            : LogLevel.Warning;

        logger.Log(
            logLevel,
            exception,
            "Exception occurred | " +
            "ErrorCode: {ErrorCode} | " +
            "StatusCode: {StatusCode} | " +
            "CorrelationId: {CorrelationId} | " +
            "ExceptionType: {ExceptionType} | " +
            "Message: {Message}",
            errorCode,
            (int)statusCode,
            correlationId,
            exception.GetType().Name,
            exception.Message);
    }

    private (HttpStatusCode statusCode, string message, string errorCode) DetermineErrorResponse(Exception exception)
    {
        return exception switch
        {
            // FluentValidation
            ValidationException =>
                (HttpStatusCode.BadRequest, "Validation failed", "VALIDATION_ERROR"),

            ArgumentNullException ane =>
                (HttpStatusCode.BadRequest, $"Required parameter is missing: {ane.ParamName}", "MISSING_ARGUMENT"),

            ArgumentException ae =>
                (HttpStatusCode.BadRequest, ae.Message, "INVALID_ARGUMENT"),

            InvalidOperationException ioe =>
                (HttpStatusCode.BadRequest, ioe.Message, "INVALID_OPERATION"),

            UnauthorizedAccessException =>
                (HttpStatusCode.Unauthorized, "Unauthorized access", "UNAUTHORIZED"),

            NotImplementedException =>
                (HttpStatusCode.NotImplemented, "This feature is not implemented yet", "NOT_IMPLEMENTED"),

            KeyNotFoundException knfe =>
                (HttpStatusCode.NotFound, knfe.Message, "RESOURCE_NOT_FOUND"),

            OperationCanceledException =>
                (HttpStatusCode.RequestTimeout, "Request timed out", "REQUEST_TIMEOUT"),

            TimeoutException =>
                (HttpStatusCode.RequestTimeout, "Operation timed out", "OPERATION_TIMEOUT"),

            // HTTP 
            HttpRequestException =>
                (HttpStatusCode.BadGateway, "External service error", "EXTERNAL_SERVICE_ERROR"),

            _ =>
                (HttpStatusCode.InternalServerError,
                    "An unexpected error occurred. Please contact support.",
                    "INTERNAL_SERVER_ERROR")
        };
    }

    /// <summary>
    /// Get detailed exception information for debugging
    /// </summary>
    private static string? GetExceptionDetails(Exception exception, IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return null; // Production: no detailed exception information
        }

        // Development: detailed exception report
        var sb = new StringBuilder();

        sb.AppendLine("EXCEPTION DETAILS");
        sb.AppendLine();

        sb.AppendLine($"Exception Type: {exception.GetType().FullName}");
        sb.AppendLine($"Message: {exception.Message}");
        sb.AppendLine();

        // Stack trace
        sb.AppendLine("Stack Trace:");
        sb.AppendLine(exception.StackTrace);
        sb.AppendLine();

        // Inner exceptions
        if (exception.InnerException != null)
        {
            sb.AppendLine("Inner Exception:");
            sb.AppendLine($"   Type: {exception.InnerException.GetType().FullName}");
            sb.AppendLine($"   Message: {exception.InnerException.Message}");
            sb.AppendLine($"   Stack: {exception.InnerException.StackTrace}");
            sb.AppendLine();
        }

        // Aggregate exceptions (for Task.WhenAll etc.)
        if (exception is AggregateException ae)
        {
            sb.AppendLine($"Aggregate Exception ({ae.InnerExceptions.Count} exceptions):");
            foreach (var innerEx in ae.InnerExceptions)
            {
                sb.AppendLine($"   - {innerEx.GetType().Name}: {innerEx.Message}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}

public class ErrorResponse
{
    public string ErrorCode { get; set; } = "";
    public string Message { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public string? Details { get; set; } = "";
    public DateTime Timestamp { get; set; }

    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}
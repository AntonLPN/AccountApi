using Polly;

namespace Account.Infrastructure.Configuration;

public static class PollyPolicies
{
    public static IAsyncPolicy<bool> GetEmailRetryPolicy()
    {
        return Policy
            .Handle<Exception>()
            .OrResult<bool>(r => !r) // retry если вернул false
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)), // exponential backoff: 2s, 4s, 8s
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s");
                });
    }
    
    public static IAsyncPolicy<bool> GetEmailRetryWithCircuitBreaker()
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .OrResult<bool>(r => !r)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)));

        var circuitBreakerPolicy = Policy
            .Handle<Exception>()
            .OrResult<bool>(r => !r)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));

        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
    }

}
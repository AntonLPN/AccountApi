using Account.Infrastructure.Configuration;
using Account.Infrastructure.HttpClients;
using Account.Infrastructure.Persistence;
using AccountApi.Authorization;
using AccountApi.Extensions;
using AccountApi.Middleware;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();//for debug
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
builder.Host.AddSerilogLogging();

builder.Services.AddMySqlDatabase(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddMassTransitMessaging(builder.Configuration);
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddLifeTimeServices();


builder.Services.AddRateLimiter(limiter =>
{
    limiter.AddFixedWindowLimiter("fixed", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromSeconds(1);
        options.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 10;
    });
});
builder.Services.Configure<KeycloakAdminOptions>(builder.Configuration.GetSection("KeycloakAdminClient"));
builder.Services.Configure<GoogleOptions>(builder.Configuration.GetSection("Google"));
builder.Services.Configure<CryptoOptions>(builder.Configuration.GetSection("Crypto"));
builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection("ApiKey"));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));


builder.Services.AddHttpClient<KeycloakHttpClient>()
    .AddStandardResilienceHandler(options =>
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(130);
    });
//Use in production:
// builder.Services.AddStackExchangeRedisCache(options =>
//     options.Configuration = builder.Configuration.GetConnectionString("Redis"));

// For debug
builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Account API v1");
//for debug, to avoid caching of swagger.json and always get the latest version
    options.ConfigObject.AdditionalItems["version"] = DateTime.UtcNow.Ticks.ToString();
});
app.UseMiddleware<ExceptionHandlingMiddleware>();
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        //For the period of development it's ok because the database is empty, and we can easily reset it,
        //but in production you should use migrations to avoid data loss
        //await dbContext.Database.MigrateAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or initializing the database.");
        throw;
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers().RequireRateLimiting("fixed");
app.Run();
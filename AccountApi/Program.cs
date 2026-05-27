using Account.Infrastructure.Configuration;
using Account.Infrastructure.HttpClients;
using Account.Infrastructure.Persistence;
using AccountApi.Authorization;
using AccountApi.Extensions;
using AccountApi.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers(options => { options.Filters.Add<ApiKeyAuthFilter>(); });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
    options.SwaggerDoc("v1.0.0", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Account API",
        Version = "v1.0.0"
    });
});

builder.Services.AddMemoryCache();
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
builder.Host.AddSerilogLogging();
builder.Services.AddMySqlDatabase(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
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
builder.Services.AddHttpClient<KeycloakHttpClient>()
    .AddStandardResilienceHandler(options =>
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "User", "Admin", "Moderator" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or initializing the database.");
        throw;
    }
}

builder.Services.AddAuthorization();
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers().RequireRateLimiting("fixed");
app.Run();
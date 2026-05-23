using AccountApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();

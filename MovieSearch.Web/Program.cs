using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using MovieSearch.Core.Interfaces;
using MovieSearch.Infrastructure;
using MovieSearch.Infrastructure.Repository;
using MovieSearch.Infrastructure.Providers.Omdb;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var apiRateLimitPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:Api:PermitLimit")
    ?? throw new InvalidOperationException("RateLimiting:Api:PermitLimit is not set in appsettings.");
var apiRateLimitWindowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:Api:WindowSeconds")
    ?? throw new InvalidOperationException("RateLimiting:Api:WindowSeconds is not set in appsettings.");
var apiRateLimitQueueLimit = builder.Configuration.GetValue<int?>("RateLimiting:Api:QueueLimit")
    ?? throw new InvalidOperationException("RateLimiting:Api:QueueLimit is not set in appsettings.");

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = apiRateLimitPermitLimit;
        limiterOptions.Window = TimeSpan.FromSeconds(apiRateLimitWindowSeconds);
        limiterOptions.QueueLimit = apiRateLimitQueueLimit;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

builder.Services.AddSingleton<IMovieSearchCache, InMemoryMovieSearchCache>();
builder.Services.AddScoped<IMovieRepository, MovieRepository>();

#region Omdb configuration

var apiKey = builder.Configuration["Providers:Omdb:ApiKey"]
    ?? throw new InvalidOperationException("Omdb: ApiKey is not set in user secrets.");

var baseUrl = builder.Configuration["Providers:Omdb:BaseUrl"]
    ?? throw new InvalidOperationException("Omdb: baseUrl is not set in appsettings.");

builder.Services.AddHttpClient<OmdbClient>((sp, client) =>
{
    client.BaseAddress = new Uri(baseUrl);
})
    .AddTypedClient(httpClient => new OmdbClient(httpClient, apiKey));

builder.Services.AddScoped<IMovieProvider, OmdbProvider>();

#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers().RequireRateLimiting("api");

app.Run();

using MovieSearch.Core.Interfaces;
using MovieSearch.Infrastracture.Providers.Omdb;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen();

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

app.UseAuthorization();

app.MapControllers();

app.Run();

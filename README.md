# MovieSearch

MovieSearch is a .NET 10 movie search app that currently uses the OMDb API.

It includes:

- ASP.NET Core Web API
- Static HTML/CSS/JavaScript page in `wwwroot`
- Core DTOs and provider interface
- Infrastructure provider for OMDb
- xUnit tests for the OMDb provider

## Project Structure

```text
MovieSearch.Core
  DTOs, pagination models, response wrapper, IMovieProvider

MovieSearch.Infrastructure
  OMDb client/provider and provider discovery helper

MovieSearch.Web
  API controllers, DI setup, static web page

MovieSearch.Tests
  xUnit tests using fake HTTP responses
```

## Configuration

The OMDb base URL is stored in `MovieSearch.Web/appsettings.json`:

```json
{
  "Providers": {
    "Omdb": {
      "BaseUrl": "https://www.Omdbapi.com/"
    }
  }
}
```

Set your OMDb API key with user secrets:

```powershell
dotnet user-secrets set "Providers:Omdb:ApiKey" "<your-api-key>" --project MovieSearch.Web/MovieSearch.Web.csproj
```

## Run

```powershell
dotnet run --project MovieSearch.Web/MovieSearch.Web.csproj --launch-profile http
```

Then open:

```text
http://localhost:5173
```

Swagger is available in development at:

```text
http://localhost:5173/swagger
```

## Test

```powershell
dotnet test
```

The provider tests do not call the real OMDb API. They use a fake `HttpMessageHandler` to test mapping, pagination, and not-found behavior.
They also cover provider error handling for failed HTTP responses and invalid JSON.

## API

All API responses use the shared `Response<T>` wrapper:

```json
{
  "data": {},
  "responseType": 2,
  "message": "Ok"
}
```

### Get Providers

```http
GET /api/movie/providers
```

Returns provider class names discovered from loaded concrete `IMovieProvider` implementations:

```json
{
  "data": ["OmdbProvider"],
  "responseType": 2,
  "message": "Ok"
}
```

### Search Movies

```http
GET /api/movie/search?query=batman&provider=OmdbProvider&pageIndex=1&pageSize=10&type=movie&year=2005
```

Query parameters:

- `query`: required search text
- `provider`: provider class name from `/api/movie/providers`
- `pageIndex`: page number, defaults to `1`
- `pageSize`: page size, defaults to `10`
- `type`: optional OMDb type: `movie`, `series`, or `episode`
- `year`: optional release year from `1888` through the current year

API routes are rate limited using the `RateLimiting:Api` settings in `appsettings.json`. Invalid search types and out-of-range years are rejected before provider calls are made.
If the movie provider fails, times out, returns a non-success HTTP status, or sends invalid data, the API returns a safe error response instead of exposing provider details.

### Get Movie Details

```http
GET /api/movie/tt0372784?provider=OmdbProvider
```

Returns full movie details, including ratings.

## Provider Resolution

`MovieRepository` reads the registered `IMovieProvider` instances, lists their provider class names, and resolves the requested provider by class name. If no provider is supplied, the first registered provider ordered by class name is used.

Current provider:

```text
OmdbProvider
```

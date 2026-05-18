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

MovieSearch.Infrastracture
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
- `type`: optional OMDb type, such as `movie`, `series`, or `episode`
- `year`: optional release year

### Get Movie Details

```http
GET /api/movie/tt0372784?provider=OmdbProvider
```

Returns full movie details, including ratings.

## Provider Discovery

`ProviderHelper` scans loaded assemblies for concrete classes that implement `IMovieProvider`.

Current provider:

```text
OmdbProvider
```

Note: the controller currently injects a single `IMovieProvider`. That works while there is only one provider. When adding more providers, switch the controller to use a provider resolver or inject `IEnumerable<IMovieProvider>` and select the provider by class name.

using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using MovieSearch.Core.Exceptions;
using MovieSearch.Infrastructure.Providers.Omdb;
using MovieSearchCore.DTOs;

namespace MovieSearch.Tests;

public class OmdbProviderTests
{
    [Fact]
    public async Task SearchMoviesAsync_MapsResultsAndPaginationMetadata()
    {
        var provider = CreateProvider(_ => SearchResponse(
            totalResults: 42,
            movies:
            [
                new SearchMovie("tt0372784", "Batman Begins", "2005")
            ]));

        var result = await provider.SearchMoviesAsync(
            "batman",
            new PaginationFilter { PageIndex = 1, PageSize = 10 });

        var movie = Assert.Single(result.Items);

        Assert.Equal("tt0372784", movie.ImdbId);
        Assert.Equal("Batman Begins", movie.Title);
        Assert.Equal("2005", movie.Year);
        Assert.Equal("movie", movie.Type);
        Assert.Equal("tt0372784.jpg", movie.Poster);

        Assert.Equal(1, result.PageIndex);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(42, result.TotalCount);
        Assert.Equal(5, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task SearchMoviesAsync_ReturnsEmptyPage_WhenOmdbReturnsFalse()
    {
        var provider = CreateProvider(_ => JsonSerializer.Serialize(new
        {
            Response = "False",
            Error = "Movie not found!"
        }));

        var result = await provider.SearchMoviesAsync(
            "does-not-exist",
            new PaginationFilter { PageIndex = 1, PageSize = 10 });

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task SearchMoviesAsync_FetchesMultipleOmdbPages_WhenPageSizeIsGreaterThanTen()
    {
        var requestedPages = new List<int>();

        var provider = CreateProvider(request =>
        {
            var page = GetQueryInt(request.RequestUri!, "page");
            requestedPages.Add(page);

            return page switch
            {
                1 => SearchResponse(12, CreateMovies(1, 10)),
                2 => SearchResponse(12, CreateMovies(11, 2)),
                _ => SearchErrorResponse()
            };
        });

        var result = await provider.SearchMoviesAsync(
            "batman",
            new PaginationFilter { PageIndex = 1, PageSize = 12 });

        Assert.Equal([1, 2], requestedPages);
        Assert.Equal(12, result.Items.Count());
        Assert.Equal("Movie 1", result.Items.First().Title);
        Assert.Equal("Movie 12", result.Items.Last().Title);
        Assert.Equal(12, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task SearchMoviesAsync_SkipsItemsFromFirstOmdbPage_ForLaterPaginationPage()
    {
        var requestedPages = new List<int>();

        var provider = CreateProvider(request =>
        {
            var page = GetQueryInt(request.RequestUri!, "page");
            requestedPages.Add(page);

            return page switch
            {
                1 => SearchResponse(25, CreateMovies(1, 10)),
                2 => SearchResponse(25, CreateMovies(11, 10)),
                _ => SearchErrorResponse()
            };
        });

        var result = await provider.SearchMoviesAsync(
            "batman",
            new PaginationFilter { PageIndex = 2, PageSize = 6 });

        Assert.Equal([1, 2], requestedPages);
        Assert.Equal(["Movie 7", "Movie 8", "Movie 9", "Movie 10", "Movie 11", "Movie 12"],
            result.Items.Select(movie => movie.Title));
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(5, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task GetMovieDetailsAsync_MapsDetailsAndRatings()
    {
        var provider = CreateProvider(_ => JsonSerializer.Serialize(new
        {
            Title = "Batman Begins",
            Year = "2005",
            Rated = "PG-13",
            Released = "15 Jun 2005",
            Runtime = "140 min",
            Genre = "Action, Crime, Drama",
            Director = "Christopher Nolan",
            Actors = "Christian Bale, Michael Caine",
            Plot = "After training with his mentor, Batman begins his fight.",
            Language = "English",
            Country = "United States",
            Awards = "Nominated for 1 Oscar.",
            Poster = "poster.jpg",
            Ratings = new[]
            {
                new
                {
                    Source = "Internet Movie Database",
                    Value = "8.2/10"
                }
            },
            imdbRating = "8.2",
            imdbVotes = "1,600,000",
            imdbID = "tt0372784",
            Type = "movie",
            Response = "True"
        }));

        var result = await provider.GetMovieDetailsAsync("tt0372784");

        Assert.NotNull(result);
        Assert.Equal("tt0372784", result.ImdbId);
        Assert.Equal("Batman Begins", result.Title);
        Assert.Equal("PG-13", result.Rated);
        Assert.Equal("Christopher Nolan", result.Director);
        Assert.Equal("8.2", result.ImdbRating);

        var rating = Assert.Single(result.Ratings);
        Assert.Equal("Internet Movie Database", rating.Source);
        Assert.Equal("8.2/10", rating.Value);
    }

    [Fact]
    public async Task GetMovieDetailsAsync_ReturnsNull_WhenOmdbReturnsFalse()
    {
        var provider = CreateProvider(_ => JsonSerializer.Serialize(new
        {
            Response = "False",
            Error = "Incorrect IMDb ID."
        }));

        var result = await provider.GetMovieDetailsAsync("bad-id");

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchMoviesAsync_ThrowsProviderException_WhenResponseIsInvalidJson()
    {
        var provider = CreateProvider(_ => "not-json");

        var exception = await Assert.ThrowsAsync<MovieProviderException>(() =>
            provider.SearchMoviesAsync(
                "batman",
                new PaginationFilter { PageIndex = 1, PageSize = 10 }));

        Assert.Equal("Movie provider returned invalid data.", exception.Message);
    }

    [Fact]
    public async Task SearchMoviesAsync_ThrowsProviderException_WhenHttpRequestFails()
    {
        var provider = CreateProvider(
            _ => "Server error",
            HttpStatusCode.InternalServerError);

        var exception = await Assert.ThrowsAsync<MovieProviderException>(() =>
            provider.SearchMoviesAsync(
                "batman",
                new PaginationFilter { PageIndex = 1, PageSize = 10 }));

        Assert.Equal("Movie provider returned HTTP 500.", exception.Message);
    }

    private static OmdbProvider CreateProvider(Func<HttpRequestMessage, string> responseFactory)
        => CreateProvider(responseFactory, HttpStatusCode.OK);

    private static OmdbProvider CreateProvider(
        Func<HttpRequestMessage, string> responseFactory,
        HttpStatusCode statusCode)
    {
        var handler = new FakeHttpMessageHandler(request =>
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseFactory(request), Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://www.Omdbapi.com/")
        };

        return new OmdbProvider(new OmdbClient(httpClient, "test-key"));
    }

    private static string SearchResponse(int totalResults, IEnumerable<SearchMovie> movies) =>
        JsonSerializer.Serialize(new
        {
            Search = movies.Select(movie => new
            {
                Title = movie.Title,
                Year = movie.Year,
                imdbID = movie.ImdbId,
                Type = "movie",
                Poster = $"{movie.ImdbId}.jpg"
            }),
            totalResults = totalResults.ToString(CultureInfo.InvariantCulture),
            Response = "True"
        });

    private static string SearchErrorResponse() =>
        JsonSerializer.Serialize(new
        {
            Response = "False",
            Error = "Movie not found!"
        });

    private static IEnumerable<SearchMovie> CreateMovies(int start, int count) =>
        Enumerable.Range(start, count)
            .Select(index => new SearchMovie(
                $"tt{index.ToString(CultureInfo.InvariantCulture).PadLeft(7, '0')}",
                $"Movie {index.ToString(CultureInfo.InvariantCulture)}",
                "2005"));

    private static int GetQueryInt(Uri uri, string name)
    {
        var value = uri.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .First(parts => Uri.UnescapeDataString(parts[0]) == name)[1];

        return int.Parse(Uri.UnescapeDataString(value), CultureInfo.InvariantCulture);
    }

    private sealed record SearchMovie(string ImdbId, string Title, string Year);

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }
}

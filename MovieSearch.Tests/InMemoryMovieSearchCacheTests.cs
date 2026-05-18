using MovieSearch.Core.DTOs.Movie;
using MovieSearch.Infrastructure;
using MovieSearchCore.DTOs;

namespace MovieSearch.Tests;

public class InMemoryMovieSearchCacheTests
{
    [Fact]
    public void TryGet_ReturnsFalse_WhenQueryIsNotCached()
    {
        var cache = new InMemoryMovieSearchCache();

        var exists = cache.TryGet("batman", out var movies);

        Assert.False(exists);
        Assert.Null(movies);
    }

    [Fact]
    public void TryGet_ReturnsCachedResult_IgnoringCaseAndWhitespace()
    {
        var cache = new InMemoryMovieSearchCache();
        var expected = new PaginatedList<MovieSearchResult>(
            [new MovieSearchResult { Title = "Batman Begins" }],
            new PaginationFilter(),
            1);

        cache.Set(" batman ", expected);

        var exists = cache.TryGet("BATMAN", out var movies);

        Assert.True(exists);
        Assert.Same(expected, movies);
    }

    [Fact]
    public void Set_ReplacesPreviousResult_ForSameQuery()
    {
        var cache = new InMemoryMovieSearchCache();
        var first = new PaginatedList<MovieSearchResult>(
            [new MovieSearchResult { Title = "Batman Begins" }],
            new PaginationFilter(),
            1);
        var latest = new PaginatedList<MovieSearchResult>(
            [new MovieSearchResult { Title = "The Batman" }],
            new PaginationFilter(),
            1);

        cache.Set("batman", first);
        cache.Set("Batman", latest);

        var exists = cache.TryGet("batman", out var movies);

        Assert.True(exists);
        Assert.Same(latest, movies);
    }
}

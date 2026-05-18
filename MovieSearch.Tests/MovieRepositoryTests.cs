using MovieSearch.Core.DTOs.Movie;
using MovieSearch.Core.Exceptions;
using MovieSearch.Core.Interfaces;
using MovieSearch.Infrastructure;
using MovieSearch.Infrastructure.Repository;
using MovieSearchCore.DTOs;
using MovieSearchCore.Enums;

namespace MovieSearch.Tests;

public class MovieRepositoryTests
{
    [Fact]
    public void GetProviders_ReturnsRegisteredProviderNames()
    {
        var repository = CreateRepository(
            new ZetaProvider(),
            new AlphaProvider(),
            new AlphaProvider());

        var result = repository.GetProviders();

        Assert.Equal(["AlphaProvider", "ZetaProvider"], result.Data);
    }

    [Fact]
    public async Task SearchAsync_UsesRequestedProviderByNameIgnoringCase()
    {
        var alphaProvider = new AlphaProvider();
        var zetaProvider = new ZetaProvider();
        var repository = CreateRepository(alphaProvider, zetaProvider);

        var result = await repository.SearchAsync(
            "batman",
            new PaginationFilter(),
            "zetaprovider");

        Assert.Equal(ResponseType.Success, result.ResponseType);
        Assert.Equal(0, alphaProvider.SearchCallCount);
        Assert.Equal(1, zetaProvider.SearchCallCount);
    }

    [Fact]
    public async Task SearchAsync_UsesFirstProviderByName_WhenProviderNameIsMissing()
    {
        var alphaProvider = new AlphaProvider();
        var zetaProvider = new ZetaProvider();
        var repository = CreateRepository(zetaProvider, alphaProvider);

        var result = await repository.SearchAsync("batman", new PaginationFilter());

        Assert.Equal(ResponseType.Success, result.ResponseType);
        Assert.Equal(1, alphaProvider.SearchCallCount);
        Assert.Equal(0, zetaProvider.SearchCallCount);
    }

    [Fact]
    public async Task SearchAsync_ReturnsError_WhenProviderIsNotRegistered()
    {
        var repository = CreateRepository(new AlphaProvider());

        var result = await repository.SearchAsync(
            "batman",
            new PaginationFilter(),
            "MissingProvider");

        Assert.Equal(ResponseType.Error, result.ResponseType);
        Assert.Equal("Provider is not available.", result.Message);
    }

    [Fact]
    public async Task SearchAsync_ReturnsError_WhenTypeIsNotAllowed()
    {
        var provider = new AlphaProvider();
        var repository = CreateRepository(provider);

        var result = await repository.SearchAsync(
            "batman",
            new PaginationFilter(),
            type: "documentary");

        Assert.Equal(ResponseType.Error, result.ResponseType);
        Assert.Equal("Type must be movie, series, or episode.", result.Message);
        Assert.Equal(0, provider.SearchCallCount);
    }

    [Fact]
    public async Task SearchAsync_ReturnsError_WhenYearIsOutOfRange()
    {
        var provider = new AlphaProvider();
        var repository = CreateRepository(provider);

        var result = await repository.SearchAsync(
            "batman",
            new PaginationFilter(),
            year: DateTime.UtcNow.Year + 1);

        Assert.Equal(ResponseType.Error, result.ResponseType);
        Assert.Equal($"Year must be between 1888 and {DateTime.UtcNow.Year}.", result.Message);
        Assert.Equal(0, provider.SearchCallCount);
    }

    [Fact]
    public async Task SearchAsync_TrimsAndNormalizesAllowedType()
    {
        var provider = new AlphaProvider();
        var repository = CreateRepository(provider);

        var result = await repository.SearchAsync(
            "batman",
            new PaginationFilter(),
            type: " Movie ");

        Assert.Equal(ResponseType.Success, result.ResponseType);
        Assert.Equal("movie", provider.LastType);
    }

    [Fact]
    public async Task SearchAsync_ReturnsError_WhenProviderCallFails()
    {
        var provider = new AlphaProvider
        {
            SearchException = new MovieProviderException("Provider failed.")
        };
        var repository = CreateRepository(provider);

        var result = await repository.SearchAsync("batman", new PaginationFilter());

        Assert.Equal(ResponseType.Error, result.ResponseType);
        Assert.Equal("Movie provider is temporarily unavailable.", result.Message);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsError_WhenProviderCallFails()
    {
        var provider = new AlphaProvider
        {
            DetailsException = new MovieProviderException("Provider failed.")
        };
        var repository = CreateRepository(provider);

        var result = await repository.GetByIdAsync("tt0372784");

        Assert.Equal(ResponseType.Error, result.ResponseType);
        Assert.Equal("Movie provider is temporarily unavailable.", result.Message);
    }

    private static MovieRepository CreateRepository(params IMovieProvider[] providers) =>
        new(providers, new InMemoryMovieSearchCache());

    private sealed class AlphaProvider : TestMovieProvider;

    private sealed class ZetaProvider : TestMovieProvider;

    private abstract class TestMovieProvider : IMovieProvider
    {
        public int SearchCallCount { get; private set; }
        public string? LastType { get; private set; }
        public Exception? SearchException { get; init; }
        public Exception? DetailsException { get; init; }

        public Task<PaginatedList<MovieSearchResult>> SearchMoviesAsync(
            string query,
            PaginationFilter filter,
            string? type = null,
            int? year = null,
            CancellationToken cancellationToken = default)
        {
            SearchCallCount++;
            LastType = type;

            if (SearchException is not null)
            {
                throw SearchException;
            }

            return Task.FromResult(new PaginatedList<MovieSearchResult>(
                [new MovieSearchResult { Title = GetType().Name }],
                filter,
                1));
        }

        public Task<MovieDetails?> GetMovieDetailsAsync(string id, CancellationToken cancellationToken = default)
        {
            if (DetailsException is not null)
            {
                throw DetailsException;
            }

            return Task.FromResult<MovieDetails?>(null);
        }
    }
}

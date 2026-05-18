using MovieSearch.Core.DTOs.Movie;
using MovieSearch.Core.Interfaces;
using MovieSearch.Infrastructure;
using MovieSearchCore.DTOs;

namespace MovieSearch.Tests;

public class ProviderHelperTests
{
    [Fact]
    public void GetAvailableProviders_ReturnsRegisteredProviderNames()
    {
        IMovieProvider[] providers =
        [
            new ZetaProvider(),
            new AlphaProvider(),
            new AlphaProvider()
        ];

        var result = ProviderHelper.GetAvailableProviders(providers);

        Assert.Equal(["AlphaProvider", "ZetaProvider"], result);
    }

    [Fact]
    public void ResolveProvider_ReturnsRequestedProviderByNameIgnoringCase()
    {
        IMovieProvider[] providers =
        [
            new AlphaProvider(),
            new ZetaProvider()
        ];

        var result = ProviderHelper.ResolveProvider(providers, "zetaprovider");

        Assert.IsType<ZetaProvider>(result);
    }

    [Fact]
    public void ResolveProvider_ReturnsFirstProviderByName_WhenProviderNameIsMissing()
    {
        IMovieProvider[] providers =
        [
            new ZetaProvider(),
            new AlphaProvider()
        ];

        var result = ProviderHelper.ResolveProvider(providers, null);

        Assert.IsType<AlphaProvider>(result);
    }

    [Fact]
    public void ResolveProvider_ReturnsNull_WhenProviderIsNotRegistered()
    {
        IMovieProvider[] providers =
        [
            new AlphaProvider()
        ];

        var result = ProviderHelper.ResolveProvider(providers, "MissingProvider");

        Assert.Null(result);
    }

    private sealed class AlphaProvider : TestMovieProvider;

    private sealed class ZetaProvider : TestMovieProvider;

    private abstract class TestMovieProvider : IMovieProvider
    {
        public Task<PaginatedList<MovieSearchResult>> SearchMoviesAsync(
            string query,
            PaginationFilter filter,
            string? type = null,
            int? year = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PaginatedList<MovieSearchResult>([], filter, 0));

        public Task<MovieDetails?> GetMovieDetailsAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult<MovieDetails?>(null);
    }
}

using MovieSearch.Core.DTOs.Movie;
using MovieSearch.Core.Interfaces;
using MovieSearchCore.DTOs;
using MovieSearchCore.DTOs.Responses;
using MovieSearchCore.Enums;

namespace MovieSearch.Infrastructure.Repository
{
    public class MovieRepository(
        IEnumerable<IMovieProvider> movieProviders,
        IMovieSearchCache movieSearchCache) : IMovieRepository
    {
        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "movie",
            "series",
            "episode"
        };

        private const int FirstMovieYear = 1888;

        private readonly IEnumerable<IMovieProvider> _movieProviders = movieProviders;
        private readonly IMovieSearchCache _movieSearchCache = movieSearchCache;

        public Response<IReadOnlyList<string>> GetProviders() =>
            new(GetAvailableProviders());

        public async Task<Response<PaginatedList<MovieSearchResult>>> SearchAsync(
            string? query,
            PaginationFilter? filter,
            string? provider = null,
            string? type = null,
            int? year = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new Response<PaginatedList<MovieSearchResult>>(
                    messageType: ResponseType.Error,
                    message: "Query is required.");
            }

            var trimmedQuery = query.Trim();
            var normalizedType = NormalizeType(type);

            if (!IsValidType(normalizedType))
            {
                return new Response<PaginatedList<MovieSearchResult>>(
                    messageType: ResponseType.Error,
                    message: "Type must be movie, series, or episode.");
            }

            if (!IsValidYear(year))
            {
                return new Response<PaginatedList<MovieSearchResult>>(
                    messageType: ResponseType.Error,
                    message: $"Year must be between {FirstMovieYear} and {DateTime.UtcNow.Year}.");
            }

            if (_movieSearchCache.TryGet(trimmedQuery, out var cachedMovies))
            {
                return new Response<PaginatedList<MovieSearchResult>>(cachedMovies);
            }

            var movieProvider = ResolveProvider(provider);

            if (movieProvider is null)
            {
                return new Response<PaginatedList<MovieSearchResult>>(
                    messageType: ResponseType.Error,
                    message: "Provider is not available.");
            }

            filter ??= new PaginationFilter();

            var movies = await movieProvider.SearchMoviesAsync(
                trimmedQuery,
                filter,
                normalizedType,
                year,
                cancellationToken);

            _movieSearchCache.Set(trimmedQuery, movies);

            return new Response<PaginatedList<MovieSearchResult>>(movies);
        }

        public async Task<Response<MovieDetails>> GetByIdAsync(
            string id,
            string? provider = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new Response<MovieDetails>(
                    messageType: ResponseType.Error,
                    message: "IMDb id is required.");
            }

            var movieProvider = ResolveProvider(provider);

            if (movieProvider is null)
            {
                return new Response<MovieDetails>(
                    messageType: ResponseType.Error,
                    message: "Provider is not available.");
            }

            var movie = await movieProvider.GetMovieDetailsAsync(id.Trim(), cancellationToken);

            if (movie is null)
            {
                return new Response<MovieDetails>(
                    messageType: ResponseType.NotFound,
                    message: "Movie not found.");
            }

            return new Response<MovieDetails>(movie);
        }

        private IReadOnlyList<string> GetAvailableProviders()
        {
            return _movieProviders
                .Select(provider => provider.GetType().Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(providerName => providerName)
                .ToList();
        }

        private IMovieProvider? ResolveProvider(string? provider)
        {
            var availableProviders = _movieProviders
                .OrderBy(provider => provider.GetType().Name)
                .ToList();

            if (string.IsNullOrWhiteSpace(provider))
            {
                return availableProviders.FirstOrDefault();
            }

            return availableProviders.FirstOrDefault(availableProvider =>
                availableProvider.GetType().Name.Equals(provider.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static string? NormalizeType(string? type) =>
            string.IsNullOrWhiteSpace(type) ? null : type.Trim().ToLowerInvariant();

        private static bool IsValidType(string? type) =>
            type is null || AllowedTypes.Contains(type);

        private static bool IsValidYear(int? year) =>
            year is null || year is >= FirstMovieYear && year <= DateTime.UtcNow.Year;
    }
}

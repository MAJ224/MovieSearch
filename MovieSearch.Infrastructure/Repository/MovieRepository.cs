using MovieSearch.Core.Exceptions;
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
            var movieProvider = ResolveProvider(provider);

            if (movieProvider is null)
            {
                return new Response<PaginatedList<MovieSearchResult>>(
                    messageType: ResponseType.Error,
                    message: "Provider is not available.");
            }

            if (_movieSearchCache.TryGet(trimmedQuery, out var cachedMovies))
            {
                return new Response<PaginatedList<MovieSearchResult>>(cachedMovies);
            }

            filter ??= new PaginationFilter();

            PaginatedList<MovieSearchResult> movies;

            try
            {
                movies = await movieProvider.SearchMoviesAsync(
                    trimmedQuery,
                    filter,
                    type,
                    year,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (MovieProviderValidationException exception)
            {
                return new Response<PaginatedList<MovieSearchResult>>(
                    messageType: ResponseType.Error,
                    message: exception.Message);
            }
            catch (MovieProviderException)
            {
                return new Response<PaginatedList<MovieSearchResult>>(
                    messageType: ResponseType.Error,
                    message: "Movie provider is temporarily unavailable.");
            }
            catch (Exception)
            {
                return new Response<PaginatedList<MovieSearchResult>>(
                    messageType: ResponseType.Error,
                    message: "Something went wrong while searching movies.");
            }

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

            MovieDetails? movie;

            try
            {
                movie = await movieProvider.GetMovieDetailsAsync(id.Trim(), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (MovieProviderException)
            {
                return new Response<MovieDetails>(
                    messageType: ResponseType.Error,
                    message: "Movie provider is temporarily unavailable.");
            }
            catch (Exception)
            {
                return new Response<MovieDetails>(
                    messageType: ResponseType.Error,
                    message: "Something went wrong while loading movie details.");
            }

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
    }
}

using MovieSearch.Core.DTOs.Movie;
using MovieSearch.Core.Interfaces;
using MovieSearch.Infrastructure.Providers.Omdb;
using MovieSearch.Infrastructure.Providers.Omdb.Requests;
using MovieSearch.Infrastructure.Providers.Omdb.Responses;
using MovieSearchCore.DTOs;
using System.Globalization;

namespace MovieSearch.Infrastructure.Providers.Omdb
{
    public class OmdbProvider(OmdbClient client) : IMovieProvider
    {
        private const int OmdbPageSize = 10;
        private readonly OmdbClient _client = client;

        public async Task<PaginatedList<MovieSearchResult>> SearchMoviesAsync(
            string query,
            PaginationFilter filter,
            string? type = null,
            int? year = null,
            CancellationToken cancellationToken = default)
        {
            var items = new List<MovieSearchResult>();
            var totalCount = 0;

            var startIndex = (filter.PageIndex - 1) * filter.PageSize;
            var OmdbPage = (startIndex / OmdbPageSize) + 1;
            var skipOnFirstPage = startIndex % OmdbPageSize;

            while (items.Count < filter.PageSize)
            {
                var response = await _client.SearchAsync(
                    new OmdbSearchRequest
                    {
                        Query = query,
                        Type = type,
                        Year = year,
                        Page = OmdbPage
                    },
                    cancellationToken);

                if (!IsSuccessful(response.Response))
                {
                    return new PaginatedList<MovieSearchResult>([], filter, totalCount);
                }

                if (totalCount == 0)
                {
                    totalCount = ParseTotalResults(response.totalResults);
                }

                var pageItems = response.Search
                    .Skip(skipOnFirstPage)
                    .Select(MapSearchItem)
                    .Take(filter.PageSize - items.Count);

                items.AddRange(pageItems);

                if (response.Search.Count < OmdbPageSize || items.Count >= filter.PageSize)
                {
                    break;
                }

                skipOnFirstPage = 0;
                OmdbPage++;
            }

            return new PaginatedList<MovieSearchResult>(items, filter, totalCount);
        }

        public async Task<MovieDetails?> GetMovieDetailsAsync(string id, CancellationToken cancellationToken = default)
        {
            var response = await _client.GetByIdAsync(
                new OmdbGetByIdRequest
                {
                    ImdbId = id
                },
                cancellationToken);

            return IsSuccessful(response.Response) ? MapMovieDetails(response) : null;
        }

        private static MovieSearchResult MapSearchItem(OmdbSearchItem item) => new()
        {
            ImdbId = item.imdbID,
            Title = item.Title,
            Year = item.Year,
            Type = item.Type,
            Poster = item.Poster
        };

        private static MovieDetails MapMovieDetails(OmdbMovieResponse movie) => new()
        {
            ImdbId = movie.imdbID,
            Title = movie.Title,
            Year = movie.Year,
            Rated = movie.Rated,
            Released = movie.Released,
            Runtime = movie.Runtime,
            Genre = movie.Genre,
            Director = movie.Director,
            Actors = movie.Actors,
            Plot = movie.Plot,
            Language = movie.Language,
            Country = movie.Country,
            Awards = movie.Awards,
            Poster = movie.Poster,
            ImdbRating = movie.imdbRating,
            ImdbVotes = movie.imdbVotes,
            Type = movie.Type,
            Ratings = [.. movie.Ratings
                .Select(rating => new MovieRating
                {
                    Source = rating.Source,
                    Value = rating.Value
                })]
        };

        private static int ParseTotalResults(string totalResults) =>
            int.TryParse(totalResults, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)
                ? count : 0;

        private static bool IsSuccessful(string response) =>
            response.Equals("True", StringComparison.OrdinalIgnoreCase);
    }
}

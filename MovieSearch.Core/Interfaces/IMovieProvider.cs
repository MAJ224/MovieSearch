using MovieSearch.Core.DTOs.Movie;
using MovieSearchCore.DTOs;

namespace MovieSearch.Core.Interfaces
{
    public interface IMovieProvider
    {
        public Task<PaginatedList<MovieSearchResult>> SearchMoviesAsync(
            string query,
            PaginationFilter filter,
            string? type = null,
            int? year = null,
            CancellationToken cancellationToken = default);

        public Task<MovieDetails?> GetMovieDetailsAsync(string id, CancellationToken cancellationToken = default);
    }
}

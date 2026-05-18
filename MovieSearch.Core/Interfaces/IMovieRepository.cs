using MovieSearch.Core.DTOs.Movie;
using MovieSearchCore.DTOs;
using MovieSearchCore.DTOs.Responses;

namespace MovieSearch.Core.Interfaces
{
    public interface IMovieRepository
    {
        Response<IReadOnlyList<string>> GetProviders();

        Task<Response<PaginatedList<MovieSearchResult>>> SearchAsync(
            string? query,
            PaginationFilter? filter,
            string? provider = null,
            string? type = null,
            int? year = null,
            CancellationToken cancellationToken = default);

        Task<Response<MovieDetails>> GetByIdAsync(
            string id,
            string? provider = null,
            CancellationToken cancellationToken = default);
    }
}

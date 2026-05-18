using MovieSearch.Core.DTOs.Movie;
using MovieSearchCore.DTOs;

namespace MovieSearch.Core.Interfaces
{
    public interface IMovieSearchCache
    {
        bool TryGet(string query, out PaginatedList<MovieSearchResult>? movies);

        void Set(string query, PaginatedList<MovieSearchResult> movies);
    }
}

using System.Collections.Concurrent;
using MovieSearch.Core.DTOs.Movie;
using MovieSearch.Core.Interfaces;
using MovieSearchCore.DTOs;

namespace MovieSearch.Infrastructure
{
    public class InMemoryMovieSearchCache : IMovieSearchCache
    {
        private readonly ConcurrentDictionary<string, PaginatedList<MovieSearchResult>> _cache = new();

        public bool TryGet(string query, out PaginatedList<MovieSearchResult>? movies) =>
            _cache.TryGetValue(GetCacheKey(query), out movies);

        public void Set(string query, PaginatedList<MovieSearchResult> movies)
        {
            _cache[GetCacheKey(query)] = movies;
        }

        private static string GetCacheKey(string query) =>
            query.Trim().ToUpperInvariant();
    }
}

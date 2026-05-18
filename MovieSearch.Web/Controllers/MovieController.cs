using Microsoft.AspNetCore.Mvc;
using MovieSearch.Core.DTOs.Movie;
using MovieSearch.Core.Interfaces;
using MovieSearch.Infrastructure;
using MovieSearchCore.DTOs;
using MovieSearchCore.DTOs.Responses;
using MovieSearchCore.Enums;
using MovieSearchService.Controllers;

namespace MovieSearch.Web.Controllers
{
    [Route("api/[controller]")]
    public class MovieController(IEnumerable<IMovieProvider> movieProviders) : ApiControllerBase
    {
        private readonly IEnumerable<IMovieProvider> _movieProviders = movieProviders;

        [HttpGet("providers")]
        public IActionResult GetProviders()
            => SendResponse(new Response<IReadOnlyList<string>>(ProviderHelper.GetAvailableProviders(_movieProviders)));

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? query,
            [FromQuery] PaginationFilter? filter,
            [FromQuery] string? provider = null,
            [FromQuery] string? type = null,
            [FromQuery] int? year = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return SendResponse(new Response<object>(
                    messageType: ResponseType.Error,
                    message: "Query is required."));
            }

            var movieProvider = ProviderHelper.ResolveProvider(_movieProviders, provider);

            if (movieProvider is null)
            {
                return SendResponse(new Response<object>(
                    messageType: ResponseType.Error,
                    message: "Provider is not available."));
            }

            filter ??= new PaginationFilter();

            var movies = await movieProvider.SearchMoviesAsync(
                query.Trim(),
                filter,
                type,
                year,
                cancellationToken);

            return SendResponse(new Response<PaginatedList<MovieSearchResult>>(movies));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            string id,
            [FromQuery] string? provider = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return SendResponse(new Response<object>(
                    messageType: ResponseType.Error,
                    message: "IMDb id is required."));
            }

            var movieProvider = ProviderHelper.ResolveProvider(_movieProviders, provider);

            if (movieProvider is null)
            {
                return SendResponse(new Response<object>(
                    messageType: ResponseType.Error,
                    message: "Provider is not available."));
            }

            var movie = await movieProvider.GetMovieDetailsAsync(id.Trim(), cancellationToken);

            if (movie is null)
            {
                return SendResponse(new Response<MovieDetails>(
                    messageType: ResponseType.NotFound,
                    message: "Movie not found."));
            }

            return SendResponse(new Response<MovieDetails>(movie));
        }
    }
}

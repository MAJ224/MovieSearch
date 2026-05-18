using Microsoft.AspNetCore.Mvc;
using MovieSearch.Core.DTOs.Movie;
using MovieSearch.Core.Interfaces;
using MovieSearch.Infrastracture.Providers;
using MovieSearchCore.DTOs;
using MovieSearchCore.DTOs.Responses;
using MovieSearchCore.Enums;
using MovieSearchService.Controllers;

namespace MovieSearch.web.Controllers
{
    [Route("api/[controller]")]
    public class MovieController(IMovieProvider movieProvider) : ApiControllerBase
    {
        private readonly IMovieProvider _movieProvider = movieProvider;

        [HttpGet("providers")]
        public IActionResult GetProviders()
            => SendResponse(new Response<IReadOnlyList<string>>(ProviderHelper.GetAvailableProviders()));

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

            if (!ProviderHelper.IsAvailableProvider(provider))
            {
                return SendResponse(new Response<object>(
                    messageType: ResponseType.Error,
                    message: "Provider is not available."));
            }

            filter ??= new PaginationFilter();

            var movies = await _movieProvider.SearchMoviesAsync(
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

            if (!ProviderHelper.IsAvailableProvider(provider))
            {
                return SendResponse(new Response<object>(
                    messageType: ResponseType.Error,
                    message: "Provider is not available."));
            }

            var movie = await _movieProvider.GetMovieDetailsAsync(id.Trim(), cancellationToken);

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

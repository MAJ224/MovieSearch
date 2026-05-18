using Microsoft.AspNetCore.Mvc;
using MovieSearch.Core.Interfaces;
using MovieSearchCore.DTOs;
using MovieSearchService.Controllers;

namespace MovieSearch.Web.Controllers
{
    [Route("api/[controller]")]
    public class MovieController(IMovieRepository movieRepository) : ApiControllerBase
    {
        [HttpGet("providers")]
        public IActionResult GetProviders()
            => SendResponse(movieRepository.GetProviders());

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] PaginationFilter? filter,
            string? query,
            string? provider = null,
            string? type = null,
            int? year = null,
            CancellationToken cancellationToken = default)
            => SendResponse(await movieRepository.SearchAsync(
                query,
                filter,
                provider,
                type,
                year,
                cancellationToken));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            string id,
            string? provider = null,
            CancellationToken cancellationToken = default)
            => SendResponse(await movieRepository.GetByIdAsync(
                id,
                provider,
                cancellationToken));
    }
}

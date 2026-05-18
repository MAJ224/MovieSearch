using Microsoft.AspNetCore.Mvc;
using MovieSearchCore.DTOs.Responses;
using MovieSearchService.Controllers;

namespace MovieSearch.web.Controllers
{
    [Route("api/[controller]")]
    public class HealthController : ApiControllerBase
    {
        [HttpGet("[action]")]
        public async Task<IActionResult> GetHealth()
            => SendResponse(new Response<object>());
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieSearchCore.DTOs.Responses;
using MovieSearchCore.Enums;

namespace MovieSearchService.Controllers
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected IActionResult SendResponse<T>(Response<T> response) => response.ResponseType switch
        {
            ResponseType.NoData => NoContent(),
            ResponseType.Confirm => Ok(response),
            ResponseType.Success => Ok(response),
            ResponseType.Warning => Ok(response),
            ResponseType.NotFound => NotFound(response),
            ResponseType.Error => BadRequest(response),
            _ => BadRequest(response),
        };

    }
}

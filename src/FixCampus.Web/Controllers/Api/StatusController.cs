using FixCampus.Web.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FixCampus.Web.Controllers.Api;

[ApiController]
[Route("api/durum")]
public sealed class StatusController(TimeProvider timeProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiStatusResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiStatusResponse> Get() => Ok(new ApiStatusResponse(
        Application: "fixCampus",
        Status: "ok",
        Stage: "api-skeleton",
        CheckedAtUtc: timeProvider.GetUtcNow()));
}

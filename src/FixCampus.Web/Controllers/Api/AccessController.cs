using System.Security.Claims;
using FixCampus.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixCampus.Web.Controllers.Api;

[ApiController, Route("api/yetki")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccessController : ControllerBase
{
    [HttpGet("ogrenci"), Authorize(Policy = AppPolicies.StudentAccess)]
    [EndpointSummary("Öğrenci veya yönetici hesabının uygulama erişimini doğrular.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult Student() => VerifiedAccess();

    [HttpGet("yonetici"), Authorize(Policy = AppPolicies.AdministratorAccess)]
    [EndpointSummary("Yalnızca yönetici hesabının erişimini doğrular.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult Administrator() => VerifiedAccess();

    private IActionResult VerifiedAccess() => Ok(new
    {
        userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        role = (string)HttpContext.Items[AppRoleHandler.VerifiedRoleKey]!,
        allowed = true
    });
}

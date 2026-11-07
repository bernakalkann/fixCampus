using FixCampus.Web.Authorization;
using FixCampus.Web.Contracts;
using FixCampus.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Controllers.Api;

[ApiController, Route("api/kategoriler"), Authorize(Policy = AppPolicies.StudentAccess)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CategoriesController(FixCampusDbContext db) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("İlan formunda kullanılacak kategori kimliklerini ve türlerini listeler.")]
    [ProducesResponseType<List<CategoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> List([FromQuery] string? tur = null, CancellationToken ct = default)
    {
        var query = db.Kategoriler.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tur))
        {
            var normalizedTur = tur.Trim();
            query = query.Where(x => x.Tur == normalizedTur);
        }

        return Ok(await query
            .OrderBy(x => x.Tur).ThenBy(x => x.Ad)
            .Select(x => new CategoryResponse(x.Id, x.Ad, x.Tur)).ToListAsync(ct));
    }
}

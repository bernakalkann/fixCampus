using System.Security.Claims;
using FixCampus.Web.Contracts;
using FixCampus.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Controllers.Api;

[ApiController, Authorize(AuthenticationSchemes = "Supabase"), Route("api/profil")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProfileController(FixCampusDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await db.Profiller.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (profile is null) return NotFound(new { message = "Profil bulunamadı. PUT /api/profil ile adınızı kaydedin." });
        return Ok(new { profile.Id, profile.AdSoyad, profile.Bolum, profile.Sinif, profile.Rol, profile.OlusturulmaTarihi,
            Email = User.FindFirstValue(ClaimTypes.Email) });
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateProfileRequest request, CancellationToken ct)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var name = request.AdSoyad.Trim();
        if (name.Length < 2) return BadRequest(new { message = "Ad soyad en az 2 karakter olmalıdır." });
        var bolum = request.Bolum?.Trim();
        var sinif = request.Sinif?.Trim();
        // Atomic upsert also handles simultaneous first requests. Existing roles are never overwritten.
        // Missing or null optional fields preserve existing values; an empty string clears the text.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Profiller" ("Id", "AdSoyad", "Bolum", "Sinif", "Rol", "OlusturulmaTarihi")
            VALUES ({id}, {name}, {bolum}, {sinif}, 'Ogrenci', {DateTimeOffset.UtcNow})
            ON CONFLICT ("Id") DO UPDATE SET
                "AdSoyad" = EXCLUDED."AdSoyad",
                "Bolum" = COALESCE(EXCLUDED."Bolum", "Profiller"."Bolum"),
                "Sinif" = COALESCE(EXCLUDED."Sinif", "Profiller"."Sinif")
            """, ct);
        return await Get(ct);
    }
}

using System.Security.Claims;
using FixCampus.Web.Contracts;
using FixCampus.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Controllers.Api;

[ApiController, Authorize, Route("api/profil")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProfileController(FixCampusDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await db.Profiller.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (profile is null) return NotFound(new { message = "Profil bulunamadı. PUT /api/profil ile adınızı kaydedin." });
        return Ok(new { profile.Id, profile.AdSoyad, profile.Rol, profile.OlusturulmaTarihi,
            Email = User.FindFirstValue(ClaimTypes.Email) });
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateProfileRequest request, CancellationToken ct)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var name = request.AdSoyad.Trim();
        if (name.Length < 2) return BadRequest(new { message = "Ad soyad en az 2 karakter olmalıdır." });
        // Atomic upsert also handles simultaneous first requests. Existing roles are never overwritten.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Profiller" ("Id", "AdSoyad", "Rol", "OlusturulmaTarihi")
            VALUES ({id}, {name}, 'Ogrenci', {DateTimeOffset.UtcNow})
            ON CONFLICT ("Id") DO UPDATE SET "AdSoyad" = EXCLUDED."AdSoyad"
            """, ct);
        return await Get(ct);
    }
}

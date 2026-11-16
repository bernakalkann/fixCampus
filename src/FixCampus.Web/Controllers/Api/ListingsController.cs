using System.Security.Claims;
using FixCampus.Web.Authorization;
using FixCampus.Web.Contracts;
using FixCampus.Web.Data;
using FixCampus.Web.Models.Entities;
using FixCampus.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Controllers.Api;

[ApiController, Route("api/ilanlar"), Authorize(Policy = AppPolicies.StudentAccess)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
public sealed class ListingsController(FixCampusDbContext db, TimeProvider? clock = null) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Gizli olmayan ilanları filtreli ve sayfalı olarak listeler.")]
    [ProducesResponseType<List<ListingResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? tur = null,
        [FromQuery] Guid? kategoriId = null,
        [FromQuery] string? q = null,
        [FromQuery] int? sayfa = null,
        [FromQuery] int? sayfaBoyutu = null,
        CancellationToken ct = default)
    {
        var query = ListingQueries.Filter(db.Ilanlar.AsNoTracking(), db.Database.IsNpgsql(), tur, kategoriId, q, visibleAtUtc: (clock ?? TimeProvider.System).GetUtcNow());
        if (sayfa.HasValue || sayfaBoyutu.HasValue)
        {
            var (page, pageSize) = ListingQueries.Normalize(sayfa, sayfaBoyutu);
            return Ok(await Project(ListingQueries.Page(query, page, pageSize)).ToListAsync(ct));
        }
        return Ok(await Project(ListingQueries.Order(query)).ToListAsync(ct));
    }

    [HttpGet("sayfali")]
    [EndpointSummary("Filtrelenmiş ilanları toplam kayıt ve sayfa bilgisiyle getirir.")]
    [ProducesResponseType<ListingPageResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> PagedList(
        [FromQuery] string? tur = null,
        [FromQuery] Guid? kategoriId = null,
        [FromQuery] string? q = null,
        [FromQuery] int? sayfa = null,
        [FromQuery] int? sayfaBoyutu = null,
        [FromQuery] bool kayipBuluntu = false,
        [FromQuery] bool aktifSosyal = false,
        CancellationToken ct = default)
    {
        var query = ListingQueries.Filter(db.Ilanlar.AsNoTracking(), db.Database.IsNpgsql(), tur, kategoriId, q, kayipBuluntu, aktifSosyal, (clock ?? TimeProvider.System).GetUtcNow());
        var (page, pageSize) = ListingQueries.Normalize(sayfa, sayfaBoyutu);
        var total = await query.CountAsync(ct);
        var items = await Project(ListingQueries.Page(query, page, pageSize)).ToListAsync(ct);
        return Ok(new ListingPageResponse(items, total, page, pageSize,
            Math.Max(1, (int)Math.Ceiling(total / (double)pageSize))));
    }

    [HttpGet("benim")]
    [EndpointSummary("Giriş yapan kullanıcının kendi aktif ilanlarını listeler.")]
    [ProducesResponseType<List<ListingResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> MyListings([FromQuery] string? tur = null, CancellationToken ct = default)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var query = db.Ilanlar.AsNoTracking()
            .Where(x => x.SahipId == userId && x.Durum != "Gizli");

        if (!string.IsNullOrWhiteSpace(tur))
        {
            var normalizedTur = tur.Trim();
            query = query.Where(x => x.Tur == normalizedTur);
        }

        return Ok(await Project(query.OrderByDescending(x => x.OlusturulmaTarihi).ThenBy(x => x.Id)).ToListAsync(ct));
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Bir ilanın detayını getirir; gizli ilanlar gösterilmez.")]
    [ProducesResponseType<ListingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var listing = await Project(db.Ilanlar.AsNoTracking()
            .Where(x => x.Id == id && x.Durum != "Gizli")).SingleOrDefaultAsync(ct);
        return listing is null ? NotFound(new { message = "İlan bulunamadı." }) : Ok(listing);
    }

    [HttpPost]
    [EndpointSummary("Giriş yapan kullanıcı adına aktif bir ilan oluşturur.")]
    [ProducesResponseType<ListingResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(SaveListingRequest request, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        if (!ValidateRequest(request)) return ValidationProblem(ModelState);
        var category = await ValidateCategory(request, ct);
        if (category is null) return ValidationProblem(ModelState);

        var listing = new Ilan { SahipId = userId, KategoriId = category.Id, Kategori = category };
        Apply(listing, request);
        db.Ilanlar.Add(listing);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = listing.Id }, ToResponse(listing));
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("İlanı yalnızca sahibi günceller; sahiplik ve durum değiştirilmez.")]
    [ProducesResponseType<ListingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, SaveListingRequest request, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var listing = await db.Ilanlar.SingleOrDefaultAsync(x => x.Id == id && x.Durum != "Gizli", ct);
        if (listing is null) return NotFound(new { message = "İlan bulunamadı." });
        if (listing.SahipId != userId) return Forbid("Supabase");
        if (!ValidateRequest(request)) return ValidationProblem(ModelState);
        var category = await ValidateCategory(request, ct);
        if (category is null) return ValidationProblem(ModelState);

        Apply(listing, request);
        listing.Kategori = category;
        await db.SaveChangesAsync(ct);
        return await Get(id, ct);
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("İlanı ve bağlı kayıtlarını yalnızca sahibi siler.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var listing = await db.Ilanlar.SingleOrDefaultAsync(x => x.Id == id && x.Durum != "Gizli", ct);
        if (listing is null) return NotFound(new { message = "İlan bulunamadı." });
        if (listing.SahipId != userId) return Forbid("Supabase");
        db.Ilanlar.Remove(listing);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/katilimcilar")]
    [EndpointSummary("Sosyal buluşmanın katılımcılarını yalnızca ilan sahibine gösterir.")]
    [ProducesResponseType<List<ParticipantResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetParticipants(Guid id, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();

        var listing = db.Ilanlar.AsNoTracking()
            .Where(x => x.Id == id && x.Tur == "Sosyal" && x.Durum != "Gizli");
        var ownerId = await listing.Select(x => (Guid?)x.SahipId).SingleOrDefaultAsync(ct);
        if (ownerId is null) return NotFound(new { message = "Sosyal ilan bulunamadı." });
        if (ownerId != userId) return Forbid("Supabase");

        var participants = await listing.Where(x => x.SahipId == userId)
            .SelectMany(x => x.Katilimlar)
            .OrderBy(x => x.KatilimTarihi).ThenBy(x => x.Id)
            .Select(x => new ParticipantResponse(x.Profil.AdSoyad, x.Profil.Bolum, x.Profil.Sinif))
            .ToListAsync(ct);

        return Ok(participants);
    }

    [HttpPut("{id:guid}/katilim")]
    [EndpointSummary("Aktif bir buluşmaya giriş yapan kullanıcı adına katılır.")]
    [ProducesResponseType<ListingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Join(Guid id, CancellationToken ct) => SetParticipation(id, true, ct);

    [HttpDelete("{id:guid}/katilim")]
    [EndpointSummary("Giriş yapan kullanıcının buluşma katılımını geri alır.")]
    [ProducesResponseType<ListingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Leave(Guid id, CancellationToken ct) => SetParticipation(id, false, ct);

    private async Task<IActionResult> SetParticipation(Guid id, bool attending, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // İlan düzenlenirken veya silinirken katılım uygunluğu değişmesin.
        var lockedIds = await db.Database.SqlQuery<Guid>($"""
            SELECT "Id" AS "Value" FROM "Ilanlar" WHERE "Id" = {id} FOR SHARE
            """).ToListAsync(ct);
        var listingQuery = db.Ilanlar.AsNoTracking().Where(x => x.Id == id && x.Durum != "Gizli");
        var meeting = lockedIds.Count == 0 ? null : await listingQuery
            .Select(x => new { x.Tur, x.Durum, x.BulusmaTarihi }).SingleOrDefaultAsync(ct);
        if (meeting is null) return NotFound(new { message = "İlan bulunamadı." });

        var now = (clock ?? TimeProvider.System).GetUtcNow();
        if (meeting.Tur != "Sosyal" || meeting.Durum != "Aktif" ||
            meeting.BulusmaTarihi is not { } meetingTime || meetingTime <= now)
            return Conflict(new { message = "Bu ilan katılıma açık değil." });

        if (attending)
        {
            // Benzersiz indeks, eşzamanlı katılma isteklerini tek kayda indirir.
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Katilimlar" ("Id", "IlanId", "ProfilId", "KatilimTarihi")
                VALUES ({Guid.NewGuid()}, {id}, {userId}, {now})
                ON CONFLICT ("IlanId", "ProfilId") DO NOTHING
                """, ct);
        }
        else
        {
            await db.Katilimlar.Where(x => x.IlanId == id && x.ProfilId == userId)
                .ExecuteDeleteAsync(ct);
        }

        var listing = await Project(listingQuery).SingleAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(listing);
    }

    private bool TryUserId(out Guid id) => Guid.TryParse(ControllerContext.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out id)
        && id != Guid.Empty;

    private bool ValidateRequest(SaveListingRequest request)
    {
        // Also enforce invariants when called outside the MVC model-validation pipeline.
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request, HttpContext.RequestServices, null), results, true);
        foreach (var result in results)
            foreach (var member in result.MemberNames.DefaultIfEmpty(string.Empty))
                ModelState.TryAddModelError(member, result.ErrorMessage ?? "Geçersiz alan.");
        return ModelState.IsValid;
    }

    private async Task<Kategori?> ValidateCategory(SaveListingRequest request, CancellationToken ct)
    {
        var category = await db.Kategoriler.SingleOrDefaultAsync(x => x.Id == request.KategoriId, ct);
        if (category is null)
            ModelState.TryAddModelError(nameof(request.KategoriId), "Kategori bulunamadı.");
        else if (category.Tur != request.Tur)
            ModelState.TryAddModelError(nameof(request.KategoriId), "Kategori türü ilan türüyle aynı olmalıdır.");
        return ModelState.IsValid ? category : null;
    }

    private static void Apply(Ilan listing, SaveListingRequest request)
    {
        listing.Baslik = request.Baslik.Trim();
        listing.Aciklama = request.Aciklama.Trim();
        listing.KategoriId = request.KategoriId;
        listing.Tur = request.Tur;
        listing.Yer = request.Yer.Trim();
        listing.BulusmaTarihi = request.BulusmaTarihi?.ToUniversalTime();
    }

    private static ListingResponse ToResponse(Ilan x) => new(x.Id, x.SahipId, x.KategoriId, x.Kategori.Ad,
        x.Baslik, x.Aciklama, x.Tur, x.Yer, x.BulusmaTarihi, x.Durum, x.OlusturulmaTarihi);

    private IQueryable<ListingResponse> Project(IQueryable<Ilan> query)
    {
        TryUserId(out var userId);
        return query.Select(x => new ListingResponse(x.Id, x.SahipId, x.KategoriId, x.Kategori.Ad,
            x.Baslik, x.Aciklama, x.Tur, x.Yer, x.BulusmaTarihi, x.Durum, x.OlusturulmaTarihi,
            x.Katilimlar.Count, x.Katilimlar.Any(k => k.ProfilId == userId)));
    }
}


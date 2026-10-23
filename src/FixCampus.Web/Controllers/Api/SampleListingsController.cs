using FixCampus.Web.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FixCampus.Web.Controllers.Api;

[ApiController]
[Route("api/ornek/ilanlar")]
[Produces("application/json")]
public sealed class SampleListingsController : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Bağlantı denemesi için örnek ilanları getirir.")]
    [EndpointDescription("Sosyal buluşma, kayıp ve buluntu için üç sabit örnek döner. Veritabanına bağlanmaz ve kayıt oluşturmaz. Nihai ilan API'si değildir.")]
    [ProducesResponseType<SampleListingsResponse>(StatusCodes.Status200OK)]
    public ActionResult<SampleListingsResponse> Get() => Ok(new SampleListingsResponse(
        DataSource: "sample",
        IsPersisted: false,
        Items:
        [
            new SampleListing(
                Id: Guid.Parse("11111111-1111-4111-8111-111111111111"),
                Type: "social",
                Title: "[Örnek] Kütüphanede çalışma grubu",
                Description: "API bağlantısını denemek için hazırlanmış örnek sosyal buluşma ilanı.",
                Location: "Örnek kampüs kütüphanesi",
                StartsAtUtc: new DateTimeOffset(2026, 10, 25, 11, 0, 0, TimeSpan.Zero)),
            new SampleListing(
                Id: Guid.Parse("22222222-2222-4222-8222-222222222222"),
                Type: "lost",
                Title: "[Örnek] Kayıp su matarası",
                Description: "Kayıp ilanı kartını denemek için hazırlanmış örnek kayıt.",
                Location: "Örnek kampüs yemekhanesi",
                StartsAtUtc: null),
            new SampleListing(
                Id: Guid.Parse("33333333-3333-4333-8333-333333333333"),
                Type: "found",
                Title: "[Örnek] Bulunan defter",
                Description: "Buluntu ilanı kartını denemek için hazırlanmış örnek kayıt.",
                Location: "Örnek öğrenci merkezi",
                StartsAtUtc: null)
        ]));
}

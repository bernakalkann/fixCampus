using FixCampus.Web.Contracts;
using FixCampus.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FixCampus.Web.Controllers.Api;

[ApiController]
[Route("api/veritabani/ornek-ilan")]
public sealed class DatabaseDemoController(
    DemoListingReader reader,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<DatabaseDemoController> logger) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Supabase PostgreSQL veritabanındaki tek demo ilanını okur.")]
    [ProducesResponseType<DatabaseListingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<DatabaseListingResponse>> Get(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("Supabase")))
            return Problem(statusCode: 503, title: "Veritabanı bağlantısı yapılandırılmamış.");
        try
        {
            var listing = await reader.ReadAsync(cancellationToken);
            return listing is null
                ? Problem(statusCode: 404, title: "Demo ilanı bulunamadı. Önce Demo:Seed=true ile örnek kaydı oluşturun.")
                : Ok(listing);
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException)
        {
            // Do not include connection strings or provider exception details in HTTP responses.
            logger.LogWarning("Veritabanı demo sorgusu başarısız. Hata türü: {Type}", ex.GetType().Name);
            return Problem(statusCode: 503, title: "Veritabanına erişilemiyor. Bağlantıyı ve migration durumunu kontrol edin.");
        }
    }
}

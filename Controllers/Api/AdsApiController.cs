using Microsoft.AspNetCore.Mvc;

namespace FixCampus.Controllers.Api;

[ApiController]
[Route("api/ads")]
[Produces("application/json")]
public class AdsApiController : ControllerBase
{
    /// <summary>
    /// fixCampus aktif ilan listesi (Önizleme / İskelet)
    /// </summary>
    [HttpGet]
    public IActionResult GetAds()
    {
        var sampleAds = new[]
        {
            new
            {
                Id = 1,
                Title = "BLM4581 Proje Beyin Fırtınası",
                Type = "Sosyal Radar",
                Location = "Kütüphane 2. Kat Grup Çalışma",
                DateTime = DateTime.Now.AddHours(2),
                AttendeesCount = 4,
                Status = "Aktif"
            },
            new
            {
                Id = 2,
                Title = "Siyah Deri Öğrenci Cüzdanı",
                Type = "Kayıp",
                Location = "Merkezi Yemekhane",
                DateTime = DateTime.Now.AddHours(-1),
                AttendeesCount = 0,
                Status = "Aranıyor"
            }
        };

        return Ok(new
        {
            Success = true,
            Count = sampleAds.Length,
            Data = sampleAds
        });
    }

    /// <summary>
    /// API Sağlık ve Sistem Durumu
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            Service = "fixCampus API",
            Version = "v1.0.0",
            Status = "Online",
            Database = "Supabase PostgreSQL (Planlandı - Hafta 1)",
            Timestamp = DateTime.UtcNow
        });
    }
}

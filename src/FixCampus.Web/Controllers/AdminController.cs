using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixCampus.Web.Controllers;

[Authorize(Roles = "Yonetici")]
public class AdminController : Controller
{
    private readonly ILogger<AdminController> _logger;

    public AdminController(ILogger<AdminController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult HideListing(string listingId)
    {
        TempData["SuccessMessage"] = $"#{listingId} numaralı bildirilmiş ilan başarıyla incelendi ve yayından kaldırıldı (gizlendi).";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApproveListing(string listingId)
    {
        TempData["SuccessMessage"] = $"#{listingId} numaralı bildirim incelendi ve ilanın kurallara uygun olduğuna karar verildi (bildirim kapatıldı).";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddCategory(string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            TempData["ErrorMessage"] = "Kategori adı boş olamaz.";
        }
        else
        {
            TempData["SuccessMessage"] = $"'{categoryName}' kategorisi sisteme başarıyla eklendi.";
        }

        return RedirectToAction("Index");
    }
}

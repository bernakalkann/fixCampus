using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FixCampus.Models;

namespace FixCampus.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "Ana Sayfa";
        return View();
    }

    public IActionResult Radar()
    {
        ViewData["Title"] = "Sosyal Radar - Buluşmalar";
        return View();
    }

    public IActionResult LostFound()
    {
        ViewData["Title"] = "Kayıp & Buluntu İlanları";
        return View();
    }

    public IActionResult Ads()
    {
        ViewData["Title"] = "Tüm İlanlar";
        return View();
    }

    public IActionResult Plan()
    {
        ViewData["Title"] = "Proje Planı ve Görev Dağılımı";
        return View();
    }

    public IActionResult Privacy()
    {
        ViewData["Title"] = "Gizlilik İlkeleri";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

using Microsoft.AspNetCore.Mvc;

namespace FixCampus.Web.Controllers;

public sealed class DatabaseDemoPageController(IWebHostEnvironment environment) : Controller
{
    [HttpGet("/veritabani-ornek")]
    public IActionResult Index() => environment.IsDevelopment()
        ? View("~/Views/DatabaseDemo/Index.cshtml")
        : NotFound();
}

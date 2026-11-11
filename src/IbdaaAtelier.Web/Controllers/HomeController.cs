using Microsoft.AspNetCore.Mvc;

namespace IbdaaAtelier.Web.Controllers;

public sealed class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", User.IsInRole("Admin") ? "Admin" : "Pos");

        return View();
    }

    [HttpGet]
    public IActionResult Error() => View();
}

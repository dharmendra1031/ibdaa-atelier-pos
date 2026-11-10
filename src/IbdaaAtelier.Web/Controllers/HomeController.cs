using Microsoft.AspNetCore.Mvc;

namespace IbdaaAtelier.Web.Controllers;

public sealed class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult Error() => View();
}

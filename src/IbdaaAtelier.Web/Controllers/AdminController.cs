using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IbdaaAtelier.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}

using Microsoft.AspNetCore.Mvc;

namespace luisfrontend.Controllers
{
    public class CocinaController : Controller
    {
        public IActionResult Index() => View();
    }
}
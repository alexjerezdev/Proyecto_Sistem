using Microsoft.AspNetCore.Mvc;

namespace luisfrontend.Controllers
{
    public class ChecklistController : Controller
    {
        public IActionResult Index() => View();
    }
}
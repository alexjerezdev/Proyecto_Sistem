using Microsoft.AspNetCore.Mvc;

namespace luisfrontend.Controllers
{
    public class ReportesController : Controller
    {
        public IActionResult Diario() => View();
        public IActionResult MasVendidos() => View();
        public IActionResult Historico() => View();
    }
}
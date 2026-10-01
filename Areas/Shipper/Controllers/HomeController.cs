using Microsoft.AspNetCore.Mvc;

namespace BeeDienLanh.Areas.Shipper.Controllers
{
    [Area("Shipper")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
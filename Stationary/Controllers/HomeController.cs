using Microsoft.AspNetCore.Mvc;

namespace Stationary.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

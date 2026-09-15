using Microsoft.AspNetCore.Mvc;

namespace golenWeb.Controllers
{
    public class CrudTestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

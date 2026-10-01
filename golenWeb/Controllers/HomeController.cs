using golenWeb.Models;
using golenWeb.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace golenWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly HomeService _homeService;

        public HomeController(HomeService homeService)
        {
            _homeService = homeService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var data = await _homeService.GetHomePageDataAsync();

            ViewData["TodayEvents"] = data.TodayEvents;
            ViewData["AllEvents"] = data.AllEvents;
            ViewData["Bulletins"] = data.Bulletins;

            return View(data);
        }

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

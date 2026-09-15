using golenWeb.Models;
using golenWeb.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace golenWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly EventService _eventService;
        private readonly BulletinService _bulletinService;

        public HomeController(EventService eventService, BulletinService bulletinService)
        {
            _eventService = eventService;
            _bulletinService = bulletinService;
        }

        public async Task<IActionResult> Index()
        {
            var todayEvents = await _eventService.GetTodayEventsAsync();
            var bulletins = await _bulletinService.GetAllAsync();
            ViewData["TodayEvents"] = todayEvents;
            ViewData["Bulletins"] = bulletins;
            return View();
        }

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

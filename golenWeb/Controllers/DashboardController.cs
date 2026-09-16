using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using golenWeb.Services;

namespace golenWeb.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly EventService _eventService;
        private readonly BulletinService _bulletinService;
        private readonly AuthService _authService;

        public DashboardController(EventService eventService, BulletinService bulletinService, AuthService authService)
        {
            _eventService = eventService;
            _bulletinService = bulletinService;
            _authService = authService;
        }

        public async Task<IActionResult> Index()
        {
            var isAdmin = User.IsInRole("Admin");

            var events = await _eventService.GetAllAsync();
            var bulletins = await _bulletinService.GetAllAsync();

            ViewData["EventCount"] = events.Count;
            ViewData["BulletinCount"] = bulletins.Count;
            ViewData["RecentEvents"] = events.Take(3).ToList();
            ViewData["RecentBulletins"] = bulletins.Take(3).ToList();
            ViewData["IsAdmin"] = isAdmin;
            ViewData["Username"] = User.Identity?.Name ?? "User";
            ViewData["Role"] = isAdmin ? "Admin" : "User";

            if (isAdmin)
            {
                ViewData["UserCount"] = await _authService.GetUserCountAsync();
            }

            return View();
        }
    }
}

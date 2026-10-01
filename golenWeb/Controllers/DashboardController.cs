using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using golenWeb.Services;

namespace golenWeb.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly DashboardService _dashboardService;

        public DashboardController(DashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vm = await _dashboardService.GetDashboardOverviewAsync(User);

            ViewData["EventCount"] = vm.EventCount;
            ViewData["BulletinCount"] = vm.BulletinCount;
            ViewData["RecentEvents"] = vm.RecentEvents;
            ViewData["RecentBulletins"] = vm.RecentBulletins;
            ViewData["IsAdmin"] = vm.IsAdmin;
            ViewData["Username"] = vm.Username;
            ViewData["Role"] = vm.Role;
            ViewData["UserCount"] = vm.UserCount;

            return View(vm);
        }
    }
}

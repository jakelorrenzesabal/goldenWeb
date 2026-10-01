using System.Security.Claims;
using golenWeb.Models;

namespace golenWeb.Services
{
    public class DashboardService
    {
        private readonly EventService _eventService;
        private readonly BulletinService _bulletinService;
        private readonly AuthService _authService;

        public DashboardService(EventService eventService, BulletinService bulletinService, AuthService authService)
        {
            _eventService = eventService;
            _bulletinService = bulletinService;
            _authService = authService;
        }

        /// <summary>
        /// Compiles all dashboard statistics and recent data for the current logged-in user.
        /// </summary>
        public async Task<DashboardViewModel> GetDashboardOverviewAsync(ClaimsPrincipal user)
        {
            var isAdmin = user.IsInRole("Admin");
            var username = user.Identity?.Name ?? "User";

            var events = await _eventService.GetAllAsync();
            var bulletins = await _bulletinService.GetAllAsync();

            var vm = new DashboardViewModel
            {
                EventCount = events.Count,
                BulletinCount = bulletins.Count,
                RecentEvents = events.Take(3).ToList(),
                RecentBulletins = bulletins.Take(3).ToList(),
                IsAdmin = isAdmin,
                Username = username,
                Role = isAdmin ? "Admin" : "User",
                UserCount = isAdmin ? await _authService.GetUserCountAsync() : 0
            };

            return vm;
        }
    }
}

using golenWeb.Models;

namespace golenWeb.Services
{
    public class HomeService
    {
        private readonly EventService _eventService;
        private readonly BulletinService _bulletinService;

        public HomeService(EventService eventService, BulletinService bulletinService)
        {
            _eventService = eventService;
            _bulletinService = bulletinService;
        }

        /// <summary>
        /// Retrieves all public home page events and bulletins.
        /// </summary>
        public async Task<HomeViewModel> GetHomePageDataAsync()
        {
            var todayEvents = await _eventService.GetTodayEventsAsync();
            var allEvents = await _eventService.GetAllAsync();
            var bulletins = await _bulletinService.GetAllAsync();

            return new HomeViewModel
            {
                TodayEvents = todayEvents,
                AllEvents = allEvents,
                Bulletins = bulletins
            };
        }
    }
}

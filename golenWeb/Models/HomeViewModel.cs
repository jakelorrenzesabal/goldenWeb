using System.Collections.Generic;

namespace golenWeb.Models
{
    public class HomeViewModel
    {
        public List<EventModel> TodayEvents { get; set; } = new();
        public List<EventModel> AllEvents { get; set; } = new();
        public List<Bulletin> Bulletins { get; set; } = new();
    }
}

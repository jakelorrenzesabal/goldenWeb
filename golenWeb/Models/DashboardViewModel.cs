using System.Collections.Generic;

namespace golenWeb.Models
{
    public class DashboardViewModel
    {
        public int EventCount { get; set; }
        public int BulletinCount { get; set; }
        public int UserCount { get; set; }
        public List<EventModel> RecentEvents { get; set; } = new();
        public List<Bulletin> RecentBulletins { get; set; } = new();
        public bool IsAdmin { get; set; }
        public string Username { get; set; } = "User";
        public string Role { get; set; } = "User";
    }
}

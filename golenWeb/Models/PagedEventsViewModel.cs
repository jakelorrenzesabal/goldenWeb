using System.Collections.Generic;

namespace golenWeb.Models
{
    public class PagedEventsViewModel
    {
        public List<EventModel> Events { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; } = 0;
        public string? CurrentSearch { get; set; }
        public string? CurrentCategory { get; set; }
        public string? SelectedDate { get; set; }
    }
}

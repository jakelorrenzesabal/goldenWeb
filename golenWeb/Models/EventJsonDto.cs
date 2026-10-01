namespace golenWeb.Models
{
    public class EventJsonDto
    {
        public int EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string EventDate { get; set; } = string.Empty;
        public string FormattedDate { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Organizer { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool HasImage { get; set; }
        public string? ImageUrl { get; set; }
    }
}

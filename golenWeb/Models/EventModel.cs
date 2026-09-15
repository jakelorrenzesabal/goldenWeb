using System.ComponentModel.DataAnnotations;

namespace golenWeb.Models
{
    public class EventModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Event title is required")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Event date is required")]
        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; } = DateTime.Today;

        [StringLength(50)]
        public string StartTime { get; set; } = "09:00 AM";

        [StringLength(50)]
        public string EndTime { get; set; } = "04:00 PM";

        [StringLength(200)]
        public string Location { get; set; } = "Main Campus Auditorium";

        [StringLength(100)]
        public string Organizer { get; set; } = "Golden Success College Admin";

        [StringLength(100)]
        public string Category { get; set; } = "Academic"; // Academic, Sports, Seminar, Ceremony, Club, Cultural

        public bool IsFeatured { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

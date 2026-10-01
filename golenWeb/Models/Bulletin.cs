using System.ComponentModel.DataAnnotations;

namespace golenWeb.Models
{
    public class Bulletin
    {
        public int BulletinId { get; set; }

        [Required(ErrorMessage = "Notice title is required")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Notice content is required")]
        public string Content { get; set; } = string.Empty;

        [StringLength(50)]
        public string Category { get; set; } = "General Notice"; // Academic, Urgent Notice, Sports, Club, Registration

        [StringLength(20)]
        public string Priority { get; set; } = "Normal"; // High, Normal, Low

        public DateTime PublishDate { get; set; } = DateTime.Today;

        [StringLength(100)]
        public string Author { get; set; } = "Office of Student Affairs";

        public byte[]? ImageData { get; set; }
        public string? ImageContentType { get; set; }
        public string? ImageHash { get; set; }
        public int? CreatedByUserId { get; set; }
    }
}

namespace golenWeb.Models
{
    public class BulletinJsonDto
    {
        public int BulletinId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string PublishDate { get; set; } = string.Empty;
        public string FormattedDate { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public bool HasImage { get; set; }
        public string? ImageUrl { get; set; }
    }
}

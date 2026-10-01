namespace golenWeb.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        // Stored password hash (do not expose in views)
        public string PasswordHash { get; set; } = string.Empty;
        // Role: "Admin" or "User"
        public string Role { get; set; } = "User";
    }
}

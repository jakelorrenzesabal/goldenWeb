namespace golenWeb.Models
{
    public class SiteSetting
    {
        public int SiteSettingId { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// View model for the site customization form
    /// </summary>
    public class SiteSettingsViewModel
    {
        public string SiteTitle { get; set; } = "Golden Success College";
        public string HeroTitle { get; set; } = "Welcome to Golden Success College";
        public string HeroSubtitle { get; set; } = "Your path to excellence starts here";
        public string Tagline { get; set; } = "Determination • Courage • Hardwork • Isa. 33:6";
        public string AboutText { get; set; } = "Golden Success College is committed to providing quality education and fostering academic excellence.";
        public string FooterText { get; set; } = "Golden Success College • All Rights Reserved";
        public string PrimaryColor { get; set; } = "#0b5e28";
        public string AccentColor { get; set; } = "#e6b800";
        public string NavBackground { get; set; } = "#073d1a";
        public string ContactEmail { get; set; } = "info@goldensuccess.edu";
        public string ContactPhone { get; set; } = "";
        public string Address { get; set; } = "";
        public string AnnouncementBanner { get; set; } = "";
        public bool ShowAnnouncementBanner { get; set; } = false;
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using golenWeb.Services;

namespace golenWeb.Filters
{
    /// <summary>
    /// Action filter that injects SiteSettings into ViewData for every controller action.
    /// This powers the dynamic layout (colors, tagline, title, etc.).
    /// </summary>
    public class SiteSettingsFilter : IAsyncActionFilter
    {
        private readonly SiteSettingsService _siteSettingsService;

        public SiteSettingsFilter(SiteSettingsService siteSettingsService)
        {
            _siteSettingsService = siteSettingsService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Only inject into controllers that return views (not API controllers)
            if (context.Controller is Controller controller)
            {
                try
                {
                    var settings = await _siteSettingsService.GetAllAsync();

                    controller.ViewData["CollegeName"] = settings.GetValueOrDefault("SiteTitle", "Golden Success College");
                    controller.ViewData["NavTagline"] = settings.GetValueOrDefault("Tagline", "Determination • Courage • Hardwork • Isa. 33:6");
                    controller.ViewData["PrimaryColor"] = settings.GetValueOrDefault("PrimaryColor", "#0b5e28");
                    controller.ViewData["AccentColor"] = settings.GetValueOrDefault("AccentColor", "#e6b800");
                    controller.ViewData["NavBackground"] = settings.GetValueOrDefault("NavBackground", "#073d1a");
                    controller.ViewData["FooterText"] = settings.GetValueOrDefault("FooterText", "Golden Success College • All Rights Reserved");
                    controller.ViewData["AnnouncementBanner"] = settings.GetValueOrDefault("AnnouncementBanner", "");
                    controller.ViewData["ShowAnnouncementBanner"] = settings.GetValueOrDefault("ShowAnnouncementBanner", "false");
                }
                catch
                {
                    // If DB isn't ready yet, fall back to defaults silently
                }
            }

            await next();
        }
    }
}

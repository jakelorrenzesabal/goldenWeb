using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using golenWeb.Models;
using golenWeb.Services;

namespace golenWeb.Controllers
{
    public class SiteSettingsController : Controller
    {
        private readonly SiteSettingsService _siteSettingsService;

        public SiteSettingsController(SiteSettingsService siteSettingsService)
        {
            _siteSettingsService = siteSettingsService;
        }

        // Both Admin and User roles can view & save customizations
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vm = await _siteSettingsService.GetViewModelAsync();
            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(SiteSettingsViewModel model)
        {
            var settings = new Dictionary<string, string>
            {
                ["SiteTitle"] = model.SiteTitle ?? "Golden Success College",
                ["HeroTitle"] = model.HeroTitle ?? "Welcome to Golden Success College",
                ["HeroSubtitle"] = model.HeroSubtitle ?? "Your path to excellence starts here",
                ["Tagline"] = model.Tagline ?? "Determination • Courage • Hardwork • Isa. 33:6",
                ["AboutText"] = model.AboutText ?? "",
                ["FooterText"] = model.FooterText ?? "Golden Success College • All Rights Reserved",
                ["PrimaryColor"] = model.PrimaryColor ?? "#0b5e28",
                ["AccentColor"] = model.AccentColor ?? "#e6b800",
                ["NavBackground"] = model.NavBackground ?? "#073d1a",
                ["ContactEmail"] = model.ContactEmail ?? "",
                ["ContactPhone"] = model.ContactPhone ?? "",
                ["Address"] = model.Address ?? "",
                ["AnnouncementBanner"] = model.AnnouncementBanner ?? "",
                ["ShowAnnouncementBanner"] = model.ShowAnnouncementBanner ? "true" : "false"
            };

            await _siteSettingsService.SetBulkAsync(settings);
            TempData["SuccessMessage"] = "Site settings saved successfully! Changes are now live.";
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetDefaults()
        {
            var defaults = new Dictionary<string, string>
            {
                ["SiteTitle"] = "Golden Success College",
                ["HeroTitle"] = "Welcome to Golden Success College",
                ["HeroSubtitle"] = "Your path to excellence starts here",
                ["Tagline"] = "Determination • Courage • Hardwork • Isa. 33:6",
                ["AboutText"] = "Golden Success College is committed to providing quality education and fostering academic excellence in a nurturing Christian environment.",
                ["FooterText"] = "Golden Success College • All Rights Reserved",
                ["PrimaryColor"] = "#0b5e28",
                ["AccentColor"] = "#e6b800",
                ["NavBackground"] = "#073d1a",
                ["ContactEmail"] = "info@goldensuccess.edu",
                ["ContactPhone"] = "",
                ["Address"] = "",
                ["AnnouncementBanner"] = "",
                ["ShowAnnouncementBanner"] = "false"
            };

            await _siteSettingsService.SetBulkAsync(defaults);
            TempData["SuccessMessage"] = "Site settings reset to defaults.";
            return RedirectToAction("Index");
        }
    }
}

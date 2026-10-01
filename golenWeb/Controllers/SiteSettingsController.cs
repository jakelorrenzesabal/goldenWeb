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
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            await _siteSettingsService.SaveViewModelAsync(model);
            TempData["SuccessMessage"] = "Site settings saved successfully! Changes are now live.";
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetDefaults()
        {
            await _siteSettingsService.ResetToDefaultsAsync();
            TempData["SuccessMessage"] = "Site settings reset to defaults.";
            return RedirectToAction("Index");
        }
    }
}

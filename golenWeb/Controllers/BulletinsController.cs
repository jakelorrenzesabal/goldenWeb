using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using golenWeb.Models;
using golenWeb.Services;

namespace golenWeb.Controllers
{
    public class BulletinsController : Controller
    {
        private readonly BulletinService _bulletinService;
        private readonly FileStorageService _fileStorage;

        public BulletinsController(BulletinService bulletinService, FileStorageService fileStorage)
        {
            _bulletinService = bulletinService;
            _fileStorage = fileStorage;
        }

        // GET: /Bulletins — public
        [HttpGet]
        public async Task<IActionResult> Index(string? category)
        {
            ViewData["CurrentCategory"] = category;
            var list = await _bulletinService.GetAllAsync(category);
            return View(list);
        }

        // GET: /Bulletins/Create — requires login
        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Bulletin
            {
                PublishDate = DateTime.Today,
                Category = "General Notice",
                Priority = "Normal",
                Author = "Office of Student Affairs"
            });
        }

        // POST: /Bulletins/Create — requires login
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Bulletin bulletin, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(bulletin);

            if (imageFile != null && imageFile.Length > 0)
            {
                bulletin.ImageUrl = await _fileStorage.SaveImageAsync(imageFile, "uploads/bulletins");
            }

            await _bulletinService.CreateAsync(bulletin);
            TempData["SuccessMessage"] = $"Bulletin '{bulletin.Title}' posted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Bulletins/Edit/5 — requires login
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var bulletin = await _bulletinService.GetByIdAsync(id);
            if (bulletin == null) return NotFound();
            return View(bulletin);
        }

        // POST: /Bulletins/Edit/5 — requires login
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Bulletin bulletin, IFormFile? imageFile)
        {
            if (id != bulletin.Id) return BadRequest();
            if (!ModelState.IsValid) return View(bulletin);

            if (imageFile != null && imageFile.Length > 0)
            {
                bulletin.ImageUrl = await _fileStorage.SaveImageAsync(imageFile, "uploads/bulletins");
            }
            else
            {
                var existing = await _bulletinService.GetByIdAsync(id);
                if (existing != null)
                {
                    bulletin.ImageUrl = existing.ImageUrl;
                }
            }

            var updated = await _bulletinService.UpdateAsync(bulletin);
            if (!updated) return NotFound();

            TempData["SuccessMessage"] = $"Bulletin '{bulletin.Title}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Bulletins/Delete/5 — requires login
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _bulletinService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Bulletin notice removed successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}

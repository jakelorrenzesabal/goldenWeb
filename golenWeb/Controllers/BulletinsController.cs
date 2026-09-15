using Microsoft.AspNetCore.Mvc;
using golenWeb.Models;
using golenWeb.Services;

namespace golenWeb.Controllers
{
    public class BulletinsController : Controller
    {
        private readonly BulletinService _bulletinService;

        public BulletinsController(BulletinService bulletinService)
        {
            _bulletinService = bulletinService;
        }

        // GET: /Bulletins
        [HttpGet]
        public async Task<IActionResult> Index(string? category)
        {
            ViewData["CurrentCategory"] = category;
            var list = await _bulletinService.GetAllAsync(category);
            return View(list);
        }

        // GET: /Bulletins/Create
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

        // POST: /Bulletins/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Bulletin bulletin)
        {
            if (!ModelState.IsValid) return View(bulletin);

            await _bulletinService.CreateAsync(bulletin);
            TempData["SuccessMessage"] = $"Bulletin '{bulletin.Title}' posted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Bulletins/Delete/5
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

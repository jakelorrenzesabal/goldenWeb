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

        // GET: /Bulletins/Details/5 — public
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var bulletin = await _bulletinService.GetByIdAsync(id);
            if (bulletin == null)
            {
                return NotFound();
            }
            return View(bulletin);
        }

        // GET: /Bulletins/GetJson/5 — public API for modal
        [HttpGet]
        public async Task<IActionResult> GetJson(int id)
        {
            var dto = await _bulletinService.GetBulletinJsonDtoAsync(id);
            if (dto == null)
            {
                return NotFound();
            }
            return Json(dto);
        }

        // GET: /Bulletins/Create — requires login
        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            return View(_bulletinService.GetDefaultBulletin());
        }

        // POST: /Bulletins/Create — requires login
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Bulletin bulletin, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(bulletin);

            var (success, error) = await _bulletinService.ProcessAndSaveBulletinAsync(bulletin, imageFile, _fileStorage);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to create bulletin");
                return View(bulletin);
            }

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
            if (!ModelState.IsValid) return View(bulletin);

            var (success, error) = await _bulletinService.ProcessAndUpdateBulletinAsync(id, bulletin, imageFile, _fileStorage);
            if (!success)
            {
                if (error == "ID mismatch" || error == "Bulletin notice not found") return NotFound();
                ModelState.AddModelError(string.Empty, error ?? "Failed to update bulletin");
                return View(bulletin);
            }

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

        // GET: /Bulletins/Image/5 — public
        [HttpGet("Bulletins/Image/{id}")]
        public async Task<IActionResult> Image(int id)
        {
            var b = await _bulletinService.GetByIdAsync(id);
            if (b == null || b.ImageData == null || b.ImageData.Length == 0)
                return NotFound();

            if (!string.IsNullOrEmpty(b.ImageHash))
            {
                var eTag = $"\"{b.ImageHash}\"";
                if (Request.Headers.IfNoneMatch == eTag)
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }
                Response.Headers.ETag = eTag;
                Response.Headers.CacheControl = "public,max-age=86400";
            }

            return File(b.ImageData, b.ImageContentType ?? "image/jpeg");
        }
    }
}

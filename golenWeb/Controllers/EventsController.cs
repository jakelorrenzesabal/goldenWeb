using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using golenWeb.Models;
using golenWeb.Services;

namespace golenWeb.Controllers
{
    public class EventsController : Controller
    {
        private readonly EventService _eventService;
        private readonly FileStorageService _fileStorage;

        public EventsController(EventService eventService, FileStorageService fileStorage)
        {
            _eventService = eventService;
            _fileStorage = fileStorage;
        }

        // GET: /Events — public
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? category, DateTime? date, int page = 1)
        {
            var pagedVm = await _eventService.GetPagedEventsViewModelAsync(search, category, date, page);

            ViewData["CurrentSearch"] = pagedVm.CurrentSearch;
            ViewData["CurrentCategory"] = pagedVm.CurrentCategory;
            ViewData["SelectedDate"] = pagedVm.SelectedDate;
            ViewData["CurrentPage"] = pagedVm.CurrentPage;
            ViewData["TotalPages"] = pagedVm.TotalPages;
            ViewData["TotalCount"] = pagedVm.TotalCount;

            return View(pagedVm.Events);
        }

        // GET: /Events/Details/5 — public
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var ev = await _eventService.GetByIdAsync(id);
            if (ev == null)
            {
                return NotFound();
            }
            return View(ev);
        }

        // GET: /Events/GetJson/5 — public API for modal
        [HttpGet]
        public async Task<IActionResult> GetJson(int id)
        {
            var dto = await _eventService.GetEventJsonDtoAsync(id);
            if (dto == null)
            {
                return NotFound();
            }
            return Json(dto);
        }

        // GET: /Events/Create — requires login
        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            return View(_eventService.GetDefaultEventModel());
        }

        // POST: /Events/Create — requires login
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventModel ev, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(ev);
            }

            var (success, error) = await _eventService.ProcessAndSaveEventAsync(ev, imageFile, _fileStorage);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to create event");
                return View(ev);
            }

            TempData["SuccessMessage"] = $"Event '{ev.Title}' has been scheduled successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Events/Edit/5 — requires login
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var ev = await _eventService.GetByIdAsync(id);
            if (ev == null)
            {
                return NotFound();
            }
            return View(ev);
        }

        // POST: /Events/Edit/5 — requires login
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventModel ev, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(ev);
            }

            var (success, error) = await _eventService.ProcessAndUpdateEventAsync(id, ev, imageFile, _fileStorage);
            if (!success)
            {
                if (error == "ID mismatch" || error == "Event not found") return NotFound();
                ModelState.AddModelError(string.Empty, error ?? "Failed to update event");
                return View(ev);
            }

            TempData["SuccessMessage"] = $"Event '{ev.Title}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Events/Delete/5 — requires login
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _eventService.GetByIdAsync(id);
            if (ev == null) return NotFound();
            return View(ev);
        }

        // POST: /Events/DeleteConfirmed/5 — requires login
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ev = await _eventService.GetByIdAsync(id);
            var title = ev?.Title ?? "Event";

            await _eventService.DeleteAsync(id);
            TempData["SuccessMessage"] = $"Event '{title}' deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Events/DeleteAll — requires login
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            var count = await _eventService.DeleteAllAsync();
            TempData["SuccessMessage"] = $"All campus events ({count}) have been deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Events/Image/5 — public
        [HttpGet("Events/Image/{id}")]
        public async Task<IActionResult> Image(int id)
        {
            var ev = await _eventService.GetByIdAsync(id);
            if (ev == null || ev.ImageData == null || ev.ImageData.Length == 0)
                return NotFound();

            if (!string.IsNullOrEmpty(ev.ImageHash))
            {
                var eTag = $"\"{ev.ImageHash}\"";
                if (Request.Headers.IfNoneMatch == eTag)
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }
                Response.Headers.ETag = eTag;
                Response.Headers.CacheControl = "public,max-age=86400";
            }

            return File(ev.ImageData, ev.ImageContentType ?? "image/jpeg");
        }
    }
}

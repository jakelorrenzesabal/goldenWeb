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
        public async Task<IActionResult> Index(string? search, string? category, DateTime? date)
        {
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentCategory"] = category;
            ViewData["SelectedDate"] = date?.ToString("yyyy-MM-dd");

            var events = await _eventService.GetAllAsync(search, category, date);
            return View(events);
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
            var ev = await _eventService.GetByIdAsync(id);
            if (ev == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = ev.Id,
                title = ev.Title,
                description = ev.Description,
                eventDate = ev.EventDate.ToString("yyyy-MM-dd"),
                formattedDate = ev.EventDate.ToString("MMMM dd, yyyy (ddd)"),
                startTime = ev.StartTime,
                endTime = ev.EndTime,
                location = ev.Location,
                organizer = ev.Organizer,
                category = ev.Category,
                hasImage = ev.ImageData != null && ev.ImageData.Length > 0,
                imageUrl = ev.ImageData != null && ev.ImageData.Length > 0 ? $"/Events/Image/{ev.Id}" : null
            });
        }

        // GET: /Events/Create — requires login
        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new EventModel
            {
                EventDate = DateTime.Today,
                StartTime = "09:00 AM",
                EndTime = "04:00 PM",
                Location = "Golden Success College Main Auditorium",
                Organizer = "Student Affairs Office",
                Category = "Academic"
            });
        }

        // POST: /Events/Create — requires login
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventModel ev, IFormFile? imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var (data, contentType, hash, error) = await _fileStorage.ProcessAndStoreImageInDbAsync(imageFile);
                if (!string.IsNullOrEmpty(error))
                {
                    ModelState.AddModelError(string.Empty, error);
                    return View(ev);
                }
                ev.ImageData = data;
                ev.ImageContentType = contentType;
                ev.ImageHash = hash;
            }

            if (!ModelState.IsValid)
            {
                return View(ev);
            }

            await _eventService.CreateAsync(ev);
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
            if (id != ev.Id) return BadRequest();

            if (imageFile != null && imageFile.Length > 0)
            {
                var (data, contentType, hash, error) = await _fileStorage.ProcessAndStoreImageInDbAsync(imageFile);
                if (!string.IsNullOrEmpty(error))
                {
                    ModelState.AddModelError(string.Empty, error);
                    return View(ev);
                }
                ev.ImageData = data;
                ev.ImageContentType = contentType;
                ev.ImageHash = hash;
            }
            else
            {
                var existing = await _eventService.GetByIdAsync(id);
                if (existing != null)
                {
                    ev.ImageData = existing.ImageData;
                    ev.ImageContentType = existing.ImageContentType;
                    ev.ImageHash = existing.ImageHash;
                }
            }

            if (!ModelState.IsValid)
            {
                return View(ev);
            }

            var updated = await _eventService.UpdateAsync(ev);
            if (!updated) return NotFound();

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

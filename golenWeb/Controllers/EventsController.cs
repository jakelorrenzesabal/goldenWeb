using Microsoft.AspNetCore.Mvc;
using golenWeb.Models;
using golenWeb.Services;

namespace golenWeb.Controllers
{
    public class EventsController : Controller
    {
        private readonly EventService _eventService;

        public EventsController(EventService eventService)
        {
            _eventService = eventService;
        }

        // GET: /Events
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? category, DateTime? date)
        {
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentCategory"] = category;
            ViewData["SelectedDate"] = date?.ToString("yyyy-MM-dd");

            var events = await _eventService.GetAllAsync(search, category, date);
            return View(events);
        }

        // GET: /Events/Create
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

        // POST: /Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventModel ev)
        {
            if (!ModelState.IsValid)
            {
                return View(ev);
            }

            await _eventService.CreateAsync(ev);
            TempData["SuccessMessage"] = $"Event '{ev.Title}' has been scheduled successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Events/Edit/5
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

        // POST: /Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventModel ev)
        {
            if (id != ev.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                return View(ev);
            }

            var updated = await _eventService.UpdateAsync(ev);
            if (!updated) return NotFound();

            TempData["SuccessMessage"] = $"Event '{ev.Title}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Events/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _eventService.GetByIdAsync(id);
            if (ev == null) return NotFound();
            return View(ev);
        }

        // POST: /Events/DeleteConfirmed/5
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

        // POST: /Events/DeleteAll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            var count = await _eventService.DeleteAllAsync();
            TempData["SuccessMessage"] = $"All campus events ({count}) have been deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}

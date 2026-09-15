using Microsoft.AspNetCore.Mvc;
using golenWeb.Models;
using golenWeb.Services;

namespace golenWeb.Controllers.Api
{
    [ApiController]
    [Route("api/events")]
    [Produces("application/json")]
    public class EventsApiController : ControllerBase
    {
        private readonly EventService _eventService;

        public EventsApiController(EventService eventService)
        {
            _eventService = eventService;
        }

        // GET: api/events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventModel>>> GetAll([FromQuery] string? search, [FromQuery] string? category, [FromQuery] DateTime? date)
        {
            var events = await _eventService.GetAllAsync(search, category, date);
            return Ok(events);
        }

        // GET: api/events/today
        [HttpGet("today")]
        public async Task<ActionResult<IEnumerable<EventModel>>> GetTodayEvents()
        {
            var events = await _eventService.GetTodayEventsAsync();
            return Ok(events);
        }

        // GET: api/events/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<EventModel>> GetById(int id)
        {
            var ev = await _eventService.GetByIdAsync(id);
            if (ev == null)
            {
                return NotFound(new { message = $"Event with ID {id} was not found." });
            }
            return Ok(ev);
        }

        // POST: api/events
        [HttpPost]
        public async Task<ActionResult<EventModel>> Create([FromBody] EventModel ev)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = await _eventService.CreateAsync(ev);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/events/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] EventModel ev)
        {
            ev.Id = id;
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _eventService.UpdateAsync(ev);
            if (!updated)
            {
                return NotFound(new { message = $"Event with ID {id} was not found." });
            }

            return Ok(new { message = $"Event #{id} updated successfully.", eventItem = ev });
        }

        // DELETE: api/events/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _eventService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Event with ID {id} was not found." });
            }

            return Ok(new { message = $"Event #{id} deleted successfully." });
        }
    }
}

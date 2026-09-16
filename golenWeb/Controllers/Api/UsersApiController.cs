using Microsoft.AspNetCore.Mvc;
using golenWeb.Models;
using golenWeb.Services;

namespace golenWeb.Controllers.Api
{
    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? NewPassword { get; set; }
    }

    [ApiController]
    [Route("api/users")]
    [Produces("application/json")]
    public class UsersApiController : ControllerBase
    {
        private readonly AuthService _authService;

        public UsersApiController(AuthService authService)
        {
            _authService = authService;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAll()
        {
            var users = await _authService.GetAllAsync();
            var sanitized = users.Select(u => new { u.Id, u.Username, u.Email });
            return Ok(sanitized);
        }

        // GET: api/users/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> GetById(int id)
        {
            var user = await _authService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} was not found." });
            }
            return Ok(new { user.Id, user.Username, user.Email });
        }

        // POST: api/users
        [HttpPost]
        public async Task<ActionResult<object>> Create([FromBody] CreateUserRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest(new { message = "Username and Password are required." });
            }

            try
            {
                var newId = await _authService.CreateUserAsync(req.Username, req.Email, req.Password);
                return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId, req.Username, req.Email });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/users/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest req)
        {
            var user = await _authService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} was not found." });
            }

            await _authService.UpdateUserAsync(id, req.Username, req.Email, user.Role, req.NewPassword);
            return Ok(new { message = $"User {id} updated successfully.", id, req.Username, req.Email });
        }

        // DELETE: api/users/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _authService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} was not found." });
            }

            await _authService.DeleteUserAsync(id);
            return Ok(new { message = $"User {id} deleted successfully." });
        }
    }
}

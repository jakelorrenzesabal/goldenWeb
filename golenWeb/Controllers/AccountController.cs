using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using golenWeb.Models;
using golenWeb.Services;

namespace golenWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _auth;

        public AccountController(AuthService auth)
        {
            _auth = auth;
        }

        // ─── Public: Login / Register / Logout ───────────────────────────

        [HttpGet]
        [Route("login")]
        [Route("portal")]
        [Route("admin/login")]
        [Route("Account/Login")]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [Route("login")]
        [Route("portal")]
        [Route("admin/login")]
        [Route("Account/Login")]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            var user = await _auth.ValidateUserAsync(username, password);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                return View();
            }

            var principal = _auth.CreateClaimsPrincipal(user);
            await HttpContext.SignInAsync("MyCookieAuth", principal);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // Redirect to dashboard after login
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        [Route("register")]
        [Route("Account/Register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [Route("register")]
        [Route("Account/Register")]
        public async Task<IActionResult> Register(string username, string email, string password)
        {
            try
            {
                // New registrations always get "User" role; Admin assigns roles from the dashboard
                await _auth.CreateUserAsync(username, email, password, "User");
                TempData["SuccessMessage"] = "Account created! You can now log in.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Index", "Home");
        }

        // ─── Admin-only: User Management CRUD ────────────────────────────

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _auth.GetAllAsync();
            return View(users);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create() => View();

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(string username, string email, string password, string role = "User")
        {
            try
            {
                await _auth.CreateUserAsync(username, email, password, role);
                TempData["SuccessMessage"] = $"User '{username}' created successfully with role '{role}'.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _auth.GetByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Edit(int id, string username, string email, string role, string? newPassword)
        {
            await _auth.UpdateUserAsync(id, username, email, role, newPassword);
            TempData["SuccessMessage"] = $"User '{username}' updated successfully.";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _auth.GetByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _auth.DeleteUserAsync(id);
            TempData["SuccessMessage"] = "User account deleted.";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ChangeRole(int id, string role)
        {
            await _auth.UpdateRoleAsync(id, role);
            TempData["SuccessMessage"] = $"Role updated to '{role}' successfully.";
            return RedirectToAction("Index");
        }
    }
}

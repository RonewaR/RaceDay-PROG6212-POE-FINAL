using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _factory;

        public AuthController(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _factory.CreateClient("RaceDayAPI");

            var response = await client.PostAsJsonAsync(
                "api/Auth/register", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Registration successful. Please log in.";

                return RedirectToAction("Login");
            }

            ViewBag.Error =
                "Registration failed: " +
                await response.Content.ReadAsStringAsync();

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _factory.CreateClient("RaceDayAPI");

            var response = await client.PostAsJsonAsync(
                "api/Auth/login", model);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Invalid email or password.";
                return View(model);
            }

            var json = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(json);

            string? token = FindJsonValue(
                document.RootElement,
                "token", "jwtToken", "accessToken");

            string? role = FindJsonValue(
                document.RootElement,
                "role");

            // Retrieve the logged-in user's ID.
            string? userId = FindJsonValue(
                document.RootElement,
                "userId");

            // If role is not returned separately, read it
            // from the JWT claims.
            if (!string.IsNullOrWhiteSpace(token) &&
                string.IsNullOrWhiteSpace(role))
            {
                role = GetRoleFromToken(token);
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Error =
                    "Login response did not contain a JWT token. " +
                    "Please check the API login response.";

                return View(model);
            }

            // Clear any previous session before starting a new one.
            HttpContext.Session.Clear();

            // Store authentication details in session.
            HttpContext.Session.SetString("JWTToken", token);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                HttpContext.Session.SetString("UserId", userId);
            }

            HttpContext.Session.SetString(
                "UserRole", role ?? "Participant");

            HttpContext.Session.SetString(
                "UserEmail", model.Email);

            if (role == "Organiser")
            {
                return RedirectToAction("Index", "Organiser");
            }

            return RedirectToAction("Index", "Participant");
        }

        // TEMPORARY: Displays the JWT token for Swagger testing.
        [HttpGet]
        public IActionResult ShowToken()
        {
            var token = HttpContext.Session.GetString("JWTToken");

            if (string.IsNullOrEmpty(token))
            {
                return Content(
                    "No token found. Please log in again.");
            }

            return Content(token);
        }

        // Finds values in JSON objects and arrays.
        // Supports both string and numeric values.
        private static string? FindJsonValue(
            JsonElement element, params string[] names)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    foreach (var name in names)
                    {
                        if (property.Name.Equals(
                            name, StringComparison.OrdinalIgnoreCase))
                        {
                            if (property.Value.ValueKind ==
                                JsonValueKind.String)
                            {
                                return property.Value.GetString();
                            }

                            if (property.Value.ValueKind ==
                                JsonValueKind.Number)
                            {
                                return property.Value.ToString();
                            }
                        }
                    }

                    var result = FindJsonValue(
                        property.Value, names);

                    if (!string.IsNullOrWhiteSpace(result))
                        return result;
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var result = FindJsonValue(item, names);

                    if (!string.IsNullOrWhiteSpace(result))
                        return result;
                }
            }

            return null;
        }

        // Reads the role from the JWT token if necessary.
        private static string? GetRoleFromToken(string token)
        {
            try
            {
                var parts = token.Split('.');

                if (parts.Length != 3)
                    return null;

                var payload = parts[1]
                    .Replace('-', '+')
                    .Replace('_', '/');

                payload = payload.PadRight(
                    payload.Length +
                    (4 - payload.Length % 4) % 4, '=');

                var bytes = Convert.FromBase64String(payload);

                using var document = JsonDocument.Parse(bytes);

                return FindJsonValue(
                    document.RootElement,
                    "role",
                    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
            }
            catch
            {
                return null;
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }
    }
}
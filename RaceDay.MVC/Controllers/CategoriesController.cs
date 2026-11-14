using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.MVC.Models;

namespace RaceDay.MVC.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly IHttpClientFactory _factory;

        public CategoriesController(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        // Display categories belonging to the logged-in organiser
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsOrganiser())
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var client = HttpClientWithToken();

                var response =
                    await client.GetAsync("api/categories/organiser");

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error =
                        "Could not load categories from the API.";

                    return View(new List<CategoryViewModel>());
                }

                var json =
                    await response.Content.ReadAsStringAsync();

                var categories =
                    JsonSerializer.Deserialize<List<CategoryViewModel>>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<CategoryViewModel>();

                return View(categories);
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Error loading categories: " + ex.Message;

                return View(new List<CategoryViewModel>());
            }
        }

        // Display category creation page
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!IsOrganiser())
            {
                return RedirectToAction("Login", "Auth");
            }

            await LoadEvents();

            return View(new CategoryViewModel());
        }

        // Create a category
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            if (!IsOrganiser())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                await LoadEvents();
                return View(model);
            }

            try
            {
                var client = HttpClientWithToken();

                var request = new
                {
                    EventId = model.EventId,
                    Name = model.Name,
                    DistanceKm = model.DistanceKm,
                    EntryFee = model.EntryFee,
                    StartTime = model.StartTime
                };

                var response = await client.PostAsJsonAsync(
                    "api/categories", request);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Category created successfully.";

                    return RedirectToAction(nameof(Index));
                }

                var error =
                    await response.Content.ReadAsStringAsync();

                ViewBag.Error =
                    "Could not create category: " + error;
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Error creating category: " + ex.Message;
            }

            await LoadEvents();

            return View(model);
        }

        // Display category editing page
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsOrganiser())
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var client = HttpClientWithToken();

                var response =
                    await client.GetAsync("api/categories/organiser");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] =
                        "Could not load category.";

                    return RedirectToAction(nameof(Index));
                }

                var json =
                    await response.Content.ReadAsStringAsync();

                var categories =
                    JsonSerializer.Deserialize<List<CategoryViewModel>>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<CategoryViewModel>();

                var category = categories.FirstOrDefault(
                    c => c.CategoryId == id);

                if (category == null)
                {
                    TempData["Error"] = "Category not found.";
                    return RedirectToAction(nameof(Index));
                }

                await LoadEvents();

                return View(category);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Error loading category: " + ex.Message;

                return RedirectToAction(nameof(Index));
            }
        }

        // Update a category
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, CategoryViewModel model)
        {
            if (!IsOrganiser())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                await LoadEvents();
                return View(model);
            }

            try
            {
                var client = HttpClientWithToken();

                var request = new
                {
                    EventId = model.EventId,
                    Name = model.Name,
                    DistanceKm = model.DistanceKm,
                    EntryFee = model.EntryFee,
                    StartTime = model.StartTime
                };

                var response = await client.PutAsJsonAsync(
                    $"api/categories/{id}", request);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Category updated successfully.";

                    return RedirectToAction(nameof(Index));
                }

                var error =
                    await response.Content.ReadAsStringAsync();

                ViewBag.Error =
                    "Could not update category: " + error;
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Error updating category: " + ex.Message;
            }

            await LoadEvents();

            return View(model);
        }

        // Delete a category
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsOrganiser())
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var client = HttpClientWithToken();

                var response =
                    await client.DeleteAsync(
                        $"api/categories/{id}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Category deleted successfully.";
                }
                else
                {
                    var error =
                        await response.Content.ReadAsStringAsync();

                    TempData["Error"] =
                        "Could not delete category: " + error;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Error deleting category: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // Load only the logged-in organiser's events
        private async Task LoadEvents()
        {
            var client = HttpClientWithToken();

            var response =
                await client.GetAsync("api/events");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Events = new List<EventViewModel>();
                return;
            }

            var json =
                await response.Content.ReadAsStringAsync();

            var events =
                JsonSerializer.Deserialize<List<EventViewModel>>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<EventViewModel>();

            var organiserIdText =
                HttpContext.Session.GetString("UserId");

            if (int.TryParse(organiserIdText, out int organiserId))
            {
                events = events
                    .Where(e => e.OrganiserId == organiserId)
                    .ToList();
            }
            else
            {
                events = new List<EventViewModel>();
            }

            ViewBag.Events = events;
        }

        // Create an API client with the organiser's JWT token
        private HttpClient HttpClientWithToken()
        {
            var client = _factory.CreateClient("RaceDayAPI");

            var token =
                HttpContext.Session.GetString("JWTToken");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        // Check organiser login and role
        private bool IsOrganiser()
        {
            return
                !string.IsNullOrEmpty(
                    HttpContext.Session.GetString("JWTToken"))
                &&
                HttpContext.Session.GetString("UserRole") == "Organiser";
        }
    }
}
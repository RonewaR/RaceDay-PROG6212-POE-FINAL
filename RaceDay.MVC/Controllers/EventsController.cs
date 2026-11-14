using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Linq;
using RaceDay.MVC.Models;

namespace RaceDay.MVC.Controllers
{
    public class EventsController : Controller
    {
        private readonly IHttpClientFactory _factory;

        public EventsController(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        private bool IsOrganiser()
        {
            return !string.IsNullOrEmpty(
                HttpContext.Session.GetString("JWTToken"))
                && HttpContext.Session.GetString("UserRole") == "Organiser";
        }

        private HttpClient CreateAuthenticatedClient()
        {
            var client = _factory.CreateClient("RaceDayAPI");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    HttpContext.Session.GetString("JWTToken"));

            return client;
        }

        private async Task<string> UploadBannerAsync(
            HttpClient client, IFormFile bannerFile)
        {
            using var form = new MultipartFormDataContent();

            using var stream = bannerFile.OpenReadStream();

            using var fileContent = new StreamContent(stream);

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(bannerFile.ContentType)
                    ? "application/octet-stream"
                    : bannerFile.ContentType);

            form.Add(fileContent, "file", bannerFile.FileName);

            var response = await client.PostAsync(
                "api/ImageUpload/event-banner", form);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(error);
            }

            var result = await response.Content
                .ReadFromJsonAsync<BannerUploadResponse>();

            if (result == null || string.IsNullOrWhiteSpace(result.ImageUrl))
            {
                throw new InvalidOperationException(
                    "Azure did not return an image URL.");
            }

            return result.ImageUrl;
        }

        // Display all events
        public async Task<IActionResult> Index()
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            var client = CreateAuthenticatedClient();

            var response = await client.GetAsync("api/events");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Could not load events from the API.";
                return View(new List<EventViewModel>());
            }

            var events = await response.Content
                .ReadFromJsonAsync<List<EventViewModel>>();

            return View(events ?? new List<EventViewModel>());
        }

        // Display Create Event form
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            return View();
        }

        // Create event and upload its banner
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            EventViewModel model, IFormFile? bannerFile)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            if (bannerFile == null || bannerFile.Length == 0)
            {
                ModelState.AddModelError(
                    "bannerFile", "Please select an event banner image.");
            }
            else if (bannerFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(
                    "bannerFile", "The banner must not exceed 5 MB.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var client = CreateAuthenticatedClient();

            try
            {
                model.ImageUrl = await UploadBannerAsync(client, bannerFile!);

                var response = await client.PostAsJsonAsync(
                    "api/events", model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Event created successfully!";
                    return RedirectToAction("Index");
                }

                ViewBag.Error = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Event creation failed: " + ex.Message;
            }

            return View(model);
        }

        // Display Edit Event form
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            var client = CreateAuthenticatedClient();

            var response = await client.GetAsync("api/events");

            if (!response.IsSuccessStatusCode)
                return RedirectToAction("Index");

            var events = await response.Content
                .ReadFromJsonAsync<List<EventViewModel>>();

            var eventItem = events?.FirstOrDefault(e => e.EventId == id);

            if (eventItem == null)
                return NotFound();

            return View(eventItem);
        }

        // Update event and optionally replace its banner
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, EventViewModel model, IFormFile? bannerFile)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            if (bannerFile != null && bannerFile.Length > 0
                && bannerFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(
                    "bannerFile", "The banner must not exceed 5 MB.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var client = CreateAuthenticatedClient();

            try
            {
                // Replace the existing banner only when a new file is selected.
                if (bannerFile != null && bannerFile.Length > 0)
                {
                    model.ImageUrl = await UploadBannerAsync(
                        client, bannerFile);
                }

                var updateData = new
                {
                    model.Title,
                    model.Description,
                    model.EventDate,
                    model.Location,
                    model.ImageUrl
                };

                var response = await client.PutAsJsonAsync(
                    $"api/events/{id}", updateData);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Event updated successfully!";
                    return RedirectToAction("Index");
                }

                ViewBag.Error = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Event update failed: " + ex.Message;
            }

            return View(model);
        }

        // Delete event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            var client = CreateAuthenticatedClient();

            var response = await client.DeleteAsync($"api/events/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Event deleted successfully!";
            }
            else
            {
                TempData["Error"] =
                    await response.Content.ReadAsStringAsync();
            }

            return RedirectToAction("Index");
        }

        private class BannerUploadResponse
        {
            public string ImageUrl { get; set; } = "";
        }
    }
}
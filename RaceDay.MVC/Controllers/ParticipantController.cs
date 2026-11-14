using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.MVC.Models;

namespace RaceDay.MVC.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly IHttpClientFactory _factory;

        public ParticipantController(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        // Participant Dashboard
        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("JWTToken")))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (HttpContext.Session.GetString("UserRole") != "Participant")
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // Display upcoming events with search and location filtering
        [HttpGet]
        public async Task<IActionResult> Events(
            string? searchTerm,
            string? locationFilter)
        {
            if (!IsParticipant())
            {
                return RedirectToAction("Login", "Auth");
            }

            var client = _factory.CreateClient("RaceDayAPI");

            var token = HttpContext.Session.GetString("JWTToken");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var model = new ParticipantEventsViewModel
            {
                SearchTerm = searchTerm ?? string.Empty,
                LocationFilter = locationFilter ?? string.Empty
            };

            // Load events from the API
            var eventResponse = await client.GetAsync("api/events");

            if (!eventResponse.IsSuccessStatusCode)
            {
                ViewBag.Error = "Could not load available events.";
                return View(model);
            }

            model.Events =
                await eventResponse.Content.ReadFromJsonAsync
                <List<EventViewModel>>()
                ?? new List<EventViewModel>();

            // Load categories from the API
            var categoryResponse =
                await client.GetAsync("api/categories");

            if (!categoryResponse.IsSuccessStatusCode)
            {
                ViewBag.Error = "Could not load event categories.";
                return View(model);
            }

            model.Categories =
                await categoryResponse.Content.ReadFromJsonAsync
                <List<CategoryViewModel>>()
                ?? new List<CategoryViewModel>();

            // Display upcoming events only
            model.Events = model.Events
                .Where(e => e.EventDate >= DateTime.Now)
                .ToList();

            // Search by event title or description
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                model.Events = model.Events
                    .Where(e =>
                        (e.Title ?? "").Contains(
                            searchTerm,
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        (e.Description ?? "").Contains(
                            searchTerm,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Filter events by location
            if (!string.IsNullOrWhiteSpace(locationFilter))
            {
                model.Events = model.Events
                    .Where(e =>
                        (e.Location ?? "").Contains(
                            locationFilter,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return View(model);
        }

        // Display the logged-in participant's enrolments
        [HttpGet]
        public async Task<IActionResult> MyEnrolments()
        {
            if (!IsParticipant())
            {
                return RedirectToAction("Login", "Auth");
            }

            var client = _factory.CreateClient("RaceDayAPI");

            var token = HttpContext.Session.GetString("JWTToken");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("api/enrolments/my");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Could not load your enrolments.";
                return View(new List<MyEnrolmentViewModel>());
            }

            var enrolments =
                await response.Content.ReadFromJsonAsync
                <List<MyEnrolmentViewModel>>()
                ?? new List<MyEnrolmentViewModel>();

            return View(enrolments);
        }

        // Display the logged-in participant's race results
        [HttpGet]
        public async Task<IActionResult> Results()
        {
            if (!IsParticipant())
            {
                return RedirectToAction("Login", "Auth");
            }

            var client = _factory.CreateClient("RaceDayAPI");

            var token = HttpContext.Session.GetString("JWTToken");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("api/results");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Could not load your race results.";
                return View(new List<ResultViewModel>());
            }

            var results =
                await response.Content.ReadFromJsonAsync
                <List<ResultViewModel>>()
                ?? new List<ResultViewModel>();

            return View(results);
        }

        // Enrol participant in a category
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(int categoryId)
        {
            if (!IsParticipant())
            {
                return RedirectToAction("Login", "Auth");
            }

            var client = _factory.CreateClient("RaceDayAPI");

            var token = HttpContext.Session.GetString("JWTToken");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync(
                "api/enrolments",
                new { CategoryId = categoryId });

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "You have successfully enrolled in the event category.";
            }
            else
            {
                var error =
                    await response.Content.ReadAsStringAsync();

                TempData["Error"] =
                    "Enrolment failed: " + error;
            }

            return RedirectToAction(nameof(Events));
        }

        // Check participant login and role
        private bool IsParticipant()
        {
            return
                !string.IsNullOrEmpty(
                    HttpContext.Session.GetString("JWTToken"))
                &&
                HttpContext.Session.GetString("UserRole") == "Participant";
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.MVC.Models;

namespace RaceDay.MVC.Controllers
{
    public class OrganiserController : Controller
    {
        // Organiser Dashboard
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsOrganiser())
            {
                return RedirectToAction("Login", "Auth");
            }

            var model = new OrganiserDashboardViewModel();

            try
            {
                // Get the logged-in organiser's ID
                var organiserIdText =
                    HttpContext.Session.GetString("UserId");

                if (!int.TryParse(organiserIdText, out int organiserId))
                {
                    ViewBag.Error =
                        "Organiser ID is missing. Please log out and log in again.";

                    return View(model);
                }

                var client = HttpClientWithToken();

                // Retrieve all events from the API
                var eventsResponse =
                    await client.GetAsync("api/events");

                if (!eventsResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error =
                        "Could not load events from the API.";

                    return View(model);
                }

                var eventsJson =
                    await eventsResponse.Content.ReadAsStringAsync();

                var allEvents =
                    JsonSerializer.Deserialize<List<EventViewModel>>(
                        eventsJson,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<EventViewModel>();

                // Keep only this organiser's events
                var organiserEvents = allEvents
                    .Where(e => e.OrganiserId == organiserId)
                    .ToList();

                // Retrieve this organiser's enrolments
                var enrolmentsResponse =
                    await client.GetAsync("api/enrolments");

                if (!enrolmentsResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error =
                        "Could not load enrolments from the API.";

                    return View(model);
                }

                var enrolmentsJson =
                    await enrolmentsResponse.Content.ReadAsStringAsync();

                var enrolments =
                    JsonSerializer.Deserialize<List<MyEnrolmentViewModel>>(
                        enrolmentsJson,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<MyEnrolmentViewModel>();

                // Calculate dashboard totals
                model.TotalEvents = organiserEvents.Count;

                model.TotalEnrolments = enrolments.Count;

                model.UpcomingEvents = organiserEvents.Count(
                    e => e.EventDate.Date >= DateTime.Today);

                // Create a summary for each event
                model.Events = organiserEvents
                    .OrderBy(e => e.EventDate)
                    .Select(e => new OrganiserEventSummaryViewModel
                    {
                        EventId = e.EventId,
                        Title = e.Title,
                        EventDate = e.EventDate,
                        Location = e.Location,
                        EnrolmentCount = enrolments.Count(
                            enrolment => enrolment.EventId == e.EventId)
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Dashboard error: " + ex.Message;
            }

            return View(model);
        }

        // Display participant enrolments for the organiser
        [HttpGet]
        public async Task<IActionResult> ManageEnrolments()
        {
            if (!IsOrganiser())
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var client = HttpClientWithToken();

                var response =
                    await client.GetAsync("api/enrolments");

                var responseContent =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error =
                        $"API Error ({(int)response.StatusCode}): {responseContent}";

                    return View(new List<MyEnrolmentViewModel>());
                }

                var enrolments =
                    JsonSerializer.Deserialize<List<MyEnrolmentViewModel>>(
                        responseContent,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<MyEnrolmentViewModel>();

                return View(enrolments);
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Error loading participant enrolments: " + ex.Message;

                return View(new List<MyEnrolmentViewModel>());
            }
        }

        // Display enrolments for managing race results
        [HttpGet]
        public async Task<IActionResult> ManageResults()
        {
            if (!IsOrganiser())
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var client = HttpClientWithToken();

                var response = await client.GetAsync("api/enrolments");

                var responseContent =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error =
                        $"API Error ({(int)response.StatusCode}): {responseContent}";

                    return View(new List<MyEnrolmentViewModel>());
                }

                var enrolments =
                    JsonSerializer.Deserialize<List<MyEnrolmentViewModel>>(
                        responseContent,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (enrolments == null)
                {
                    ViewBag.Error =
                        "The API returned an empty or invalid response.";

                    return View(new List<MyEnrolmentViewModel>());
                }

                ViewBag.DebugMessage =
                    $"API returned {enrolments.Count} enrolment(s).";

                return View(enrolments);
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Error loading enrolments: " + ex.Message;

                return View(new List<MyEnrolmentViewModel>());
            }
        }

        // Record a participant's race result
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordResult(
            int enrolmentId,
            string? finishTime,
            int? position,
            bool isDnf)
        {
            if (!IsOrganiser())
            {
                return RedirectToAction("Login", "Auth");
            }

            TimeOnly? parsedFinishTime = null;

            if (!isDnf && !string.IsNullOrWhiteSpace(finishTime))
            {
                if (!TimeOnly.TryParse(finishTime, out var parsed))
                {
                    TempData["Error"] =
                        "Please enter a valid finish time.";

                    return RedirectToAction(nameof(ManageResults));
                }

                parsedFinishTime = parsed;
            }

            var client = HttpClientWithToken();

            var result = new
            {
                EnrolmentId = enrolmentId,
                FinishTime = isDnf ? null : parsedFinishTime,
                Position = isDnf ? null : position,
                IsDnf = isDnf
            };

            var response = await client.PostAsJsonAsync(
                "api/results", result);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Race result recorded successfully.";
            }
            else
            {
                var error =
                    await response.Content.ReadAsStringAsync();

                TempData["Error"] =
                    "Could not record result: " + error;
            }

            return RedirectToAction(nameof(ManageResults));
        }

        // Create an API client with the organiser's JWT token
        private HttpClient HttpClientWithToken()
        {
            var client = HttpContext.RequestServices
                .GetRequiredService<IHttpClientFactory>()
                .CreateClient("RaceDayAPI");

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
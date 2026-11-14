using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.MVC.Models;

namespace RaceDay.MVC.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IHttpClientFactory _factory;

        public ProfileController(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        // Display the logged-in user's profile
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Auth");

            var client = GetClient();

            var response = await client.GetAsync("api/users/me");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Could not load your profile.";
                return View(new ProfileViewModel());
            }

            var profile =
                await response.Content.ReadFromJsonAsync<ProfileViewModel>();

            if (profile == null)
            {
                ViewBag.Error = "Profile information was not returned.";
                return View(new ProfileViewModel());
            }

            return View(profile);
        }

        // Update name and email
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ProfileViewModel model)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View("Index", model);

            var client = GetClient();

            var response = await client.PutAsJsonAsync(
                "api/users/me",
                new
                {
                    model.FullName,
                    model.Email
                });

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Your profile was updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            var error = await response.Content.ReadAsStringAsync();

            ViewBag.Error = "Could not update your profile: " + error;

            return View("Index", model);
        }

        // Upload profile picture to Azure Blob Storage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPicture(IFormFile file)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Auth");

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select an image.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var client = GetClient();

                using var content = new MultipartFormDataContent();

                using var fileStream = file.OpenReadStream();

                using var streamContent = new StreamContent(fileStream);

                streamContent.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        file.ContentType ?? "application/octet-stream");

                content.Add(streamContent, "file", file.FileName);

                // Upload image to Azure through the API
                var uploadResponse = await client.PostAsync(
                    "api/ImageUpload/profile-picture", content);

                if (!uploadResponse.IsSuccessStatusCode)
                {
                    var error =
                        await uploadResponse.Content.ReadAsStringAsync();

                    TempData["Error"] =
                        "Image upload failed: " + error;

                    return RedirectToAction(nameof(Index));
                }

                var uploadResult =
                    await uploadResponse.Content.ReadFromJsonAsync<ImageUploadResponse>();

                if (uploadResult == null ||
                    string.IsNullOrWhiteSpace(uploadResult.ImageUrl))
                {
                    TempData["Error"] =
                        "Azure did not return an image URL.";

                    return RedirectToAction(nameof(Index));
                }

                // Save the Azure image URL to the user's profile
                var updateResponse = await client.PutAsJsonAsync(
                    "api/users/me/profile-picture",
                    new
                    {
                        ProfilePictureUrl = uploadResult.ImageUrl
                    });

                if (updateResponse.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Your profile picture was uploaded successfully!";
                }
                else
                {
                    var error =
                        await updateResponse.Content.ReadAsStringAsync();

                    TempData["Error"] =
                        "Image uploaded, but profile update failed: " + error;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "An error occurred while uploading your picture: " +
                    ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // Create an authenticated API client
        private HttpClient GetClient()
        {
            var client = _factory.CreateClient("RaceDayAPI");

            var token = HttpContext.Session.GetString("JWTToken");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        // Check login session
        private bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(
                HttpContext.Session.GetString("JWTToken"));
        }
    }

    // Model for the image upload API response
    public class ImageUploadResponse
    {
        public string Message { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;
    }
}
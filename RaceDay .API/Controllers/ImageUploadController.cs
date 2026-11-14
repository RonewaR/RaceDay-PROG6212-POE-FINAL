using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RaceDay_.API.Services;

namespace RaceDay_.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ImageUploadController : ControllerBase
    {
        private readonly AzureBlobStorageService _storageService;

        public ImageUploadController(
            AzureBlobStorageService storageService)
        {
            _storageService = storageService;
        }

        // Upload participant profile picture
        [HttpPost("profile-picture")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> UploadProfilePicture(
            IFormFile file)
        {
            try
            {
                string imageUrl =
                    await _storageService.UploadImageAsync(
                        file, "profile-pictures");

                return Ok(new
                {
                    message = "Profile picture uploaded successfully.",
                    imageUrl
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while uploading the profile picture."
                });
            }
        }

        // Upload event banner
        [HttpPost("event-banner")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UploadEventBanner(
            IFormFile file)
        {
            try
            {
                string imageUrl =
                    await _storageService.UploadImageAsync(
                        file, "event-banners");

                return Ok(new
                {
                    message = "Event banner uploaded successfully.",
                    imageUrl
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while uploading the event banner."
                });
            }
        }
    }
}
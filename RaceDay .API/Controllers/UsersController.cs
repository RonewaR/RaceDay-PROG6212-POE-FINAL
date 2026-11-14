using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using RaceDay_.API.Data;
using RaceDay_.API.Models;

namespace RaceDay_.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public UsersController(RaceDayDbContext context)
        {
            _context = context;
        }

        // Organiser: View all users
        [HttpGet]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new
                {
                    u.UserId,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.CreatedAt,
                    u.ProfilePictureUrl
                })
                .ToListAsync();

            return Ok(users);
        }

        // View logged-in user's profile
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new
                {
                    u.UserId,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.CreatedAt,
                    u.ProfilePictureUrl
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound("User profile not found.");

            return Ok(user);
        }

        // Update logged-in user's profile
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(
            [FromBody] UpdateProfileRequest request)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound("User profile not found.");

            var fullName = request.FullName.Trim();
            var email = request.Email.Trim();

            var emailExists = await _context.Users.AnyAsync(
                u => u.Email == email && u.UserId != userId);

            if (emailExists)
                return Conflict("This email address is already in use.");

            user.FullName = fullName;
            user.Email = email;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile updated successfully.",
                user.UserId,
                user.FullName,
                user.Email,
                user.Role,
                user.CreatedAt,
                user.ProfilePictureUrl
            });
        }

        // Update logged-in user's profile picture URL
        [HttpPut("me/profile-picture")]
        public async Task<IActionResult> UpdateProfilePicture(
            [FromBody] UpdateProfilePictureRequest request)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound("User profile not found.");

            user.ProfilePictureUrl = request.ProfilePictureUrl?.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile picture updated successfully.",
                user.ProfilePictureUrl
            });
        }

        // Read the logged-in user's ID from the JWT
        private bool TryGetUserId(out int userId)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                        ?? User.FindFirst("userId");

            return int.TryParse(claim?.Value, out userId);
        }
    }

    public class UpdateProfileRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateProfilePictureRequest
    {
        [StringLength(500)]
        public string? ProfilePictureUrl { get; set; }
    }
}
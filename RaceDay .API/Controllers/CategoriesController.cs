using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using RaceDay_.API.Data;
using RaceDay_.API.Models;

namespace RaceDay_.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public CategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        // Participants can still browse all categories.
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/Categories/organiser
        // Organisers can view only categories belonging to their events.
        [Authorize(Roles = "Organiser")]
        [HttpGet("organiser")]
        public async Task<IActionResult> GetOrganiserCategories()
        {
            if (!TryGetOrganiserId(out int organiserId))
            {
                return Unauthorized();
            }

            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.Event.OrganiserId == organiserId)
                .ToListAsync();

            return Ok(categories);
        }

        // POST: api/Categories
        // Only the event's organiser can create a category.
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        public async Task<IActionResult> CreateCategory(
            [FromBody] CategoryRequest request)
        {
            if (!TryGetOrganiserId(out int organiserId))
            {
                return Unauthorized();
            }

            var eventExists = await _context.Events
                .AnyAsync(e => e.EventId == request.EventId);

            if (!eventExists)
            {
                return BadRequest(new
                {
                    message = "The specified event does not exist."
                });
            }

            var eventBelongsToOrganiser = await _context.Events
                .AnyAsync(e =>
                    e.EventId == request.EventId &&
                    e.OrganiserId == organiserId);

            if (!eventBelongsToOrganiser)
            {
                return Forbid();
            }

            var category = new Category
            {
                EventId = request.EventId,
                Name = request.Name,
                DistanceKm = request.DistanceKm,
                EntryFee = request.EntryFee,
                StartTime = request.StartTime
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Category created successfully.",
                category.CategoryId,
                category.EventId,
                category.Name
            });
        }

        // PUT: api/Categories/{id}
        // Only the event's organiser can update a category.
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCategory(
            int id,
            [FromBody] CategoryRequest request)
        {
            if (!TryGetOrganiserId(out int organiserId))
            {
                return Unauthorized();
            }

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new
                {
                    message = "Category not found."
                });
            }

            if (category.Event.OrganiserId != organiserId)
            {
                return Forbid();
            }

            var eventExists = await _context.Events
                .AnyAsync(e => e.EventId == request.EventId);

            if (!eventExists)
            {
                return BadRequest(new
                {
                    message = "The specified event does not exist."
                });
            }

            var targetEventBelongsToOrganiser =
                await _context.Events.AnyAsync(e =>
                    e.EventId == request.EventId &&
                    e.OrganiserId == organiserId);

            if (!targetEventBelongsToOrganiser)
            {
                return Forbid();
            }

            category.EventId = request.EventId;
            category.Name = request.Name;
            category.DistanceKm = request.DistanceKm;
            category.EntryFee = request.EntryFee;
            category.StartTime = request.StartTime;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Category updated successfully.",
                category.CategoryId,
                category.EventId,
                category.Name
            });
        }

        // DELETE: api/Categories/{id}
        // Only the event's organiser can delete a category.
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (!TryGetOrganiserId(out int organiserId))
            {
                return Unauthorized();
            }

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new
                {
                    message = "Category not found."
                });
            }

            if (category.Event.OrganiserId != organiserId)
            {
                return Forbid();
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Category deleted successfully."
            });
        }

        // Read the organiser ID from the JWT token.
        private bool TryGetOrganiserId(out int organiserId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(userId, out organiserId);
        }
    }

    public class CategoryRequest
    {
        [Range(1, int.MaxValue)]
        public int EventId { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.01", "1000000")]
        public decimal DistanceKm { get; set; }

        [Range(typeof(decimal), "0", "1000000")]
        public decimal EntryFee { get; set; }

        public DateTime StartTime { get; set; }
    }
}
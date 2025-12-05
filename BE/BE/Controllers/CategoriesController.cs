using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Models.Data;
using BE.Models.Entities;

namespace BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppBookStoreContext _context;

        public CategoriesController(AppBookStoreContext context)
        {
            _context = context;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName
                })
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/categories/{category_id}/books
        [HttpGet("{categoryId}/books")]
        public async Task<ActionResult<IEnumerable<object>>> GetBooksByCategory(int categoryId)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
            {
                return NotFound(new { message = "Category not found" });
            }

            var books = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Category)
                .Where(b => b.CategoryId == categoryId)
                .Select(b => new
                {
                    b.BookId,
                    b.Title,
                    b.Price,
                    b.Quantity,
                    b.Description,
                    b.ImageUrl,
                    b.CreatedAt,
                    Author = b.Author != null ? new { b.Author.AuthorId, b.Author.AuthorName } : null,
                    Publisher = b.Publisher != null ? new { b.Publisher.PublisherId, b.Publisher.PublisherName } : null,
                    Category = b.Category != null ? new { b.Category.CategoryId, b.Category.CategoryName } : null
                })
                .ToListAsync();

            return Ok(books);
        }
    }
}


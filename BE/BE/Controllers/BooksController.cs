using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Models.Data;
using BE.Models.Entities;

namespace BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly AppBookStoreContext _context;

        public BooksController(AppBookStoreContext context)
        {
            _context = context;
        }

        // GET: api/books
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetBooks(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = "title",
            [FromQuery] string? sortOrder = "asc")
        {
            var query = _context.Books
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Category)
                .AsQueryable();

            // Sorting
            if (sortOrder?.ToLower() == "desc")
            {
                query = sortBy?.ToLower() switch
                {
                    "price" => query.OrderByDescending(b => b.Price),
                    "created_at" => query.OrderByDescending(b => b.CreatedAt),
                    _ => query.OrderByDescending(b => b.Title)
                };
            }
            else
            {
                query = sortBy?.ToLower() switch
                {
                    "price" => query.OrderBy(b => b.Price),
                    "created_at" => query.OrderBy(b => b.CreatedAt),
                    _ => query.OrderBy(b => b.Title)
                };
            }

            var totalCount = await query.CountAsync();
            var books = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

            return Ok(new
            {
                data = books,
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        // GET: api/books/{book_id}
        [HttpGet("{bookId}")]
        public async Task<ActionResult<object>> GetBook(int bookId)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Category)
                .Where(b => b.BookId == bookId)
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
                .FirstOrDefaultAsync();

            if (book == null)
            {
                return NotFound(new { message = "Book not found" });
            }

            return Ok(book);
        }

        // GET: api/books/search?q={keyword}
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<object>>> SearchBooks([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { message = "Search keyword is required" });
            }

            var books = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Category)
                .Where(b => b.Title != null && b.Title.Contains(q) ||
                            b.Author != null && b.Author.AuthorName != null && b.Author.AuthorName.Contains(q) ||
                            b.Description != null && b.Description.Contains(q))
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


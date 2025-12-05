using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Models.Data;
using BE.Models.Entities;

namespace BE.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/books")]
    public class AdminBooksController : ControllerBase
    {
        private readonly AppBookStoreContext _context;

        public AdminBooksController(AppBookStoreContext context)
        {
            _context = context;
        }

        // GET: api/admin/books
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetBooks()
        {
            var books = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Category)
                .Select(b => new
                {
                    b.BookId,
                    b.Title,
                    b.Price,
                    b.Quantity,
                    b.Description,
                    b.ImageUrl,
                    b.CreatedAt,
                    b.CategoryId,
                    b.AuthorId,
                    b.PublisherId,
                    Author = b.Author != null ? new { b.Author.AuthorId, b.Author.AuthorName } : null,
                    Publisher = b.Publisher != null ? new { b.Publisher.PublisherId, b.Publisher.PublisherName } : null,
                    Category = b.Category != null ? new { b.Category.CategoryId, b.Category.CategoryName } : null
                })
                .ToListAsync();

            return Ok(books);
        }

        // GET: api/admin/books/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetBook(int id)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Category)
                .Where(b => b.BookId == id)
                .Select(b => new
                {
                    b.BookId,
                    b.Title,
                    b.Price,
                    b.Quantity,
                    b.Description,
                    b.ImageUrl,
                    b.CreatedAt,
                    b.CategoryId,
                    b.AuthorId,
                    b.PublisherId,
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

        // POST: api/admin/books
        [HttpPost]
        public async Task<ActionResult<object>> CreateBook([FromBody] CreateBookRequest request)
        {
            var book = new Book
            {
                Title = request.Title,
                Price = request.Price,
                Quantity = request.Quantity,
                Description = request.Description,
                ImageUrl = request.ImageUrl,
                CategoryId = request.CategoryId,
                AuthorId = request.AuthorId,
                PublisherId = request.PublisherId,
                CreatedAt = DateTime.Now
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBook), new { id = book.BookId }, new
            {
                message = "Book created successfully",
                bookId = book.BookId
            });
        }

        // PUT: api/admin/books/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateBook(int id, [FromBody] UpdateBookRequest request)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound(new { message = "Book not found" });
            }

            book.Title = request.Title ?? book.Title;
            book.Price = request.Price ?? book.Price;
            book.Quantity = request.Quantity ?? book.Quantity;
            book.Description = request.Description ?? book.Description;
            book.ImageUrl = request.ImageUrl ?? book.ImageUrl;
            book.CategoryId = request.CategoryId ?? book.CategoryId;
            book.AuthorId = request.AuthorId ?? book.AuthorId;
            book.PublisherId = request.PublisherId ?? book.PublisherId;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Book updated successfully" });
        }

        // DELETE: api/admin/books/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound(new { message = "Book not found" });
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Book deleted successfully" });
        }
    }

    public class CreateBookRequest
    {
        public string? Title { get; set; }
        public decimal? Price { get; set; }
        public int? Quantity { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int? CategoryId { get; set; }
        public int? AuthorId { get; set; }
        public int? PublisherId { get; set; }
    }

    public class UpdateBookRequest
    {
        public string? Title { get; set; }
        public decimal? Price { get; set; }
        public int? Quantity { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int? CategoryId { get; set; }
        public int? AuthorId { get; set; }
        public int? PublisherId { get; set; }
    }
}


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Models.Data;
using BE.Models.Entities;

namespace BE.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/authors")]
    public class AdminAuthorsController : ControllerBase
    {
        private readonly AppBookStoreContext _context;

        public AdminAuthorsController(AppBookStoreContext context)
        {
            _context = context;
        }

        // GET: api/admin/authors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAuthors()
        {
            var authors = await _context.Authors
                .Select(a => new
                {
                    a.AuthorId,
                    a.AuthorName
                })
                .ToListAsync();

            return Ok(authors);
        }

        // GET: api/admin/authors/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetAuthor(int id)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author == null)
            {
                return NotFound(new { message = "Author not found" });
            }

            return Ok(new
            {
                author.AuthorId,
                author.AuthorName
            });
        }

        // POST: api/admin/authors
        [HttpPost]
        public async Task<ActionResult<object>> CreateAuthor([FromBody] CreateAuthorRequest request)
        {
            var author = new Author
            {
                AuthorName = request.AuthorName
            };

            _context.Authors.Add(author);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAuthor), new { id = author.AuthorId }, new
            {
                message = "Author created successfully",
                authorId = author.AuthorId
            });
        }

        // PUT: api/admin/authors/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAuthor(int id, [FromBody] UpdateAuthorRequest request)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author == null)
            {
                return NotFound(new { message = "Author not found" });
            }

            author.AuthorName = request.AuthorName ?? author.AuthorName;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Author updated successfully" });
        }

        // DELETE: api/admin/authors/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAuthor(int id)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author == null)
            {
                return NotFound(new { message = "Author not found" });
            }

            _context.Authors.Remove(author);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Author deleted successfully" });
        }
    }

    public class CreateAuthorRequest
    {
        public string? AuthorName { get; set; }
    }

    public class UpdateAuthorRequest
    {
        public string? AuthorName { get; set; }
    }
}


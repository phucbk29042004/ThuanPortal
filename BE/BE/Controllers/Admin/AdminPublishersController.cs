using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Models.Data;
using BE.Models.Entities;

namespace BE.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/publishers")]
    public class AdminPublishersController : ControllerBase
    {
        private readonly AppBookStoreContext _context;

        public AdminPublishersController(AppBookStoreContext context)
        {
            _context = context;
        }

        // GET: api/admin/publishers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPublishers()
        {
            var publishers = await _context.Publishers
                .Select(p => new
                {
                    p.PublisherId,
                    p.PublisherName
                })
                .ToListAsync();

            return Ok(publishers);
        }

        // GET: api/admin/publishers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPublisher(int id)
        {
            var publisher = await _context.Publishers.FindAsync(id);
            if (publisher == null)
            {
                return NotFound(new { message = "Publisher not found" });
            }

            return Ok(new
            {
                publisher.PublisherId,
                publisher.PublisherName
            });
        }

        // POST: api/admin/publishers
        [HttpPost]
        public async Task<ActionResult<object>> CreatePublisher([FromBody] CreatePublisherRequest request)
        {
            var publisher = new Publisher
            {
                PublisherName = request.PublisherName
            };

            _context.Publishers.Add(publisher);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPublisher), new { id = publisher.PublisherId }, new
            {
                message = "Publisher created successfully",
                publisherId = publisher.PublisherId
            });
        }

        // PUT: api/admin/publishers/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdatePublisher(int id, [FromBody] UpdatePublisherRequest request)
        {
            var publisher = await _context.Publishers.FindAsync(id);
            if (publisher == null)
            {
                return NotFound(new { message = "Publisher not found" });
            }

            publisher.PublisherName = request.PublisherName ?? publisher.PublisherName;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Publisher updated successfully" });
        }

        // DELETE: api/admin/publishers/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePublisher(int id)
        {
            var publisher = await _context.Publishers.FindAsync(id);
            if (publisher == null)
            {
                return NotFound(new { message = "Publisher not found" });
            }

            _context.Publishers.Remove(publisher);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Publisher deleted successfully" });
        }
    }

    public class CreatePublisherRequest
    {
        public string? PublisherName { get; set; }
    }

    public class UpdatePublisherRequest
    {
        public string? PublisherName { get; set; }
    }
}


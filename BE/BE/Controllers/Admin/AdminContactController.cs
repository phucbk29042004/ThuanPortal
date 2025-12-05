using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Models.Data;
using BE.Models.Entities;

namespace BE.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/contact")]
    public class AdminContactController : ControllerBase
    {
        private readonly AppBookStoreContext _context;

        public AdminContactController(AppBookStoreContext context)
        {
            _context = context;
        }

        // GET: api/admin/contact
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetContacts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Contacts
                .OrderByDescending(c => c.CreatedAt)
                .AsQueryable();

            var totalCount = await query.CountAsync();
            var contacts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.ContactId,
                    c.Name,
                    c.Email,
                    c.Message,
                    c.Status,
                    c.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                data = contacts,
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        // GET: api/admin/contact/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetContact(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null)
            {
                return NotFound(new { message = "Contact not found" });
            }

            return Ok(new
            {
                contact.ContactId,
                contact.Name,
                contact.Email,
                contact.Message,
                contact.Status,
                contact.CreatedAt
            });
        }

        // PUT: api/admin/contact/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateContactStatus(int id, [FromBody] UpdateContactStatusRequest request)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null)
            {
                return NotFound(new { message = "Contact not found" });
            }

            contact.Status = request.Status ?? contact.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Contact status updated successfully" });
        }
    }

    public class UpdateContactStatusRequest
    {
        public string? Status { get; set; }
    }
}


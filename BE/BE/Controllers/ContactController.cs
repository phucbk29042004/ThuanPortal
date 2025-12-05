using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Models.Data;
using BE.Models.Entities;

namespace BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly AppBookStoreContext _context;

        public ContactController(AppBookStoreContext context)
        {
            _context = context;
        }

        // POST: api/contact
        [HttpPost]
        public async Task<ActionResult<object>> CreateContact([FromBody] CreateContactRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Name, email, and message are required" });
            }

            var contact = new Contact
            {
                Name = request.Name,
                Email = request.Email,
                Message = request.Message,
                Status = "pending",
                CreatedAt = DateTime.Now
            };

            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Contact message sent successfully",
                contactId = contact.ContactId
            });
        }
    }

    public class CreateContactRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}


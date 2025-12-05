using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Models.Data;
using BE.Models.Entities;

namespace BE.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly AppBookStoreContext _context;

        public UsersController(AppBookStoreContext context)
        {
            _context = context;
        }

        // GET: api/users/{userId}
        [HttpGet("{userId}")]
        public async Task<ActionResult<object>> GetUser(int userId)
        {
            var user = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new
                {
                    u.UserId,
                    u.FullName,
                    u.Email,
                    u.Phone,
                    u.Role,
                    u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }

        // GET: api/users/{userId}/orders
        [HttpGet("{userId}/orders")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserOrders(int userId)
        {
            // TODO: Implement authentication to verify userId matches token
            var orders = await _context.Orders
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Book)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.OrderId,
                    o.TotalPrice,
                    o.Status,
                    o.CreatedAt,
                    OrderDetails = o.OrderDetails!.Select(od => new
                    {
                        od.OrderDetailId,
                        od.BookId,
                        od.Quantity,
                        od.Price,
                        Book = od.Book != null ? new
                        {
                            od.Book.Title,
                            od.Book.ImageUrl
                        } : null
                    })
                })
                .ToListAsync();

            return Ok(orders);
        }

        // PUT: api/users/{userId}
        [HttpPut("{userId}")]
        public async Task<ActionResult<object>> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                user.FullName = request.FullName;
            }
            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                user.Phone = request.Phone;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User updated successfully",
                user = new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.Phone,
                    user.Role
                }
            });
        }
    }

    public class UpdateUserRequest
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
    }
}


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Models.Data;
using BE.Models.Entities;

namespace BE.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/orders")]
    public class AdminOrdersController : ControllerBase
    {
        private readonly AppBookStoreContext _context;

        public AdminOrdersController(AppBookStoreContext context)
        {
            _context = context;
        }

        // GET: api/admin/orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Book)
                .OrderByDescending(o => o.CreatedAt)
                .AsQueryable();

            var totalCount = await query.CountAsync();
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.OrderId,
                    o.UserId,
                    o.TotalPrice,
                    o.Status,
                    o.CreatedAt,
                    User = o.User != null ? new
                    {
                        o.User.FullName,
                        o.User.Email
                    } : null,
                    OrderDetails = o.OrderDetails!.Select(od => new
                    {
                        od.OrderDetailId,
                        od.BookId,
                        od.Quantity,
                        od.Price,
                        Book = od.Book != null ? new
                        {
                            od.Book.Title
                        } : null
                    })
                })
                .ToListAsync();

            return Ok(new
            {
                data = orders,
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        // GET: api/admin/orders/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Book)
                .Include(o => o.Payments)
                .Where(o => o.OrderId == id)
                .Select(o => new
                {
                    o.OrderId,
                    o.UserId,
                    o.TotalPrice,
                    o.Status,
                    o.CreatedAt,
                    User = o.User != null ? new
                    {
                        o.User.FullName,
                        o.User.Email,
                        o.User.Phone
                    } : null,
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
                    }),
                    Payments = o.Payments!.Select(p => new
                    {
                        p.PaymentId,
                        p.Amount,
                        p.PaymentMethod,
                        p.PaymentStatus
                    })
                })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            return Ok(order);
        }

        // PUT: api/admin/orders/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            order.Status = request.Status ?? order.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order status updated successfully" });
        }
    }

    public class UpdateOrderStatusRequest
    {
        public string? Status { get; set; }
    }
}


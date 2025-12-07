using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Models.Data;
using BE.Models.Entities;

namespace BE.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/dashboard")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly AppBookStoreContext _context;

        public AdminDashboardController(AppBookStoreContext context)
        {
            _context = context;
        }

        // GET: api/admin/dashboard/stats
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetDashboardStats()
        {
            try
            {
                // Total books
                var totalBooks = await _context.Books.CountAsync();

                // Total users
                var totalUsers = await _context.Users.CountAsync();

                // Total orders
                var totalOrders = await _context.Orders.CountAsync();

                // Total revenue (sum of all completed/delivered orders)
                var totalRevenue = await _context.Orders
                    .Where(o => o.Status == "delivered" || o.Status == "completed")
                    .SumAsync(o => (decimal?)o.TotalPrice) ?? 0;

                return Ok(new
                {
                    totalBooks,
                    totalUsers,
                    totalOrders,
                    totalRevenue
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error calculating dashboard stats: " + ex.Message });
            }
        }

        // GET: api/admin/dashboard/top-books
        [HttpGet("top-books")]
        public async Task<ActionResult<IEnumerable<object>>> GetTopBooks([FromQuery] int limit = 10)
        {
            try
            {
                // Load order details with books first
                var orderDetails = await _context.OrderDetails
                    .Include(od => od.Book)
                        .ThenInclude(b => b.Author)
                    .ToListAsync();

                // Group by BookId on client side
                var topBooks = orderDetails
                    .Where(od => od.BookId != null && od.Book != null)
                    .GroupBy(od => od.BookId)
                    .Select(g => new
                    {
                        bookId = g.Key,
                        book = g.First().Book != null ? new
                        {
                            bookId = g.First().Book.BookId,
                            title = g.First().Book.Title,
                            price = g.First().Book.Price,
                            imageUrl = g.First().Book.ImageUrl,
                            author = g.First().Book.Author != null ? new
                            {
                                authorName = g.First().Book.Author.AuthorName
                            } : null
                        } : null,
                        totalSold = g.Sum(od => od.Quantity)
                    })
                    .OrderByDescending(x => x.totalSold)
                    .Take(limit)
                    .ToList();

                return Ok(topBooks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error getting top books: " + ex.Message });
            }
        }

        // GET: api/admin/dashboard/top-users
        [HttpGet("top-users")]
        public async Task<ActionResult<IEnumerable<object>>> GetTopUsers([FromQuery] int limit = 10)
        {
            try
            {
                // Load orders with users first
                var orders = await _context.Orders
                    .Include(o => o.User)
                    .ToListAsync();

                // Group by UserId on client side
                var topUsers = orders
                    .Where(o => o.UserId != null)
                    .GroupBy(o => o.UserId)
                    .Select(g => new
                    {
                        userId = g.Key,
                        user = g.First().User != null ? new
                        {
                            userId = g.First().User.UserId,
                            fullName = g.First().User.FullName,
                            email = g.First().User.Email
                        } : null,
                        totalOrders = g.Count(),
                        totalSpent = g.Sum(o => o.TotalPrice)
                    })
                    .OrderByDescending(x => x.totalSpent)
                    .Take(limit)
                    .ToList();

                return Ok(topUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error getting top users: " + ex.Message });
            }
        }
    }
}


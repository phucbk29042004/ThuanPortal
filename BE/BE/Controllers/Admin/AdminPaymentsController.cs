using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Models.Data;
using BE.Models.Entities;

namespace BE.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class AdminPaymentsController : ControllerBase
    {
        private readonly AppBookStoreContext _context;

        public AdminPaymentsController(AppBookStoreContext context)
        {
            _context = context;
        }

        // GET: api/admin/payments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPayments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? paymentMethod = null)
        {
            var query = _context.Payments
                .Include(p => p.Order)
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.PaymentStatus != null && p.PaymentStatus.ToLower() == status.ToLower());
            }

            // Filter by payment method
            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                query = query.Where(p => p.PaymentMethod != null && p.PaymentMethod.ToLower() == paymentMethod.ToLower());
            }

            var totalCount = await query.CountAsync();
            var payments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.PaymentId,
                    p.OrderId,
                    p.UserId,
                    p.Amount,
                    p.PaymentMethod,
                    p.TransactionId,
                    p.PaymentStatus,
                    p.CreatedAt,
                    p.UpdatedAt,
                    Order = p.Order != null ? new
                    {
                        p.Order.OrderId,
                        p.Order.TotalPrice,
                        p.Order.Status
                    } : null,
                    User = p.User != null ? new
                    {
                        p.User.UserId,
                        p.User.FullName,
                        p.User.Email
                    } : null
                })
                .ToListAsync();

            return Ok(new
            {
                data = payments,
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        // GET: api/admin/payments/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPayment(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o!.OrderDetails!)
                        .ThenInclude(od => od.Book)
                .Include(p => p.User)
                .Where(p => p.PaymentId == id)
                .Select(p => new
                {
                    p.PaymentId,
                    p.OrderId,
                    p.UserId,
                    p.Amount,
                    p.PaymentMethod,
                    p.TransactionId,
                    p.PaymentStatus,
                    p.CreatedAt,
                    p.UpdatedAt,
                    Order = p.Order != null ? new
                    {
                        p.Order.OrderId,
                        p.Order.TotalPrice,
                        p.Order.Status,
                        p.Order.CreatedAt,
                        OrderDetails = p.Order.OrderDetails!.Select(od => new
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
                    } : null,
                    User = p.User != null ? new
                    {
                        p.User.UserId,
                        p.User.FullName,
                        p.User.Email,
                        p.User.Phone
                    } : null
                })
                .FirstOrDefaultAsync();

            if (payment == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            return Ok(payment);
        }

        // PUT: api/admin/payments/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdatePayment(int id, [FromBody] UpdatePaymentRequest request)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            // Update payment status
            if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
            {
                payment.PaymentStatus = request.PaymentStatus;
                payment.UpdatedAt = DateTime.Now;

                // Update order status based on payment status
                if (payment.Order != null)
                {
                    if (request.PaymentStatus.ToLower() == "success" || request.PaymentStatus.ToLower() == "completed")
                    {
                        payment.Order.Status = "confirmed";
                    }
                    else if (request.PaymentStatus.ToLower() == "failed" || request.PaymentStatus.ToLower() == "cancelled")
                    {
                        payment.Order.Status = "cancelled";
                    }
                }
            }

            // Update transaction ID if provided
            if (!string.IsNullOrWhiteSpace(request.TransactionId))
            {
                payment.TransactionId = request.TransactionId;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Payment updated successfully",
                paymentId = payment.PaymentId,
                paymentStatus = payment.PaymentStatus,
                orderStatus = payment.Order?.Status
            });
        }
    }

    public class UpdatePaymentRequest
    {
        public string? PaymentStatus { get; set; }
        public string? TransactionId { get; set; }
    }
}


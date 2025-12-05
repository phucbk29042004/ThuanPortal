using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Models.Data;
using BE.Models.Entities;
using System;
using System.Linq;

namespace BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly AppBookStoreContext _context;

        public OrdersController(AppBookStoreContext context)
        {
            _context = context;
        }

        // POST: api/orders/checkout - Tạo đơn hàng VÀ thanh toán cùng lúc
        [HttpPost("checkout")]
        public async Task<ActionResult<object>> Checkout([FromBody] CheckoutRequest request)
        {
            // Bắt đầu Transaction để đảm bảo tất cả các thay đổi được thực hiện hoặc không có gì cả
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Validate user
                if (request.UserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "User authentication required" });
                }

                // 2. Get user's cart với đầy đủ thông tin
                var cart = await _context.Carts
                    .Include(c => c.CartItems!)
                        .ThenInclude(ci => ci.Book)
                    .FirstOrDefaultAsync(c => c.UserId == request.UserId);

                if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
                {
                    return BadRequest(new { success = false, message = "Giỏ hàng trống" });
                }

                // 3. Validate stock và calculate total
                decimal totalPrice = 0;
                var orderDetails = new List<OrderDetail>();

                foreach (var cartItem in cart.CartItems)
                {
                    if (cartItem.Book == null) continue;

                    var bookPrice = cartItem.Book.Price ?? 0;
                    var quantity = cartItem.Quantity ?? 0;

                    // Check stock
                    if (cartItem.Book.Quantity < quantity)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Sách '{cartItem.Book.Title}' không đủ số lượng trong kho. Còn lại: {cartItem.Book.Quantity}"
                        });
                    }

                    // *** LƯU Ý: Nếu có áp dụng khuyến mãi, logic tính giá itemTotal cần được thêm vào đây
                    var itemTotal = bookPrice * quantity;
                    totalPrice += itemTotal;

                    orderDetails.Add(new OrderDetail
                    {
                        BookId = cartItem.BookId,
                        Quantity = quantity,
                        Price = bookPrice // Lưu giá tại thời điểm đặt hàng
                    });
                }

                // 4. Validate payment method
                var validPaymentMethods = new[] { "COD", "Banking" }; // Chỉ còn COD và Banking
                if (!validPaymentMethods.Contains(request.PaymentMethod, StringComparer.OrdinalIgnoreCase))
                {
                    return BadRequest(new { success = false, message = "Phương thức thanh toán không hợp lệ. Chỉ chấp nhận COD hoặc Banking." });
                }

                // 5. Create Order
                var order = new Order
                {
                    UserId = request.UserId,
                    TotalPrice = totalPrice,
                    Status = "pending", // Sẽ update sau tùy payment method
                    CreatedAt = DateTime.Now,
                    OrderDetails = orderDetails
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Save để có OrderId

                // 6. Create Payment record
                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    UserId = request.UserId,
                    Amount = totalPrice,
                    PaymentMethod = request.PaymentMethod,
                    PaymentStatus = "Pending",
                    CreatedAt = DateTime.Now
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // 7. Xử lý theo từng payment method
                string? qrCodeUrl = null;
                bool shouldReduceStock = false;
                bool shouldClearCart = false;

                switch (request.PaymentMethod.ToUpper())
                {
                    case "COD":
                        // COD: Trừ kho ngay lập tức, xác nhận đơn hàng ngay
                        order.Status = "confirmed";
                        payment.PaymentStatus = "Pending"; // Sẽ thanh toán khi nhận hàng
                        shouldReduceStock = true;
                        shouldClearCart = true;
                        break;

                    case "BANKING":
                        // Banking: Đơn hàng chờ thanh toán, trừ kho sau khi Admin/Người dùng xác nhận thanh toán thành công
                        order.Status = "awaiting_payment";
                        payment.PaymentStatus = "Pending";
                        qrCodeUrl = GenerateBankingQrCodeUrl(totalPrice, order.OrderId); // Tạo mã QR
                        shouldClearCart = true; // Xóa giỏ hàng để tránh đặt trùng
                        break;
                }

                // 8. Reduce stock if needed (chỉ COD)
                if (shouldReduceStock)
                {
                    foreach (var cartItem in cart.CartItems)
                    {
                        if (cartItem.Book != null)
                        {
                            // Trừ trực tiếp vào quantity trong entity Book
                            cartItem.Book.Quantity -= cartItem.Quantity ?? 0;
                        }
                    }
                }

                // 9. Clear cart if needed
                if (shouldClearCart)
                {
                    _context.CartItems.RemoveRange(cart.CartItems);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 10. Return response
                return Ok(new
                {
                    success = true,
                    message = "Đặt hàng thành công",
                    data = new
                    {
                        orderId = order.OrderId,
                        paymentId = payment.PaymentId,
                        totalPrice = totalPrice,
                        status = order.Status,
                        paymentMethod = payment.PaymentMethod,
                        paymentStatus = payment.PaymentStatus,
                        qrCodeUrl = qrCodeUrl, // Mã QR cho Banking, null cho COD
                        createdAt = order.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi tạo đơn hàng",
                    error = ex.Message
                });
            }
        }

        // POST: api/orders/confirm-payment - Xác nhận thanh toán (Dùng cho Banking thủ công)
        [HttpPost("confirm-payment")]
        public async Task<ActionResult<object>> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
        {
            // Logic này KHÔNG thay đổi. Nó được sử dụng để xác nhận thanh toán
            // (thủ công bởi Admin đối với Banking hoặc tự động từ Webhook nếu dùng VNPay/MoMo)
            // Giữ lại để Admin có thể xác nhận chuyển khoản Ngân hàng
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Get payment info
                var payment = await _context.Payments
                    .Include(p => p.Order!)
                        .ThenInclude(o => o.OrderDetails!)
                            .ThenInclude(od => od.Book)
                    .FirstOrDefaultAsync(p => p.PaymentId == request.PaymentId);

                if (payment == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy thông tin thanh toán" });
                }

                if (payment.PaymentStatus == "Completed")
                {
                    return BadRequest(new { success = false, message = "Thanh toán đã được xác nhận trước đó" });
                }

                // 2. Update payment status
                payment.PaymentStatus = request.IsSuccess ? "Completed" : "Failed";
                payment.TransactionId = request.TransactionId;
                payment.UpdatedAt = DateTime.Now;

                if (request.IsSuccess)
                {
                    // 3. Update order status
                    if (payment.Order != null)
                    {
                        payment.Order.Status = "confirmed";

                        // 4. Reduce stock (chỉ trừ khi trạng thái đơn hàng là awaiting_payment, tức là Banking)
                        if (payment.Order.OrderDetails != null)
                        {
                            foreach (var detail in payment.Order.OrderDetails)
                            {
                                if (detail.Book != null)
                                {
                                    detail.Book.Quantity -= detail.Quantity ?? 0;

                                    if (detail.Book.Quantity < 0)
                                    {
                                        await transaction.RollbackAsync();
                                        return BadRequest(new
                                        {
                                            success = false,
                                            message = $"Không đủ số lượng sách '{detail.Book.Title}'"
                                        });
                                    }
                                }
                            }
                        }

                        // 5. Clear cart - Giỏ hàng đã được xóa ở bước Checkout (shouldClearCart = true cho Banking)
                        // Bỏ qua bước này nếu đã xóa ở checkout để tránh lỗi.
                    }
                }
                else
                {
                    // Payment failed -> Hủy đơn hàng
                    if (payment.Order != null)
                    {
                        payment.Order.Status = "cancelled";
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = request.IsSuccess ? "Xác nhận thanh toán thành công" : "Xác nhận thanh toán thất bại",
                    data = new
                    {
                        orderId = payment.OrderId,
                        paymentId = payment.PaymentId,
                        paymentStatus = payment.PaymentStatus,
                        orderStatus = payment.Order?.Status
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi xác nhận thanh toán",
                    error = ex.Message
                });
            }
        }

        // Các API GetOrder, GetUserOrders, UpdateOrderStatus, CancelOrder (giữ nguyên)
        // ... (phần code còn lại)

        // Helper methods
        private string GenerateBankingQrCodeUrl(decimal amount, int orderId)
        {
            // TODO: Thay thế bằng logic tạo mã QR thực tế (ví dụ: tạo link VietQR, hoặc link đến ảnh QR code tĩnh)
            // Cấu trúc chung thường là: [BASE_URL_CỦA_MÃ_QR]?amount=[AMOUNT]&content=[NỘI_DUNG_CHUYỂN_KHOẢN]
            // Nội dung chuyển khoản nên bao gồm mã đơn hàng (orderId) để dễ đối soát.

            string bankAccountInfo = "Chủ TK: Nguyen Van A - STK: 123456789 - Ngân hàng ABC";
            string transferContent = $"BOOK{orderId}";

            // Ví dụ trả về một URL giả định để hiển thị mã QR
            return $"https://fake-qr-generator.com/generate?amount={amount}&orderId={orderId}&content={transferContent}&bank={bankAccountInfo}";
        }

        // Bỏ các hàm GenerateVNPayUrl và GenerateMoMoUrl
        // private string GenerateVNPayUrl(int orderId, decimal amount) { ... }
        // private string GenerateMoMoUrl(int orderId, decimal amount) { ... }

        // GET: api/orders/{orderId}
        [HttpGet("{orderId}")]
        public async Task<ActionResult<object>> GetOrder(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Book!)
                        .ThenInclude(b => b.Author)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Book!)
                        .ThenInclude(b => b.Publisher)
                .Include(o => o.Payments)
                .Where(o => o.OrderId == orderId)
                .Select(o => new
                {
                    o.OrderId,
                    o.UserId,
                    User = o.User != null ? new
                    {
                        o.User.UserId,
                        o.User.FullName,
                        o.User.Email,
                        o.User.Phone
                    } : null,
                    o.TotalPrice,
                    o.Status,
                    o.CreatedAt,
                    OrderDetails = o.OrderDetails!.Select(od => new
                    {
                        od.OrderDetailId,
                        od.BookId,
                        od.Quantity,
                        od.Price,
                        Subtotal = od.Quantity * od.Price,
                        Book = od.Book != null ? new
                        {
                            od.Book.BookId,
                            od.Book.Title,
                            od.Book.ImageUrl,
                            od.Book.Description,
                            Author = od.Book.Author != null ? od.Book.Author.AuthorName : null,
                            Publisher = od.Book.Publisher != null ? od.Book.Publisher.PublisherName : null
                        } : null
                    }),
                    Payments = o.Payments!.Select(p => new
                    {
                        p.PaymentId,
                        p.Amount,
                        p.PaymentMethod,
                        p.PaymentStatus,
                        p.TransactionId,
                        p.CreatedAt,
                        p.UpdatedAt
                    })
                })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            return Ok(new { success = true, data = order });
        }

        // GET: api/orders/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<object>> GetUserOrders(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Book)
                .Include(o => o.Payments)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.OrderId,
                    o.TotalPrice,
                    o.Status,
                    o.CreatedAt,
                    ItemCount = o.OrderDetails!.Count,
                    TotalItems = o.OrderDetails!.Sum(od => od.Quantity ?? 0),
                    Payment = o.Payments!.OrderByDescending(p => p.CreatedAt).Select(p => new
                    {
                        p.PaymentMethod,
                        p.PaymentStatus
                    }).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(new { success = true, data = orders });
        }

        // PUT: api/orders/{orderId}/status
        [HttpPut("{orderId}/status")]
        public async Task<ActionResult<object>> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            var validStatuses = new[] { "pending", "awaiting_payment", "confirmed", "shipping", "delivered", "cancelled", "refunded" };
            if (!validStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Trạng thái không hợp lệ" });
            }

            order.Status = request.Status;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Cập nhật trạng thái thành công",
                data = new { orderId = order.OrderId, status = order.Status }
            });
        }

        // DELETE: api/orders/{orderId} (Cancel order)
        [HttpDelete("{orderId}")]
        public async Task<ActionResult<object>> CancelOrder(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Payments)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Book)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            // Chỉ có thể hủy nếu đang pending hoặc awaiting_payment
            if (order.Status != "pending" && order.Status != "awaiting_payment")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Không thể hủy đơn hàng đã được xác nhận hoặc đang giao"
                });
            }

            // Hoàn lại kho nếu đã trừ (COD)
            if (order.Status == "confirmed" && order.OrderDetails != null)
            {
                foreach (var detail in order.OrderDetails)
                {
                    if (detail.Book != null)
                    {
                        detail.Book.Quantity += detail.Quantity ?? 0;
                    }
                }
            }

            order.Status = "cancelled";

            // Update payment status
            if (order.Payments != null)
            {
                foreach (var payment in order.Payments)
                {
                    payment.PaymentStatus = "Cancelled";
                    payment.UpdatedAt = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Hủy đơn hàng thành công",
                data = new { orderId = order.OrderId, status = order.Status }
            });
        }
    }

    // Request DTOs (giữ nguyên)
    public class CheckoutRequest
    {
        public int UserId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class ConfirmPaymentRequest
    {
        public int PaymentId { get; set; }
        public bool IsSuccess { get; set; }
        public string? TransactionId { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
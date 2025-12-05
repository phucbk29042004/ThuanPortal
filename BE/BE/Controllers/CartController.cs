using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Models.Data;
using BE.Models.Entities;

namespace BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly AppBookStoreContext _context;

        public CartController(AppBookStoreContext context)
        {
            _context = context;
        }

        // GET: api/cart
        [HttpGet]
        public async Task<ActionResult<object>> GetCart([FromQuery] int userId)
        {
            // TODO: Implement authentication to get userId from token
            if (userId <= 0)
            {
                return Unauthorized(new { message = "User authentication required" });
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems!)
                    .ThenInclude(ci => ci.Book!)
                        .ThenInclude(b => b.Author)
                .Include(c => c.CartItems!)
                    .ThenInclude(ci => ci.Book!)
                        .ThenInclude(b => b.Publisher)
                .Include(c => c.CartItems!)
                    .ThenInclude(ci => ci.Book!)
                        .ThenInclude(b => b.Category)
                .Where(c => c.UserId == userId)
                .Select(c => new
                {
                    c.CartId,
                    c.UserId,
                    c.CreatedAt,
                    CartItems = c.CartItems!.Select(ci => new
                    {
                        ci.Id,
                        ci.BookId,
                        ci.Quantity,
                        Book = ci.Book != null ? new
                        {
                            ci.Book.BookId,
                            ci.Book.Title,
                            ci.Book.Price,
                            ci.Book.ImageUrl,
                            Author = ci.Book.Author != null ? new { ci.Book.Author.AuthorName } : null
                        } : null
                    })
                })
                .FirstOrDefaultAsync();

            if (cart == null)
            {
                return NotFound(new { message = "Cart not found" });
            }

            return Ok(cart);
        }

        // POST: api/cart/add
        [HttpPost("add")]
        public async Task<ActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            // TODO: Implement authentication to get userId from token
            if (request.UserId <= 0)
            {
                return Unauthorized(new { message = "User authentication required" });
            }

            var book = await _context.Books.FindAsync(request.BookId);
            if (book == null)
            {
                return NotFound(new { message = "Book not found" });
            }

            if (book.Quantity < request.Quantity)
            {
                return BadRequest(new { message = "Insufficient stock" });
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == request.UserId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = request.UserId,
                    CreatedAt = DateTime.Now
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var existingCartItem = cart.CartItems?.FirstOrDefault(ci => ci.BookId == request.BookId);
            if (existingCartItem != null)
            {
                existingCartItem.Quantity = (existingCartItem.Quantity ?? 0) + request.Quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    BookId = request.BookId,
                    Quantity = request.Quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Item added to cart successfully" });
        }

        // PUT: api/cart/update
        [HttpPut("update")]
        public async Task<ActionResult> UpdateCartItem([FromBody] UpdateCartItemRequest request)
        {
            var cartItem = await _context.CartItems.FindAsync(request.CartItemId);
            if (cartItem == null)
            {
                return NotFound(new { message = "Cart item not found" });
            }

            if (request.Quantity <= 0)
            {
                return BadRequest(new { message = "Quantity must be greater than 0" });
            }

            var book = await _context.Books.FindAsync(cartItem.BookId);
            if (book != null && book.Quantity < request.Quantity)
            {
                return BadRequest(new { message = "Insufficient stock" });
            }

            cartItem.Quantity = request.Quantity;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cart item updated successfully" });
        }

        // DELETE: api/cart/remove/{cart_item_id}
        [HttpDelete("remove/{cartItemId}")]
        public async Task<ActionResult> RemoveFromCart(int cartItemId)
        {
            var cartItem = await _context.CartItems.FindAsync(cartItemId);
            if (cartItem == null)
            {
                return NotFound(new { message = "Cart item not found" });
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Item removed from cart successfully" });
        }
    }

    public class AddToCartRequest
    {
        public int UserId { get; set; }
        public int BookId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateCartItemRequest
    {
        public int CartItemId { get; set; }
        public int Quantity { get; set; }
    }
}


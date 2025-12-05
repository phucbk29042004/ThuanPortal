using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Models.Data;
using BE.Models.Entities;

namespace BE.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/promotion-items")]
    public class AdminPromotionItemsController : ControllerBase
    {
        private readonly AppBookStoreContext _context;

        public AdminPromotionItemsController(AppBookStoreContext context)
        {
            _context = context;
        }

        // POST: api/admin/promotion-items
        [HttpPost]
        public async Task<ActionResult<object>> AddBookToPromotion([FromBody] CreatePromotionItemRequest request)
        {
            var promotion = await _context.Promotions.FindAsync(request.PromotionId);
            if (promotion == null)
            {
                return NotFound(new { message = "Promotion not found" });
            }

            var book = await _context.Books.FindAsync(request.BookId);
            if (book == null)
            {
                return NotFound(new { message = "Book not found" });
            }

            // Check if already exists
            var existing = await _context.PromotionItems
                .FirstOrDefaultAsync(pi => pi.PromotionId == request.PromotionId && pi.BookId == request.BookId);

            if (existing != null)
            {
                return Conflict(new { message = "Book already in this promotion" });
            }

            var promotionItem = new PromotionItem
            {
                PromotionId = request.PromotionId,
                BookId = request.BookId,
                SpecificDiscount = request.SpecificDiscount
            };

            _context.PromotionItems.Add(promotionItem);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Book added to promotion successfully",
                promoItemId = promotionItem.PromoItemId
            });
        }

        // DELETE: api/admin/promotion-items/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> RemoveBookFromPromotion(int id)
        {
            var promotionItem = await _context.PromotionItems.FindAsync(id);
            if (promotionItem == null)
            {
                return NotFound(new { message = "Promotion item not found" });
            }

            _context.PromotionItems.Remove(promotionItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Book removed from promotion successfully" });
        }
    }

    public class CreatePromotionItemRequest
    {
        public int PromotionId { get; set; }
        public int BookId { get; set; }
        public decimal? SpecificDiscount { get; set; }
    }
}


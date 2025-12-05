using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Models.Data;
using BE.Models.Entities;

namespace BE.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/promotions")]
    public class AdminPromotionsController : ControllerBase
    {
        private readonly AppBookStoreContext _context;

        public AdminPromotionsController(AppBookStoreContext context)
        {
            _context = context;
        }

        // GET: api/admin/promotions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPromotions()
        {
            var promotions = await _context.Promotions
                .Include(p => p.PromotionItems!)
                    .ThenInclude(pi => pi.Book)
                .Select(p => new
                {
                    p.PromotionId,
                    p.PromotionName,
                    p.PromotionType,
                    p.DiscountValue,
                    p.StartDate,
                    p.EndDate,
                    p.IsActive,
                    PromotionItems = p.PromotionItems!.Select(pi => new
                    {
                        pi.PromoItemId,
                        pi.BookId,
                        pi.SpecificDiscount,
                        Book = pi.Book != null ? new
                        {
                            pi.Book.Title
                        } : null
                    })
                })
                .ToListAsync();

            return Ok(promotions);
        }

        // GET: api/admin/promotions/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPromotion(int id)
        {
            var promotion = await _context.Promotions
                .Include(p => p.PromotionItems!)
                    .ThenInclude(pi => pi.Book)
                .Where(p => p.PromotionId == id)
                .Select(p => new
                {
                    p.PromotionId,
                    p.PromotionName,
                    p.PromotionType,
                    p.DiscountValue,
                    p.StartDate,
                    p.EndDate,
                    p.IsActive,
                    PromotionItems = p.PromotionItems!.Select(pi => new
                    {
                        pi.PromoItemId,
                        pi.BookId,
                        pi.SpecificDiscount,
                        Book = pi.Book != null ? new
                        {
                            pi.Book.Title
                        } : null
                    })
                })
                .FirstOrDefaultAsync();

            if (promotion == null)
            {
                return NotFound(new { message = "Promotion not found" });
            }

            return Ok(promotion);
        }

        // POST: api/admin/promotions
        [HttpPost]
        public async Task<ActionResult<object>> CreatePromotion([FromBody] CreatePromotionRequest request)
        {
            var promotion = new Promotion
            {
                PromotionName = request.PromotionName,
                PromotionType = request.PromotionType,
                DiscountValue = request.DiscountValue,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsActive = request.IsActive
            };

            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPromotion), new { id = promotion.PromotionId }, new
            {
                message = "Promotion created successfully",
                promotionId = promotion.PromotionId
            });
        }

        // PUT: api/admin/promotions/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdatePromotion(int id, [FromBody] UpdatePromotionRequest request)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return NotFound(new { message = "Promotion not found" });
            }

            promotion.PromotionName = request.PromotionName ?? promotion.PromotionName;
            promotion.PromotionType = request.PromotionType ?? promotion.PromotionType;
            promotion.DiscountValue = request.DiscountValue ?? promotion.DiscountValue;
            promotion.StartDate = request.StartDate ?? promotion.StartDate;
            promotion.EndDate = request.EndDate ?? promotion.EndDate;
            promotion.IsActive = request.IsActive ?? promotion.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Promotion updated successfully" });
        }

        // DELETE: api/admin/promotions/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePromotion(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return NotFound(new { message = "Promotion not found" });
            }

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Promotion deleted successfully" });
        }
    }

    public class CreatePromotionRequest
    {
        public string PromotionName { get; set; } = string.Empty;
        public string PromotionType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdatePromotionRequest
    {
        public string? PromotionName { get; set; }
        public string? PromotionType { get; set; }
        public decimal? DiscountValue { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsActive { get; set; }
    }
}


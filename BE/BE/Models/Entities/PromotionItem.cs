using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Models.Entities
{
    [Table("PromotionItems")]
    public class PromotionItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("promo_item_id")]
        public int PromoItemId { get; set; }

        [Required]
        [ForeignKey("Promotion")]
        [Column("promotion_id")]
        public int PromotionId { get; set; }

        [Required]
        [ForeignKey("Book")]
        [Column("book_id")]
        public int BookId { get; set; }

        [Column("specific_discount", TypeName = "decimal(10, 2)")]
        public decimal? SpecificDiscount { get; set; }

        // Navigation Properties
        public virtual Promotion? Promotion { get; set; }
        public virtual Book? Book { get; set; }
    }
}

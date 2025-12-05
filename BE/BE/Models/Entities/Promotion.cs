using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Models.Entities
{
    [Table("Promotions")]
    public class Promotion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("promotion_id")]
        public int PromotionId { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("promotion_name")]
        public string PromotionName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("promotion_type")]
        public string PromotionType { get; set; } = string.Empty;

        [Required]
        [Column("discount_value", TypeName = "decimal(10, 2)")]
        public decimal DiscountValue { get; set; }

        [Required]
        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Required]
        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Required]
        [Column("is_active")]
        public bool IsActive { get; set; }

        // Navigation Properties
        public virtual ICollection<PromotionItem>? PromotionItems { get; set; }
    }
}

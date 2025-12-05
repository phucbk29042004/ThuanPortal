using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Models.Entities
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("payment_id")]
        public int PaymentId { get; set; }

        [Required]
        [ForeignKey("Order")]
        [Column("order_id")]
        public int OrderId { get; set; }

        [Required]
        [ForeignKey("User")]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("amount", TypeName = "decimal(10, 2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("payment_method")]
        public string PaymentMethod { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("transaction_id")]
        public string? TransactionId { get; set; }

        [Required]
        [MaxLength(30)]
        [Column("payment_status")]
        public string PaymentStatus { get; set; } = "Pending";

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Order? Order { get; set; }
        public virtual User? User { get; set; }
    }
}


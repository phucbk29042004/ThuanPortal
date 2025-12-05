using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Models.Entities
{
    [Table("OrderDetails")]
    public class OrderDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("order_detail_id")]
        public int OrderDetailId { get; set; }

        [ForeignKey("Order")]
        [Column("order_id")]
        public int? OrderId { get; set; }

        [ForeignKey("Book")]
        [Column("book_id")]
        public int? BookId { get; set; }

        [Column("quantity")]
        public int? Quantity { get; set; }

        [Column("price", TypeName = "decimal(10, 2)")]
        public decimal? Price { get; set; }

        // Navigation Properties
        public virtual Order? Order { get; set; }
        public virtual Book? Book { get; set; }
    }
}

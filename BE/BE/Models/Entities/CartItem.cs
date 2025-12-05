using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Models.Entities
{
    [Table("CartItems")]
    public class CartItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [ForeignKey("Cart")]
        [Column("cart_id")]
        public int? CartId { get; set; }

        [ForeignKey("Book")]
        [Column("book_id")]
        public int? BookId { get; set; }

        [Column("quantity")]
        public int? Quantity { get; set; }

        // Navigation Properties
        public virtual Cart? Cart { get; set; }
        public virtual Book? Book { get; set; }
    }
}

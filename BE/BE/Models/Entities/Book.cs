using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Models.Entities
{
    [Table("Books")]
    public class Book
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("book_id")]
        public int BookId { get; set; }

        [MaxLength(255)]
        [Column("title")]
        public string? Title { get; set; }

        [Column("price", TypeName = "decimal(10, 2)")]
        public decimal? Price { get; set; }

        [Column("quantity")]
        public int? Quantity { get; set; }

        [ForeignKey("Category")]
        [Column("category_id")]
        public int? CategoryId { get; set; }

        [ForeignKey("Author")]
        [Column("author_id")]
        public int? AuthorId { get; set; }

        [ForeignKey("Publisher")]
        [Column("publisher_id")]
        public int? PublisherId { get; set; }

        [Column("description", TypeName = "nvarchar(max)")]
        public string? Description { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [MaxLength(500)]
        [Column("image_url")]
        public string? ImageUrl { get; set; }

        // Navigation properties
        public virtual Category? Category { get; set; }
        public virtual Author? Author { get; set; }
        public virtual Publisher? Publisher { get; set; }
        public virtual ICollection<CartItem>? CartItems { get; set; }
        public virtual ICollection<OrderDetail>? OrderDetails { get; set; }
        public virtual ICollection<PromotionItem>? PromotionItems { get; set; }
    }
}


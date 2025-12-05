using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Models.Entities
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("category_id")]
        public int CategoryId { get; set; }

        [MaxLength(255)]
        [Column("category_name")]
        public string? CategoryName { get; set; }

        // Navigation Properties
        public virtual ICollection<Book>? Books { get; set; }
    }
}

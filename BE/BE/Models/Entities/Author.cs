using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Models.Entities
{
    [Table("Authors")]
    public class Author
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("author_id")]
        public int AuthorId { get; set; }

        [MaxLength(255)]
        [Column("author_name")]
        public string? AuthorName { get; set; }

        // Navigation properties
        public virtual ICollection<Book>? Books { get; set; }
    }
}


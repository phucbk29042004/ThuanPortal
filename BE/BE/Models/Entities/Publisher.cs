using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Models.Entities
{
    [Table("Publishers")]
    public class Publisher
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("publisher_id")]
        public int PublisherId { get; set; }

        [MaxLength(255)]
        [Column("publisher_name")]
        public string? PublisherName { get; set; }

        // Navigation properties
        public virtual ICollection<Book>? Books { get; set; }
    }
}


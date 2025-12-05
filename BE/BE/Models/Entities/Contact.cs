using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Models.Entities
{
    [Table("Contact")]
    public class Contact
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("contact_id")]
        public int ContactId { get; set; }

        [MaxLength(255)]
        [Column("name")]
        public string? Name { get; set; }

        [MaxLength(255)]
        [Column("email")]
        public string? Email { get; set; }

        [Column("message", TypeName = "nvarchar(max)")]
        public string? Message { get; set; }

        [MaxLength(20)]
        [Column("status")]
        public string? Status { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}


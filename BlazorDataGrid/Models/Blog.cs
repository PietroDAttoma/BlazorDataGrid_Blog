using System.ComponentModel.DataAnnotations;

namespace BlazorDataGrid.Models
{
    public class Blog
    {
        // 🔑 Chiave primaria
        [Key]
        public int BlogId { get; set; }

        // 📛 Proprietà principali
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // 📛 Proprietà secondarie
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        // 🗑 Soft Delete
        public bool IsDeleted { get; set; } = false;

        // 🔄 Concorrenza ottimistica
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}

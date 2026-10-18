using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicStore.Models
{
    public class Cart
    {
        [Key] public int RecordId { get; set; }
        [Required, StringLength(64), Index("UX_Cart_Album", 1, IsUnique = true)]
        public string CartId { get; set; }
        [Index("UX_Cart_Album", 2, IsUnique = true)] public int AlbumId { get; set; }
        [Range(1, 99)] public int Count { get; set; }
        public DateTime DateCreated { get; set; }
        [Timestamp] public byte[] RowVersion { get; set; }
        public virtual Album Album { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace MusicStore.Models
{
    public class OrderDetail
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public int AlbumId { get; set; }
        [Range(1, 99)] public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        [Required, StringLength(160)] public string AlbumTitle { get; set; }
        [Required, StringLength(160)] public string ArtistName { get; set; }
        public virtual Album Album { get; set; }
        public virtual Order Order { get; set; }
    }
}

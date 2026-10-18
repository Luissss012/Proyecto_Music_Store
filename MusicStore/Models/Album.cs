using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MusicStore.Models
{
    public class Album
    {
        public int AlbumId { get; set; }

        [DisplayName("Género")]
        public int GenreId { get; set; }

        [DisplayName("Artista")]
        public int ArtistId { get; set; }

        [Required(ErrorMessage = "El título del álbum es obligatorio")]
        [StringLength(160)]
        public string Title { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, 100.00, ErrorMessage = "El precio debe estar entre 0.01 y 100.00")]
        public decimal Price { get; set; }

        [DisplayName("URL de portada")]
        [StringLength(1024)]
        public string AlbumArtUrl { get; set; }

        public Genre Genre { get; set; }

        public Artist Artist { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}

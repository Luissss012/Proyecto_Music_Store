using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicStore.Models
{
    public class Genre
    {
        public int GenreId { get; set; }

        [Required, StringLength(160), Index("UX_Genre_Name", IsUnique = true)]
        public string Name { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public List<Album> Albums { get; set; }
    }
}

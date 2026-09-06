using System.Collections.Generic;
using System.Data.Entity;

namespace MusicStore.Models
{
    public class SampleData : DropCreateDatabaseIfModelChanges<MusicStoreEntities>
    {
        protected override void Seed(MusicStoreEntities context)
        {
            var rock = new Genre
            {
                Name = "Rock",
                Description = "Rock music"
            };

            var jazz = new Genre
            {
                Name = "Jazz",
                Description = "Jazz music"
            };

            var artist1 = new Artist
            {
                Name = "Sample Artist"
            };

            var artist2 = new Artist
            {
                Name = "Jazz Artist"
            };

            context.Genres.Add(rock);
            context.Genres.Add(jazz);

            context.Artists.Add(artist1);
            context.Artists.Add(artist2);

            context.Albums.Add(new Album
            {
                Title = "Rock Album",
                Price = 9.99m,
                AlbumArtUrl = "/Content/Images/placeholder.gif",
                Genre = rock,
                Artist = artist1
            });

            context.Albums.Add(new Album
            {
                Title = "Jazz Album",
                Price = 11.99m,
                AlbumArtUrl = "/Content/Images/placeholder.gif",
                Genre = jazz,
                Artist = artist2
            });

            context.SaveChanges();
        }
    }
}
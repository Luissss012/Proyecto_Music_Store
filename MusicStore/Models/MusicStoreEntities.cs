using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace MusicStore.Models
{
    public class MusicStoreEntities : DbContext
    {
        public MusicStoreEntities()
            : base("name=MusicStoreEntities")
        {
        }

        public DbSet<Album> Albums { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Artist> Artists { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Album>().HasRequired(a => a.Artist).WithMany()
                .HasForeignKey(a => a.ArtistId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Album>().HasRequired(a => a.Genre).WithMany(g => g.Albums)
                .HasForeignKey(a => a.GenreId).WillCascadeOnDelete(false);
        }
    }
}

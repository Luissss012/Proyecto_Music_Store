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
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<UserCart> UserCarts { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Cart>().HasRequired(c => c.Album).WithMany()
                .HasForeignKey(c => c.AlbumId).WillCascadeOnDelete(false);
            modelBuilder.Entity<OrderDetail>().HasRequired(d => d.Album).WithMany()
                .HasForeignKey(d => d.AlbumId).WillCascadeOnDelete(false);
            modelBuilder.Entity<OrderDetail>().HasRequired(d => d.Order).WithMany(o => o.OrderDetails)
                .HasForeignKey(d => d.OrderId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Order>().Property(o => o.Total).HasPrecision(18, 2);
            modelBuilder.Entity<OrderDetail>().Property(d => d.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Album>().HasRequired(a => a.Artist).WithMany()
                .HasForeignKey(a => a.ArtistId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Album>().HasRequired(a => a.Genre).WithMany(g => g.Albums)
                .HasForeignKey(a => a.GenreId).WillCascadeOnDelete(false);
        }
    }
}

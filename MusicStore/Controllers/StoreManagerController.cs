using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using MusicStore.Models;

namespace MusicStore.Controllers
{
    public class StoreManagerController : AdminControllerBase
    {
        private readonly MusicStoreEntities db = new MusicStoreEntities();
        [HttpGet]
        public ActionResult Index() { return View(db.Albums.Include(a => a.Artist).Include(a => a.Genre).OrderBy(a => a.Title).ToList()); }
        private Album Find(int id) { return db.Albums.Include(a => a.Artist).Include(a => a.Genre).SingleOrDefault(a => a.AlbumId == id); }
        [HttpGet]
        public ActionResult Details(int? id)
        {
            if (!id.HasValue || id <= 0) return new HttpStatusCodeResult(400);
            var album = Find(id.Value); return album == null ? (ActionResult)HttpNotFound() : View(album);
        }
        private void Choices(Album album)
        {
            ViewBag.ArtistId = new SelectList(db.Artists.OrderBy(a => a.Name), "ArtistId", "Name", album.ArtistId);
            ViewBag.GenreId = new SelectList(db.Genres.OrderBy(g => g.Name), "GenreId", "Name", album.GenreId);
        }
        private void ValidateAlbum(Album album)
        {
            album.Title = (album.Title ?? "").Trim();
            if (string.IsNullOrWhiteSpace(album.Title)) ModelState.AddModelError("Title", "El título es obligatorio.");
            if (!db.Artists.Any(a => a.ArtistId == album.ArtistId)) ModelState.AddModelError("ArtistId", "Selecciona un artista existente.");
            if (!db.Genres.Any(g => g.GenreId == album.GenreId)) ModelState.AddModelError("GenreId", "Selecciona un género existente.");
            var image = (album.AlbumArtUrl ?? "").Trim();
            album.AlbumArtUrl = image;
            if (image != "")
            {
                Uri absolute;
                var local = (image.StartsWith("~/") || (image.StartsWith("/") && !image.StartsWith("//"))) && !image.Contains("\\") && !image.Any(char.IsControl);
                if (!local && !(Uri.TryCreate(image, UriKind.Absolute, out absolute) && (absolute.Scheme == "http" || absolute.Scheme == "https")))
                    ModelState.AddModelError("AlbumArtUrl", "Usa una URL http/https o una ruta local de imagen.");
            }
        }
        [HttpGet]
        public ActionResult Create() { var album = new Album(); Choices(album); return View(album); }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "GenreId,ArtistId,Title,Price,AlbumArtUrl")] Album album)
        {
            ValidateAlbum(album);
            if (ModelState.IsValid) { db.Albums.Add(album); db.SaveChanges(); return RedirectToAction("Index"); }
            Choices(album); return View(album);
        }
        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue || id <= 0) return new HttpStatusCodeResult(400);
            var album = db.Albums.Find(id.Value); if (album == null) return HttpNotFound();
            Choices(album); return View(album);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(int? id, [Bind(Include = "AlbumId,GenreId,ArtistId,Title,Price,AlbumArtUrl")] Album form, string version)
        {
            if (!id.HasValue || id <= 0 || form.AlbumId != id.Value || ReadVersion(version) == null) return new HttpStatusCodeResult(400);
            var album = db.Albums.Find(id.Value); if (album == null) return HttpNotFound();
            ValidateAlbum(form);
            if (!ModelState.IsValid) { form.RowVersion = ReadVersion(version); Choices(form); return View(form); }
            var rows = db.Database.ExecuteSqlCommand(@"UPDATE dbo.Albums SET Title=@title,Price=@price,ArtistId=@artist,GenreId=@genre,AlbumArtUrl=@image
                WHERE AlbumId=@id AND RowVersion=@version", new SqlParameter("@title", form.Title), new SqlParameter("@price", form.Price),
                new SqlParameter("@artist", form.ArtistId), new SqlParameter("@genre", form.GenreId), new SqlParameter("@image", form.AlbumArtUrl),
                new SqlParameter("@id", id.Value), new SqlParameter("@version", ReadVersion(version)));
            if (rows == 0) return StaleRecord();
            return RedirectToAction("Index");
        }
        [HttpGet]
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue || id <= 0) return new HttpStatusCodeResult(400);
            var album = Find(id.Value); return album == null ? (ActionResult)HttpNotFound() : View(album);
        }
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int? id, string version)
        {
            if (!id.HasValue || id <= 0 || ReadVersion(version) == null) return new HttpStatusCodeResult(400);
            var album = db.Albums.Find(id.Value); if (album == null) return HttpNotFound();
            var rows = db.Database.ExecuteSqlCommand("DELETE FROM dbo.Albums WHERE AlbumId=@id AND RowVersion=@version",
                new SqlParameter("@id", id.Value), new SqlParameter("@version", ReadVersion(version)));
            if (rows == 0) return StaleRecord();
            return RedirectToAction("Index");
        }
        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}

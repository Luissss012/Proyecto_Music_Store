using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using MusicStore.Models;

namespace MusicStore.Controllers
{
    // Only the two concrete controllers below are routable.
    public abstract class LookupsController : AdminControllerBase
    {
        private readonly MusicStoreEntities db = new MusicStoreEntities();
        protected abstract bool IsGenre { get; }
        private void Labels()
        {
            ViewBag.IsGenre = IsGenre;
            ViewBag.Label = IsGenre ? "Géneros" : "Artistas";
        }
        private LookupForm Find(int id)
        {
            if (IsGenre)
            {
                var item = db.Genres.Find(id);
                return item == null ? null : new LookupForm { Id = id, Name = item.Name, Description = item.Description, Version = Convert.ToBase64String(item.RowVersion), AlbumCount = db.Albums.Count(a => a.GenreId == id) };
            }
            var artist = db.Artists.Find(id);
            return artist == null ? null : new LookupForm { Id = id, Name = artist.Name, Version = Convert.ToBase64String(artist.RowVersion), AlbumCount = db.Albums.Count(a => a.ArtistId == id) };
        }

        [HttpGet]
        public ActionResult Index(string search = "", int page = 1)
        {
            if (page < 1 || page > 10000 || (search != null && search.Length > 160)) return new HttpStatusCodeResult(400);
            Labels();
            search = (search ?? "").Trim();
            IQueryable<LookupForm> items = IsGenre
                ? db.Genres.Where(g => search == "" || g.Name.Contains(search)).Select(g => new LookupForm { Id = g.GenreId, Name = g.Name, Description = g.Description, AlbumCount = g.Albums.Count })
                : db.Artists.Where(a => search == "" || a.Name.Contains(search)).Select(a => new LookupForm { Id = a.ArtistId, Name = a.Name, AlbumCount = db.Albums.Count(b => b.ArtistId == a.ArtistId) });
            var list = items.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * 25).Take(26).ToList();
            return View("~/Views/Lookups/Index.cshtml", new LookupList { Items = list.Take(25).ToList(), Page = page, HasNext = list.Count > 25, Search = search });
        }

        [HttpGet]
        public ActionResult Create() { Labels(); return View("~/Views/Lookups/Form.cshtml", new LookupForm()); }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Name,Description")] LookupForm form)
        {
            return Save(null, form);
        }

        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue || id <= 0) return new HttpStatusCodeResult(400);
            Labels(); var form = Find(id.Value);
            return form == null ? (ActionResult)HttpNotFound() : View("~/Views/Lookups/Form.cshtml", form);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(int? id, [Bind(Include = "Name,Description,Version")] LookupForm form)
        {
            if (!id.HasValue || id <= 0 || ReadVersion(form.Version) == null) return new HttpStatusCodeResult(400);
            return Save(id, form);
        }

        private ActionResult Save(int? id, LookupForm form)
        {
            Labels(); form.Id = id ?? 0;
            form.Name = Regex.Replace((form.Name ?? "").Trim(), @"\s+", " ");
            form.Description = IsGenre ? (form.Description ?? "").Trim() : null;
            if (string.IsNullOrWhiteSpace(form.Name)) ModelState.AddModelError("Name", "Escribe un nombre.");
            // SQL Server's collation applies the same equality rules as the unique index.
            if (IsGenre ? db.Genres.Any(g => g.Name == form.Name && g.GenreId != (id ?? 0)) : db.Artists.Any(a => a.Name == form.Name && a.ArtistId != (id ?? 0)))
                ModelState.AddModelError("Name", "Ese nombre ya existe.");
            if (!ModelState.IsValid) return View("~/Views/Lookups/Form.cshtml", form);
            if (id.HasValue)
            {
                // A stale version is an expected result, not an EF exception for the debugger.
                var rows = IsGenre
                    ? db.Database.ExecuteSqlCommand("UPDATE dbo.Genres SET Name=@name,Description=@description WHERE GenreId=@id AND RowVersion=@version",
                        new SqlParameter("@name", form.Name), new SqlParameter("@description", form.Description), new SqlParameter("@id", id.Value), new SqlParameter("@version", ReadVersion(form.Version)))
                    : db.Database.ExecuteSqlCommand("UPDATE dbo.Artists SET Name=@name WHERE ArtistId=@id AND RowVersion=@version",
                        new SqlParameter("@name", form.Name), new SqlParameter("@id", id.Value), new SqlParameter("@version", ReadVersion(form.Version)));
                if (rows == 0) return StaleRecord();
                TempData["AdminMessage"] = "Cambios guardados.";
                return RedirectToAction("Index");
            }
            if (IsGenre)
            {
                var item = new Genre();
                item.Name = form.Name; item.Description = form.Description;
                db.Genres.Add(item);
            }
            else
            {
                var item = new Artist();
                item.Name = form.Name;
                db.Artists.Add(item);
            }
            db.SaveChanges(); TempData["AdminMessage"] = "Cambios guardados.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue || id <= 0) return new HttpStatusCodeResult(400);
            Labels(); var form = Find(id.Value);
            return form == null ? (ActionResult)HttpNotFound() : View("~/Views/Lookups/Delete.cshtml", form);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int? id, string version)
        {
            if (!id.HasValue || id <= 0 || ReadVersion(version) == null) return new HttpStatusCodeResult(400);
            var form = Find(id.Value);
            if (form == null) return HttpNotFound();
            if (form.AlbumCount > 0)
            {
                Labels(); ModelState.AddModelError("", "No se puede eliminar: existen álbumes asociados. Puedes editar el nombre o reasignar primero esos álbumes.");
                return View("~/Views/Lookups/Delete.cshtml", form);
            }
            // NO ACTION foreign keys also reject an album inserted concurrently.
            var rows = db.Database.ExecuteSqlCommand(IsGenre
                ? "DELETE FROM dbo.Genres WHERE GenreId=@id AND RowVersion=@version"
                : "DELETE FROM dbo.Artists WHERE ArtistId=@id AND RowVersion=@version",
                new SqlParameter("@id", id.Value), new SqlParameter("@version", ReadVersion(version)));
            if (rows == 0) return StaleRecord();
            TempData["AdminMessage"] = "Registro eliminado."; return RedirectToAction("Index");
        }
        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
    public class ArtistsController : LookupsController { protected override bool IsGenre { get { return false; } } }
    public class GenresController : LookupsController { protected override bool IsGenre { get { return true; } } }
}

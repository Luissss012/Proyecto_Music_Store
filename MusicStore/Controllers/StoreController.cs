using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MusicStore.Models;

namespace MusicStore.Controllers
{
    public class StoreController : Controller
    {
        private MusicStoreEntities storeDB = new MusicStoreEntities();

        public ActionResult Index()
        {
            var genres = storeDB.Genres.OrderBy(g => g.Name).ToList();

            return View(genres);
        }

        public ActionResult Browse(string genre)
        {
            var genreModel = storeDB.Genres
                .Include(g => g.Albums.Select(a => a.Artist))
                .SingleOrDefault(g => g.Name == genre);

            if (genreModel == null) return HttpNotFound();

            return View(genreModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) storeDB.Dispose();
            base.Dispose(disposing);
        }

        public ActionResult Details(int? id)
        {
            if (!id.HasValue)
            {
                return RedirectToAction("Index");
            }

            var album = storeDB.Albums
                .Include(a => a.Genre)
                .Include(a => a.Artist)
                .SingleOrDefault(a => a.AlbumId == id.Value);

            if (album == null)
            {
                return HttpNotFound();
            }

            return View(album);
        }
    }
}

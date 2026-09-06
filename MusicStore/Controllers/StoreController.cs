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
            var genres = storeDB.Genres.ToList();

            return View(genres);
        }

        public ActionResult Browse(string genre)
        {
            var genreModel = storeDB.Genres
                .Include(g => g.Albums)
                .Single(g => g.Name == genre);

            return View(genreModel);
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
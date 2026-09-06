using System.Collections.Generic;
using System.Web.Mvc;
using MusicStore.Models;

namespace MusicStore.Controllers
{
    public class StoreController : Controller
    {
        public ActionResult Index()
        {
            var genres = new List<Genre>
            {
                new Genre { Name = "Rock" },
                new Genre { Name = "Jazz" },
                new Genre { Name = "Disco" }
            };

            return View(genres);
        }

        public ActionResult Browse(string genre)
        {
            var genreModel = new Genre
            {
                Name = genre,
                Albums = new List<Album>
        {
            new Album
            {
                Title = genre + " - Álbum 1",
                Genre = new Genre { Name = genre }
            },
            new Album
            {
                Title = genre + " - Álbum 2",
                Genre = new Genre { Name = genre }
            },
            new Album
            {
                Title = genre + " - Álbum 3",
                Genre = new Genre { Name = genre }
            }
        }
            };

            return View(genreModel);
        }

        public ActionResult Details(int? id, string genre)
        {
            if (!id.HasValue)
            {
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(genre))
            {
                genre = "Rock";
            }

            var album = new Album
            {
                Title = genre + " - Álbum " + id.Value,
                Genre = new Genre
                {
                    Name = genre
                }
            };

            return View(album);
        }
    }
}
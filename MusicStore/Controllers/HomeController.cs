using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using MusicStore.Models;

namespace MusicStore.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            using (var db = new MusicStoreEntities())
            {
                var sales = db.OrderDetails.GroupBy(d => d.AlbumId)
                    .Select(g => new { AlbumId = g.Key, Units = g.Sum(d => d.Quantity) });
                var topIds = (from album in db.Albums
                           join sale in sales on album.AlbumId equals sale.AlbumId
                           orderby sale.Units descending, album.AlbumId
                           select album.AlbumId).Take(6).ToList();
                var top = db.Albums.Include(a => a.Artist).Include(a => a.Genre).Where(a => topIds.Contains(a.AlbumId)).ToList()
                    .OrderBy(a => topIds.IndexOf(a.AlbumId)).ToList();
                ViewBag.HasSales = top.Count > 0;
                ViewBag.NewArrivals = db.Albums.Include(a => a.Artist).Include(a => a.Genre).OrderByDescending(a => a.AlbumId).Take(6).ToList();
                if (top.Count == 0) top = db.Albums.Include(a => a.Artist).Include(a => a.Genre).OrderByDescending(a => a.AlbumId).Take(6).ToList();
                return View(top);
            }
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}

using System;
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using MusicStore.Models;
using MusicStore.Services;

namespace MusicStore.Controllers
{
    public class NavigationController : Controller
    {
        [ChildActionOnly]
        public ActionResult Genres()
        {
            try
            {
                using (var db = new MusicStoreEntities())
                    return PartialView("~/Views/Shared/_GenreMenu.cshtml", db.Genres.AsNoTracking().OrderBy(g => g.Name).ToList());
            }
            catch (Exception) { return Content("Menú de géneros no disponible.", "text/plain"); }
        }
        [ChildActionOnly]
        public ActionResult CartSummary()
        {
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            try
            {
                var resolved = CartAccountService.Resolve(HttpContext);
                if (resolved.Success)
                    using (var cart = new ShoppingCartService(resolved.CartId))
                        return PartialView("~/Views/Shared/_CartSummary.cshtml", cart.GetCart());
            }
            catch (Exception) { /* The page remains usable when the summary cannot load. */ }
            return PartialView("~/Views/Shared/_CartSummary.cshtml", (object)null);
        }
    }
}

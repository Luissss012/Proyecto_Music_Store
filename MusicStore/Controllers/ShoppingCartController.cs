using System;
using System.Data.SqlClient;
using System.Web;
using System.Web.Mvc;
using MusicStore.Models;
using MusicStore.Services;

namespace MusicStore.Controllers
{
    [AllowAnonymous]
    public class ShoppingCartController : Controller
    {
        private ShoppingCartService service;
        private ShoppingCartService Cart
        {
            get
            {
                if (service == null)
                {
                    var id = Session[ShoppingCartService.SessionKey] as string;
                    if (id == null) { id = ShoppingCartService.NewIdentity(); Session[ShoppingCartService.SessionKey] = id; }
                    service = new ShoppingCartService(id);
                }
                return service;
            }
        }
        protected override void OnActionExecuting(ActionExecutingContext context)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            base.OnActionExecuting(context);
        }
        [HttpGet]
        public ActionResult Index()
        {
            var model = Cart.GetCart(); model.Message = TempData["CartNotice"] as string;
            return View(model);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AddToCart(int? albumId)
        {
            if (!ModelState.IsValid || !albumId.HasValue || albumId <= 0) return Show(new CartOperationResult { Status = 400, Message = "Selecciona un álbum válido." });
            return Show(Cart.Add(albumId.Value));
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int? id, int? quantity, string version)
        {
            if (!ModelState.IsValid || !id.HasValue || id <= 0 || !quantity.HasValue) return Show(new CartOperationResult { Status = 400, Message = "Datos inválidos. Usa una cantidad entera entre 1 y 99." });
            return Show(Cart.Update(id.Value, quantity.Value, version));
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult RemoveFromCart(int? id, string version)
        {
            if (!ModelState.IsValid || !id.HasValue || id <= 0) return Show(new CartOperationResult { Status = 400, Message = "Producto inválido." });
            return Show(Cart.Remove(id.Value, version));
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult EmptyCart(string revision) { return Show(Cart.Empty(revision)); }
        private ActionResult Show(CartOperationResult result)
        {
            if (!Request.IsAjaxRequest() && result.Status == 200)
            { TempData["CartNotice"] = result.Message; return RedirectToAction("Index"); }
            var model = Cart.GetCart(); model.Message = result.Message; model.HasError = result.Status >= 400;
            Response.StatusCode = result.Status; Response.TrySkipIisCustomErrors = true;
            ModelState.Clear();
            return Request.IsAjaxRequest() ? (ActionResult)PartialView("_Cart", model) : View("Index", model);
        }
        protected override void OnException(ExceptionContext context)
        {
            var status = 500;
            var message = "No se pudo completar la operación. Recarga el carrito e inténtalo nuevamente.";
            if (context.Exception is HttpAntiForgeryException || context.Exception is HttpRequestValidationException)
            { status = 400; message = "El formulario no es válido o expiró. Recarga la página antes de continuar."; }
            else
            {
                var sql = context.Exception.GetBaseException() as SqlException;
                if (sql != null && (sql.Number == 1205 || sql.Number == -2 || sql.Number == 547 || sql.Number == 2601 || sql.Number == 2627))
                { status = 409; message = "Los datos cambiaron mientras se procesaba la solicitud. Recarga el carrito."; }
            }
            context.ExceptionHandled = true;
            Response.StatusCode = status; Response.TrySkipIisCustomErrors = true;
            if (Request.IsAjaxRequest()) context.Result = Content(message, "text/plain");
            else context.Result = View("~/Views/ShoppingCart/Problem.cshtml", (object)message);
        }
        protected override void Dispose(bool disposing) { if (disposing && service != null) service.Dispose(); base.Dispose(disposing); }
    }
}

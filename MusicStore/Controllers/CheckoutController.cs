using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using MusicStore.Models;
using MusicStore.Services;

namespace MusicStore.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private string cartId;
        protected override void OnActionExecuting(ActionExecutingContext context)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            var resolved = CartAccountService.Resolve(HttpContext);
            if (!resolved.Success)
            {
                Response.StatusCode = 409; Response.TrySkipIisCustomErrors = true;
                context.Result = View("~/Views/ShoppingCart/Problem.cshtml", (object)resolved.Error);
                return;
            }
            cartId = resolved.CartId;
            base.OnActionExecuting(context);
        }
        [HttpGet]
        public ActionResult Index() { return RedirectToAction("AddressAndPayment"); }

        [HttpGet]
        public ActionResult AddressAndPayment()
        {
            using (var cart = new ShoppingCartService(cartId))
            {
                var snapshot = cart.GetCart();
                if (snapshot.Items.Count == 0) { TempData["CartNotice"] = "Agrega un álbum antes de continuar con la compra."; return RedirectToAction("Index", "ShoppingCart"); }
                return View(new CheckoutForm { Email = User.Identity.Name, Country = "Guatemala", Cart = snapshot,
                    Ticket = CheckoutService.CreateTicket(User.Identity.GetUserId(), cartId, snapshot.Revision) });
            }
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AddressAndPayment([Bind(Include = "FirstName,LastName,Address,City,State,PostalCode,Country,Phone,Email,PromoCode,Ticket")] CheckoutForm form)
        {
            Normalize(form);
            if (form.Phone != null && form.Phone.Count(char.IsDigit) < 7) ModelState.AddModelError("Phone", "El teléfono debe tener al menos siete dígitos.");
            if (!string.Equals(form.PromoCode, "FREE", StringComparison.OrdinalIgnoreCase)) ModelState.AddModelError("PromoCode", "Usa el código FREE para esta compra de demostración.");
            if (ModelState.IsValid)
            {
                var result = CheckoutService.Submit(User.Identity.GetUserId(), cartId, form);
                if (result.OrderId.HasValue) return RedirectToAction("Complete", new { id = result.OrderId.Value });
                Response.StatusCode = result.Status; Response.TrySkipIisCustomErrors = true;
                ModelState.AddModelError("", result.Message);
            }
            else { Response.StatusCode = 400; Response.TrySkipIisCustomErrors = true; }
            using (var cart = new ShoppingCartService(cartId)) form.Cart = cart.GetCart();
            form.Ticket = CheckoutService.CreateTicket(User.Identity.GetUserId(), cartId, form.Cart.Revision);
            ModelState.Remove("Ticket");
            return View(form);
        }
        private static void Normalize(CheckoutForm form)
        {
            form.FirstName = (form.FirstName ?? "").Trim(); form.LastName = (form.LastName ?? "").Trim();
            form.Address = (form.Address ?? "").Trim(); form.City = (form.City ?? "").Trim();
            form.State = (form.State ?? "").Trim(); form.PostalCode = (form.PostalCode ?? "").Trim();
            form.Country = (form.Country ?? "").Trim(); form.Phone = (form.Phone ?? "").Trim();
            form.Email = (form.Email ?? "").Trim(); form.PromoCode = (form.PromoCode ?? "").Trim();
        }
        [HttpGet]
        public ActionResult Complete(int? id)
        {
            if (!id.HasValue || id <= 0) return new HttpStatusCodeResult(400);
            var userId = User.Identity.GetUserId();
            using (var db = new MusicStoreEntities())
            {
                var order = db.Orders.AsNoTracking().Include(o => o.OrderDetails).SingleOrDefault(o => o.OrderId == id && o.UserId == userId);
                if (order == null) return HttpNotFound();
                return View(order);
            }
        }
        [HttpGet]
        public ActionResult Orders(int page = 1)
        {
            if (page < 1 || page > 10000) return new HttpStatusCodeResult(400);
            var userId = User.Identity.GetUserId();
            using (var db = new MusicStoreEntities())
            {
                var list = db.Orders.AsNoTracking().Where(o => o.UserId == userId).OrderByDescending(o => o.OrderId).Skip((page - 1) * 25).Take(26).ToList();
                return View(new OrderListViewModel { Orders = list.Take(25).ToList(), Page = page, HasNext = list.Count > 25 });
            }
        }
        protected override void OnException(ExceptionContext context)
        {
            int status = 500;
            string message = "No se pudo completar la compra. Revisa Mis pedidos antes de reintentar; el carrito solo se vacía si la orden se guarda correctamente.";
            if (context.Exception is HttpAntiForgeryException || context.Exception is HttpRequestValidationException)
            { status = 400; message = "El formulario no es válido o expiró. Vuelve al carrito y abre la compra nuevamente."; }
            else
            {
                var sql = context.Exception.GetBaseException() as SqlException;
                if (sql != null && (sql.Number == 1205 || sql.Number == -2 || sql.Number == 547 || sql.Number == 2601 || sql.Number == 2627))
                { status = 409; message = "Los datos cambiaron durante la compra. Revisa Mis pedidos y el carrito antes de volver a confirmar."; }
            }
            context.ExceptionHandled = true; Response.StatusCode = status; Response.TrySkipIisCustomErrors = true;
            context.Result = View("~/Views/Checkout/Problem.cshtml", (object)message);
        }
    }
}

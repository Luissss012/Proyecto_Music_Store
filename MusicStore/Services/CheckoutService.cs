using System;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Web.Security;
using MusicStore.Models;

namespace MusicStore.Services
{
    public static class CheckoutService
    {
        private const string Purpose = "MusicStore.Checkout.v1";
        public static string CreateTicket(string userId, string cartId, string revision)
        {
            var ticket = new CheckoutTicket { UserId = userId, CartId = cartId, Revision = revision,
                Nonce = ShoppingCartService.NewIdentity(), ExpiresUtcTicks = DateTime.UtcNow.AddMinutes(20).Ticks };
            var json = new JavaScriptSerializer().Serialize(ticket);
            return Convert.ToBase64String(MachineKey.Protect(Encoding.UTF8.GetBytes(json), Purpose));
        }
        private static CheckoutTicket ReadTicket(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 4096) return null;
            try
            {
                var bytes = MachineKey.Unprotect(Convert.FromBase64String(value), Purpose);
                if (bytes == null) return null;
                return new JavaScriptSerializer().Deserialize<CheckoutTicket>(Encoding.UTF8.GetString(bytes));
            }
            catch (FormatException) { return null; }
            catch (System.Security.Cryptography.CryptographicException) { return null; }
            catch (ArgumentException) { return null; }
            catch (InvalidOperationException) { return null; }
        }
        private static CheckoutResult Fail(int status, string message) { return new CheckoutResult { Status = status, Message = message }; }

        public static CheckoutResult Submit(string userId, string cartId, CheckoutForm form)
        {
            var ticket = ReadTicket(form.Ticket);
            if (ticket == null || ticket.UserId != userId || ticket.CartId != cartId || ticket.Nonce == null || ticket.Nonce.Length != 64)
                return Fail(400, "La confirmación no es válida. Revisa el carrito y vuelve a abrir la compra.");
            using (var db = new MusicStoreEntities())
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.RepeatableRead))
            {
                if (!ShoppingCartService.AcquireLock(db, cartId)) return Fail(409, "Hay otra operación en curso. Vuelve a intentarlo.");
                // Retrying a committed request returns its original order, even when the cart is now empty.
                var prior = db.Orders.AsNoTracking().Where(o => o.CheckoutToken == ticket.Nonce && o.UserId == userId).Select(o => (int?)o.OrderId).SingleOrDefault();
                if (prior.HasValue) return new CheckoutResult { Status = 200, OrderId = prior.Value };
                if (ticket.ExpiresUtcTicks < DateTime.UtcNow.Ticks) return Fail(400, "La confirmación expiró. Revisa los datos y confirma nuevamente.");
                var snapshot = ShoppingCartService.ReadCart(db, cartId);
                if (snapshot.Items.Count == 0) return Fail(409, "El carrito está vacío. Agrega un álbum antes de comprar.");
                if (!string.Equals(ticket.Revision, snapshot.Revision, StringComparison.Ordinal))
                    return Fail(409, "El carrito o sus precios cambiaron. Revisa el resumen actualizado y vuelve a confirmar.");
                var order = new Order
                {
                    UserId = userId, CheckoutToken = ticket.Nonce, OrderDate = DateTime.UtcNow,
                    FirstName = form.FirstName, LastName = form.LastName, Address = form.Address, City = form.City,
                    State = form.State, PostalCode = form.PostalCode, Country = form.Country, Phone = form.Phone,
                    Email = form.Email, Total = snapshot.Total,
                    OrderDetails = snapshot.Items.Select(i => new OrderDetail { AlbumId = i.AlbumId, Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice, AlbumTitle = i.Title, ArtistName = i.Artist }).ToList()
                };
                db.Orders.Add(order);
                db.SaveChanges();
                db.Database.ExecuteSqlCommand("DELETE dbo.Carts WHERE CartId=@cart", new SqlParameter("@cart", cartId));
                transaction.Commit();
                return new CheckoutResult { Status = 200, OrderId = order.OrderId };
            }
        }
    }
}

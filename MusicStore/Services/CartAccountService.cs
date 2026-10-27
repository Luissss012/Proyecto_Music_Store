using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;
using MusicStore.Models;

namespace MusicStore.Services
{
    public static class CartAccountService
    {
        public const string OwnerSessionKey = "MusicStore.CartUserId";

        public static CartResolution Resolve(HttpContextBase context)
        {
            var session = context.Session;
            var previousOwner = session[OwnerSessionKey] as string;
            var currentCart = session[ShoppingCartService.SessionKey] as string;
            if (context.User.Identity.IsAuthenticated)
            {
                var userId = context.User.Identity.GetUserId();
                if (previousOwner == userId && currentCart != null) return new CartResolution { CartId = currentCart };
                var resolution = Attach(userId, previousOwner == null ? currentCart : null);
                if (resolution.Success) Remember(context, userId, resolution.CartId);
                return resolution;
            }
            // Expired authentication must not turn a saved account cart into an anonymous cart.
            if (previousOwner != null) { currentCart = null; session.Remove(OwnerSessionKey); }
            if (currentCart == null) { currentCart = ShoppingCartService.NewIdentity(); session[ShoppingCartService.SessionKey] = currentCart; }
            return new CartResolution { CartId = currentCart };
        }
        public static void Remember(HttpContextBase context, string userId, string cartId)
        {
            context.Session[ShoppingCartService.SessionKey] = cartId;
            context.Session[OwnerSessionKey] = userId;
        }
        public static CartResolution SignIn(HttpContextBase context, string userId)
        {
            var anonymous = context.Session[OwnerSessionKey] == null
                ? context.Session[ShoppingCartService.SessionKey] as string : null;
            var result = Attach(userId, anonymous);
            if (result.Success) Remember(context, userId, result.CartId);
            return result;
        }
        private static CartResolution Fail(string error) { return new CartResolution { Error = error }; }

        public static CartResolution Attach(string userId, string anonymousId)
        {
            if (string.IsNullOrEmpty(userId) || userId.Length > 128) return Fail("La sesión no es válida. Vuelve a iniciar sesión.");
            using (var db = new MusicStoreEntities())
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var locked = db.Database.SqlQuery<int>(@"DECLARE @result int; EXEC @result=sys.sp_getapplock
                    @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=5000; SELECT @result;",
                    new SqlParameter("@resource", "MusicStore.UserCart." + userId)).Single();
                if (locked < 0) return Fail("El carrito se está actualizando. Inténtalo nuevamente.");
                var saved = db.UserCarts.Find(userId);
                if (saved == null) { saved = new UserCart { UserId = userId, CartId = ShoppingCartService.NewIdentity() }; db.UserCarts.Add(saved); }
                var cartIds = new[] { saved.CartId, anonymousId }.Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x, StringComparer.Ordinal);
                foreach (var cartId in cartIds)
                    if (!ShoppingCartService.AcquireLock(db, cartId)) return Fail("El carrito se está actualizando. Inténtalo nuevamente.");
                if (!string.IsNullOrEmpty(anonymousId) && anonymousId != saved.CartId)
                {
                    if (db.UserCarts.Any(c => c.CartId == anonymousId)) return Fail("No se puede combinar un carrito que pertenece a otra cuenta.");
                    var guest = db.Carts.Where(c => c.CartId == anonymousId).ToList();
                    var target = db.Carts.Where(c => c.CartId == saved.CartId).ToList();
                    if (guest.Select(c => c.AlbumId).Union(target.Select(c => c.AlbumId)).Count() > ShoppingCartService.MaxDistinctAlbums)
                        return Fail("Tus carritos juntos superan 50 álbumes diferentes. No se modificó ninguno. Revisa los productos actuales antes de volver a entrar; también puedes ajustar el guardado desde otra ventana privada.");
                    if (guest.Any(g => g.Count + target.Where(t => t.AlbumId == g.AlbumId).Sum(t => t.Count) > ShoppingCartService.MaxQuantity))
                        return Fail("Tus carritos juntos superan 99 unidades de algún álbum. No se modificó ninguno. Reduce las cantidades actuales antes de volver a entrar; también puedes ajustar el guardado desde otra ventana privada.");
                    foreach (var item in guest)
                    {
                        var existing = target.SingleOrDefault(t => t.AlbumId == item.AlbumId);
                        if (existing == null) item.CartId = saved.CartId;
                        else { existing.Count += item.Count; db.Carts.Remove(item); }
                    }
                }
                db.SaveChanges(); transaction.Commit();
                return new CartResolution { CartId = saved.CartId };
            }
        }
    }
}

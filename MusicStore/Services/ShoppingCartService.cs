using System;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MusicStore.Models;

namespace MusicStore.Services
{
    public class ShoppingCartService : IDisposable
    {
        public const string SessionKey = "MusicStore.CartId";
        public const int MaxQuantity = 99;
        public const int MaxDistinctAlbums = 50;
        private readonly MusicStoreEntities db = new MusicStoreEntities();
        private readonly string cartId;
        public ShoppingCartService(string id)
        {
            if (id == null || id.Length != 64 || !id.All(Uri.IsHexDigit)) throw new ArgumentException("Invalid cart identity.");
            cartId = id;
        }
        public static string NewIdentity()
        {
            var bytes = new byte[32];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
        private static CartOperationResult Result(int status, string message) { return new CartOperationResult { Status = status, Message = message }; }
        private bool Lock()
        {
            return db.Database.SqlQuery<int>(@"DECLARE @result int; EXEC @result=sys.sp_getapplock
                @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=5000;
                SELECT @result;", new SqlParameter("@resource", "MusicStore.Cart." + cartId)).Single() >= 0;
        }
        private static byte[] Version(string version)
        {
            if (string.IsNullOrEmpty(version) || version.Length > 24) return null;
            try { var bytes = Convert.FromBase64String(version); return bytes.Length == 8 ? bytes : null; }
            catch (FormatException) { return null; }
        }
        public ShoppingCartViewModel GetCart()
        {
            var lines = db.Carts.AsNoTracking().Where(c => c.CartId == cartId).OrderBy(c => c.RecordId)
                .Select(c => new { c.RecordId, c.AlbumId, c.Count, c.RowVersion, c.Album.Title, Artist = c.Album.Artist.Name, c.Album.Price }).ToList();
            var items = lines.Select(c => new CartLineViewModel { RecordId = c.RecordId, AlbumId = c.AlbumId, Quantity = c.Count,
                Title = c.Title, Artist = c.Artist, UnitPrice = c.Price, Version = Convert.ToBase64String(c.RowVersion) }).ToList();
            var fingerprint = cartId + "|" + string.Join("|", items.Select(c => c.RecordId + ":" + c.Version + ":" + c.UnitPrice.ToString(CultureInfo.InvariantCulture)));
            string revision;
            using (var hash = SHA256.Create()) revision = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(fingerprint))).Replace("-", "");
            return new ShoppingCartViewModel { Items = items, Count = items.Sum(c => c.Quantity), Total = items.Sum(c => c.Subtotal), Revision = revision };
        }
        public CartOperationResult Add(int albumId)
        {
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                if (!Lock()) return Result(409, "Hay otra operación en curso. Inténtalo nuevamente.");
                if (!db.Albums.Any(a => a.AlbumId == albumId)) return Result(404, "El álbum ya no está disponible.");
                var line = db.Carts.SingleOrDefault(c => c.CartId == cartId && c.AlbumId == albumId);
                if (line == null)
                {
                    if (db.Carts.Count(c => c.CartId == cartId) >= MaxDistinctAlbums) return Result(400, "Puedes agregar hasta 50 álbumes diferentes.");
                    db.Carts.Add(new Cart { CartId = cartId, AlbumId = albumId, Count = 1, DateCreated = DateTime.UtcNow });
                }
                else
                {
                    if (line.Count >= MaxQuantity) return Result(400, "Puedes agregar hasta 99 unidades de cada álbum.");
                    line.Count++;
                }
                db.SaveChanges(); transaction.Commit();
                return Result(200, "Álbum agregado al carrito.");
            }
        }
        public CartOperationResult Update(int recordId, int quantity, string version)
        {
            if (quantity < 1 || quantity > MaxQuantity || Version(version) == null) return Result(400, "La cantidad debe ser un número entero entre 1 y 99. Recarga si el formulario es antiguo.");
            return ChangeLine(recordId, quantity, version, false);
        }
        public CartOperationResult Remove(int recordId, string version)
        {
            if (Version(version) == null) return Result(400, "Formulario inválido. Recarga el carrito.");
            return ChangeLine(recordId, 0, version, true);
        }
        private CartOperationResult ChangeLine(int recordId, int quantity, string version, bool remove)
        {
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                if (!Lock()) return Result(409, "Hay otra operación en curso. Inténtalo nuevamente.");
                if (!db.Carts.Any(c => c.CartId == cartId && c.RecordId == recordId)) return Result(404, "El producto no está en tu carrito. La lista se actualizó.");
                var parameters = new[] { new SqlParameter("@cart", cartId), new SqlParameter("@id", recordId), new SqlParameter("@version", Version(version)), new SqlParameter("@quantity", quantity) };
                var rows = db.Database.ExecuteSqlCommand(remove
                    ? "DELETE dbo.Carts WHERE CartId=@cart AND RecordId=@id AND RowVersion=@version"
                    : "UPDATE dbo.Carts SET [Count]=@quantity WHERE CartId=@cart AND RecordId=@id AND RowVersion=@version", parameters);
                if (rows == 0) return Result(409, "El carrito cambió en otra pestaña. Se muestra la versión actual; revisa antes de volver a guardar.");
                transaction.Commit();
                return Result(200, remove ? "Producto eliminado." : "Cantidad actualizada.");
            }
        }
        public CartOperationResult Empty(string revision)
        {
            if (string.IsNullOrEmpty(revision) || revision.Length != 64) return Result(400, "Formulario inválido. Recarga el carrito.");
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                if (!Lock()) return Result(409, "Hay otra operación en curso. Inténtalo nuevamente.");
                if (!string.Equals(revision, GetCart().Revision, StringComparison.Ordinal)) return Result(409, "El carrito cambió. Revisa la lista actual antes de vaciarlo.");
                db.Database.ExecuteSqlCommand("DELETE dbo.Carts WHERE CartId=@cart", new SqlParameter("@cart", cartId));
                transaction.Commit(); return Result(200, "Carrito vaciado.");
            }
        }
        public void Dispose() { db.Dispose(); }
    }
}

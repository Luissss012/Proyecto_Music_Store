using System.Collections.Generic;

namespace MusicStore.Models
{
    public class CartLineViewModel
    {
        public int RecordId { get; set; }
        public int AlbumId { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get { return UnitPrice * Quantity; } }
        public string Version { get; set; }
    }
    public class ShoppingCartViewModel
    {
        public List<CartLineViewModel> Items { get; set; }
        public decimal Total { get; set; }
        public int Count { get; set; }
        public string Revision { get; set; }
        public string Message { get; set; }
        public bool HasError { get; set; }
    }
    public class CartOperationResult
    {
        public int Status { get; set; }
        public string Message { get; set; }
    }
}

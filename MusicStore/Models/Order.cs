using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MusicStore.Models
{
    // Sprint 6 prepares persistence. Checkout and ownership checks arrive in Sprint 7.
    public class Order
    {
        public int OrderId { get; set; }
        [Required, StringLength(128)] public string UserId { get; set; }
        [Required, StringLength(80)] public string FirstName { get; set; }
        [Required, StringLength(80)] public string LastName { get; set; }
        [Required, StringLength(200)] public string Address { get; set; }
        [Required, StringLength(80)] public string City { get; set; }
        [StringLength(80)] public string State { get; set; }
        [Required, StringLength(20)] public string PostalCode { get; set; }
        [Required, StringLength(80)] public string Country { get; set; }
        [Required, StringLength(30)] public string Phone { get; set; }
        [Required, EmailAddress, StringLength(254)] public string Email { get; set; }
        public decimal Total { get; set; }
        public DateTime OrderDate { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MusicStore.Models
{
    public class CheckoutForm
    {
        [Required(ErrorMessage = "Escribe tu nombre."), StringLength(80), Display(Name = "Nombre")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Escribe tu apellido."), StringLength(80), Display(Name = "Apellido")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "Escribe la dirección."), StringLength(200), Display(Name = "Dirección")]
        public string Address { get; set; }
        [Required(ErrorMessage = "Escribe la ciudad."), StringLength(80), Display(Name = "Ciudad")]
        public string City { get; set; }
        [StringLength(80), Display(Name = "Departamento o estado")]
        public string State { get; set; }
        [Required(ErrorMessage = "Escribe el código postal."), StringLength(20), RegularExpression(@"^[a-zA-Z0-9 \-]{3,20}$", ErrorMessage = "Revisa el código postal."), Display(Name = "Código postal")]
        public string PostalCode { get; set; }
        [Required(ErrorMessage = "Escribe el país."), StringLength(80), Display(Name = "País")]
        public string Country { get; set; }
        [Required(ErrorMessage = "Escribe el teléfono."), StringLength(30), RegularExpression(@"^[0-9+(). \-]{7,30}$", ErrorMessage = "Revisa el teléfono."), Display(Name = "Teléfono")]
        public string Phone { get; set; }
        [Required(ErrorMessage = "Escribe el correo electrónico."), EmailAddress(ErrorMessage = "Escribe un correo electrónico válido."), StringLength(254), Display(Name = "Correo electrónico")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Escribe el código FREE para esta demostración."), StringLength(20), Display(Name = "Código de demostración")]
        public string PromoCode { get; set; }
        [Required, StringLength(4096)] public string Ticket { get; set; }
        public ShoppingCartViewModel Cart { get; set; }
    }
    public class CheckoutTicket
    {
        public string UserId { get; set; }
        public string CartId { get; set; }
        public string Revision { get; set; }
        public string Nonce { get; set; }
        public long ExpiresUtcTicks { get; set; }
    }
    public class CheckoutResult
    {
        public int Status { get; set; }
        public string Message { get; set; }
        public int? OrderId { get; set; }
    }
    public class OrderListViewModel
    {
        public List<Order> Orders { get; set; }
        public int Page { get; set; }
        public bool HasNext { get; set; }
    }
    public class CartResolution
    {
        public string CartId { get; set; }
        public string Error { get; set; }
        public bool Success { get { return Error == null; } }
    }
}

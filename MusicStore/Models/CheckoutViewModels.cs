using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace MusicStore.Models
{
    public class CheckoutForm
    {
        [Required(ErrorMessage = "Escribe tu nombre."), StringLength(80), Display(Name = "Nombre")]
        [CheckoutPattern(@" *\p{L}[\p{L}\p{M}]*(?:[ .’'\-]+\p{L}[\p{L}\p{M}]*)*\.? *", ErrorMessage = "El nombre debe contener letras; puede incluir tildes, espacios, apóstrofes y guiones, pero no números.")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Escribe tu apellido."), StringLength(80), Display(Name = "Apellido")]
        [CheckoutPattern(@" *\p{L}[\p{L}\p{M}]*(?:[ .’'\-]+\p{L}[\p{L}\p{M}]*)*\.? *", ErrorMessage = "El apellido debe contener letras; puede incluir tildes, espacios, apóstrofes y guiones, pero no números.")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "Escribe la dirección."), StringLength(200), Display(Name = "Dirección")]
        [CheckoutPattern(@"(?=.{5,200}$)(?=.*\p{L})[^\p{Cc}<>]+", ErrorMessage = "Escribe una dirección de al menos cinco caracteres, con letras y sin caracteres de control.")]
        public string Address { get; set; }
        [Required(ErrorMessage = "Escribe la ciudad."), StringLength(80), Display(Name = "Ciudad")]
        [CheckoutPattern(@"(?=.*\p{L})[\p{L}\p{M}0-9 .’'\-]+", ErrorMessage = "Escribe una ciudad con letras y sin símbolos especiales.")]
        public string City { get; set; }
        [StringLength(80), Display(Name = "Departamento o estado")]
        [CheckoutPattern(@"(?=.*\p{L})[\p{L}\p{M}0-9 .’'\-]+", ErrorMessage = "Revisa el departamento o estado: debe contener letras y no símbolos especiales.")]
        public string State { get; set; }
        [Required(ErrorMessage = "Escribe el código postal."), StringLength(20), RegularExpression(@"^[a-zA-Z0-9 \-]{3,20}$", ErrorMessage = "Revisa el código postal."), Display(Name = "Código postal")]
        public string PostalCode { get; set; }
        [Required(ErrorMessage = "Escribe el país."), StringLength(80), Display(Name = "País")]
        [CheckoutPattern(@" *\p{L}[\p{L}\p{M} .’'\-]*", ErrorMessage = "Escribe el país con letras, sin números ni símbolos especiales.")]
        public string Country { get; set; }
        [Required(ErrorMessage = "Escribe el teléfono."), StringLength(30), Display(Name = "Teléfono")]
        [CheckoutPattern(@"(?=(?:[^0-9]*[0-9]){7,15}[^0-9]*$)\+?[0-9]+(?:[ .\-][0-9]+)*", ErrorMessage = "El teléfono debe contener de 7 a 15 dígitos; usa + solo al inicio y espacios, puntos o guiones entre grupos.")]
        public string Phone { get; set; }
        [Required(ErrorMessage = "Escribe el correo electrónico."), EmailAddress(ErrorMessage = "Escribe un correo electrónico válido."), StringLength(254), Display(Name = "Correo electrónico")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Escribe el código FREE para esta demostración."), StringLength(20), Display(Name = "Código de demostración")]
        public string PromoCode { get; set; }
        [Required, StringLength(4096)] public string Ticket { get; set; }
        public ShoppingCartViewModel Cart { get; set; }
    }
    // Unicode patterns run on the server even when JavaScript is disabled.
    public sealed class CheckoutPatternAttribute : RegularExpressionAttribute, IClientValidatable
    {
        public CheckoutPatternAttribute(string pattern) : base(pattern) { }
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context)
        {
            var rule = new ModelClientValidationRule { ValidationType = "checkoutpattern", ErrorMessage = FormatErrorMessage(metadata.GetDisplayName()) };
            rule.ValidationParameters["pattern"] = Pattern;
            yield return rule;
        }
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

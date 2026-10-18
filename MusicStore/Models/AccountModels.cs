using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace MusicStore.Models
{
    public class ApplicationUser : IdentityUser { }

    public class AccountsContext : IdentityDbContext<ApplicationUser>
    {
        public AccountsContext() : base("AccountsConnection") { }
        public static UserManager<ApplicationUser> CreateManager(AccountsContext context)
        {
            var manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));
            manager.UserValidator = new UserValidator<ApplicationUser>(manager) { RequireUniqueEmail = true, AllowOnlyAlphanumericUserNames = false };
            manager.PasswordValidator = new PasswordValidator { RequiredLength = 10, RequireDigit = true, RequireLowercase = true, RequireUppercase = true, RequireNonLetterOrDigit = true };
            manager.UserLockoutEnabledByDefault = true;
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;
            manager.DefaultAccountLockoutTimeSpan = System.TimeSpan.FromMinutes(15);
            return manager;
        }
    }

    public class LoginModel
    {
        [Required, EmailAddress, StringLength(254), Display(Name = "Correo electrónico")]
        public string Email { get; set; }
        [Required, StringLength(128), DataType(DataType.Password), Display(Name = "Contraseña")]
        public string Password { get; set; }
    }

    public class RegisterModel : LoginModel
    {
        [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Las contraseñas no coinciden."), Display(Name = "Confirmar contraseña")]
        public string ConfirmPassword { get; set; }
    }
}

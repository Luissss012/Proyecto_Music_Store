using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using MusicStore.Models;

namespace MusicStore.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly AccountsContext db = new AccountsContext();
        private UserManager<ApplicationUser> manager;
        private UserManager<ApplicationUser> Manager { get { return manager ?? (manager = AccountsContext.CreateManager(db)); } }

        [AllowAnonymous]
        public ActionResult Register() { return View(new RegisterModel()); }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email.Trim(), Email = model.Email.Trim() };
                using (var transaction = db.Database.BeginTransaction())
                {
                    var result = await Manager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        result = await Manager.AddToRoleAsync(user.Id, "Usuario");
                        if (result.Succeeded)
                        {
                            transaction.Commit();
                            TempData["AccountMessage"] = "Cuenta creada. Ahora puedes iniciar sesión.";
                            return RedirectToAction("Login");
                        }
                    }
                    foreach (var error in result.Errors) ModelState.AddModelError("", error);
                }
            }
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
            return View(new LoginModel());
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginModel model, string returnUrl)
        {
            ViewBag.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
            if (ModelState.IsValid)
            {
                var user = await Manager.FindByEmailAsync(model.Email.Trim());
                if (user != null && !await Manager.IsLockedOutAsync(user.Id))
                {
                    if (await Manager.CheckPasswordAsync(user, model.Password))
                    {
                        await Manager.ResetAccessFailedCountAsync(user.Id);
                        var identity = await Manager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
                        HttpContext.GetOwinContext().Authentication.SignIn(new AuthenticationProperties { IsPersistent = false }, identity);
                        if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                        return RedirectToAction("Index", "Home");
                    }
                    await Manager.AccessFailedAsync(user.Id);
                }
                ModelState.AddModelError("", "No se pudo iniciar sesión. Revisa tus datos o espera si la cuenta está bloqueada.");
            }
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            // A different account using this browser must not inherit the previous cart.
            Session.Remove(MusicStore.Services.ShoppingCartService.SessionKey);
            HttpContext.GetOwinContext().Authentication.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public ActionResult AccessDenied()
        {
            Response.StatusCode = 403;
            Response.TrySkipIisCustomErrors = true;
            return View();
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.Exception is System.Web.Mvc.HttpAntiForgeryException)
            {
                filterContext.ExceptionHandled = true;
                Response.TrySkipIisCustomErrors = true;
                filterContext.Result = new HttpStatusCodeResult(400, "Formulario no valido. Recarga la pagina e intenta de nuevo.");
            }
            else base.OnException(filterContext);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { if (manager != null) manager.Dispose(); db.Dispose(); }
            base.Dispose(disposing);
        }
    }
}

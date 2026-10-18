using System;
using System.Threading.Tasks;
using System.Web.Helpers;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using MusicStore.Models;
using Owin;

[assembly: OwinStartup(typeof(MusicStore.Startup))]
namespace MusicStore
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            AntiForgeryConfig.UniqueClaimTypeIdentifier = System.Security.Claims.ClaimTypes.NameIdentifier;
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                CookieName = "MusicStore.Auth",
                CookieHttpOnly = true,
                CookieSecure = CookieSecureOption.SameAsRequest,
                CookieSameSite = SameSiteMode.Lax,
                ExpireTimeSpan = TimeSpan.FromMinutes(30),
                SlidingExpiration = true,
                Provider = new CookieAuthenticationProvider
                {
                    OnValidateIdentity = async context =>
                    {
                        using (var db = new AccountsContext())
                        using (var manager = AccountsContext.CreateManager(db))
                        {
                            var user = await manager.FindByIdAsync(context.Identity.GetUserId());
                            var stamp = context.Identity.FindFirst("AspNet.Identity.SecurityStamp");
                            if (user == null || stamp == null || stamp.Value != user.SecurityStamp || await manager.IsLockedOutAsync(user.Id))
                            {
                                context.RejectIdentity();
                                context.OwinContext.Authentication.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                                return;
                            }
                            context.ReplaceIdentity(await manager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie));
                        }
                    },
                    OnApplyRedirect = context =>
                    {
                        if (context.OwinContext.Authentication.User.Identity.IsAuthenticated)
                            context.Response.Redirect("/Account/AccessDenied");
                        else context.Response.Redirect(context.RedirectUri);
                    }
                }
            });
        }
    }
}

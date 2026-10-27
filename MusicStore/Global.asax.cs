using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Data.Entity;
using MusicStore.Models;

namespace MusicStore
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Error()
        {
            var error = Server.GetLastError();
            var http = error as HttpException;
            var status = http == null ? 500 : http.GetHttpCode();
            if (status < 400 || status > 599) status = 500;
            Server.ClearError();
            WriteErrorResponse(status);
        }
        protected void Application_EndRequest()
        {
            if (Response.StatusCode == 404 && !string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)) WriteErrorResponse(404);
        }
        private void WriteErrorResponse(int status)
        {
            Response.Clear();
            Response.StatusCode = status;
            Response.TrySkipIisCustomErrors = true;
            Response.ContentType = "text/html; charset=utf-8";
            Response.Cache.SetNoStore();
            var title = status == 404 ? "No encontramos esta página" : status == 400 ? "Revisa la solicitud" : "No pudimos completar la solicitud";
            Response.Write("<!doctype html><html lang=\"es\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>" + title + " - Music Store</title><body style=\"font:18px Arial;background:#f5f2eb;color:#22251f;padding:8vw;max-width:800px\"><p>MUSIC STORE / " + status + "</p><h1>" + title + "</h1><p>Vuelve a la tienda para continuar. Si estabas comprando, revisa Mis pedidos antes de reintentar.</p><a href=\"" + VirtualPathUtility.ToAbsolute("~/Store") + "\">Volver a la colección</a></body></html>");
        }
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            // Fingerprinted bundle URLs refresh styles and scripts after each release.
            BundleTable.EnableOptimizations = true;

            // Apply schema changes explicitly through reviewed EF migrations.
            Database.SetInitializer<MusicStoreEntities>(null);
        }
    }
}

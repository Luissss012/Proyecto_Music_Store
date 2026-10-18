using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Web;
using System.Web.Mvc;

namespace MusicStore.Controllers
{
    [Authorize(Roles = "Administrator")]
    public abstract class AdminControllerBase : Controller
    {
        protected ActionResult StaleRecord()
        {
            Response.StatusCode = 409;
            Response.TrySkipIisCustomErrors = true;
            return View("~/Views/Admin/Problem.cshtml", (object)"Otro usuario modificó o eliminó este registro. Tus cambios no se guardaron. Recarga la lista antes de continuar.");
        }

        protected override void OnException(ExceptionContext context)
        {
            var code = 0;
            var message = "";
            if (context.Exception is HttpAntiForgeryException || context.Exception is HttpRequestValidationException)
            { code = 400; message = "Formulario inválido. Recarga la página y vuelve a intentarlo."; }
            else if (context.Exception is DbUpdateConcurrencyException)
            { code = 409; message = "Otro usuario modificó o eliminó este registro. Recarga la lista antes de continuar."; }
            else
            {
                var sql = context.Exception.GetBaseException() as SqlException;
                if (sql != null && (sql.Number == 2601 || sql.Number == 2627))
                { code = 409; message = "Ese nombre ya existe. Vuelve a la lista y revisa los datos."; }
                else if (sql != null && sql.Number == 547)
                { code = 409; message = "No se puede completar la operación porque hay datos relacionados. Recarga la lista."; }
                else if (sql != null && (sql.Number == 1205 || sql.Number == -2))
                { code = 409; message = "Otra operación está en curso. Recarga la página e inténtalo nuevamente."; }
            }
            if (code == 0)
            {
                System.Diagnostics.Trace.TraceError("Administrative operation failed: {0}", context.Exception.GetType().FullName);
                code = 500; message = "No se pudo completar la operación. Vuelve al panel e inténtalo nuevamente.";
            }
            context.ExceptionHandled = true;
            Response.TrySkipIisCustomErrors = true;
            Response.StatusCode = code;
            context.Result = View("~/Views/Admin/Problem.cshtml", (object)message);
        }

        protected static byte[] ReadVersion(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 24) return null;
            try { var bytes = Convert.FromBase64String(value); return bytes.Length == 8 ? bytes : null; }
            catch (FormatException) { return null; }
        }
    }
}

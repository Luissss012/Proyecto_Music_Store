using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.AspNet.Identity;
using MusicStore.Models;

namespace MusicStore.Services
{
    public class RoleAdministration
    {
        // Database-scoped lock serializes all web role changes, including separate IIS workers.
        public static string Change(string actorId, RoleChangeForm form)
        {
            using (var db = new AccountsContext())
            using (var manager = AccountsContext.CreateManager(db))
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var locked = db.Database.SqlQuery<int>(@"DECLARE @result int;
                    EXEC @result = sys.sp_getapplock @Resource=N'MusicStore.RoleAdministration',
                    @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000;
                    SELECT @result;").Single();
                if (locked < 0) return "Otra operación de permisos está en curso. Inténtalo nuevamente.";
                var actor = manager.FindById(actorId);
                if (actor == null || !manager.IsInRole(actorId, "Owner") || !manager.IsInRole(actorId, "Administrator") || manager.IsLockedOut(actorId))
                    return "Tu cuenta ya no puede administrar permisos. Vuelve a iniciar sesión.";
                if (!manager.CheckPassword(actor, form.Password))
                {
                    Check(manager.AccessFailed(actorId));
                    transaction.Commit();
                    return "La contraseña de confirmación es incorrecta. Tras cinco intentos se bloquea temporalmente tu cuenta.";
                }
                if (actorId == form.Id) return "No puedes cambiar tus propios permisos desde esta pantalla.";
                var target = manager.FindById(form.Id);
                if (target == null) return "La cuenta ya no existe.";
                if (manager.IsInRole(target.Id, "Owner")) return "La cuenta propietaria está protegida y no puede cambiarse desde la web.";
                if (target.SecurityStamp != form.Stamp) return "La cuenta cambió desde que abriste el formulario. Recarga la lista.";
                bool isAdmin = manager.IsInRole(target.Id, "Administrator");
                if (isAdmin == form.MakeAdministrator) return "Los permisos ya cambiaron. Recarga la lista.";
                var roleId = db.Roles.Where(r => r.Name == "Administrator").Select(r => r.Id).Single();
                if (!form.MakeAdministrator && db.Users.Count(u => u.Roles.Any(r => r.RoleId == roleId)) <= 1)
                    return "Debe conservarse al menos un administrador.";
                Check(form.MakeAdministrator ? manager.AddToRole(target.Id, "Administrator") : manager.RemoveFromRole(target.Id, "Administrator"));
                if (!manager.IsInRole(target.Id, "Usuario")) Check(manager.AddToRole(target.Id, "Usuario"));
                Check(manager.UpdateSecurityStamp(target.Id));
                Check(manager.ResetAccessFailedCount(actorId));
                db.Database.ExecuteSqlCommand(@"INSERT dbo.AdminRoleAudit(ActorId,TargetId,Granted) VALUES (@actor,@target,@granted)",
                    new SqlParameter("@actor", actorId), new SqlParameter("@target", target.Id), new SqlParameter("@granted", form.MakeAdministrator));
                transaction.Commit();
                return null;
            }
        }
        private static void Check(IdentityResult result)
        {
            if (!result.Succeeded) throw new InvalidOperationException("No se pudo guardar el cambio de permisos. No se aplicaron cambios parciales.");
        }
    }
}

using System;
using System.Data.Entity;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using MusicStore.Models;

// Local bootstrap/recovery tool. Web role administration requires an authenticated administrator.
public class AccountsAdmin
{
    public static int Main(string[] args)
    {
        try
        {
            if (!(args.Length == 1 && args[0] == "setup") && !(args.Length == 2 && (args[0] == "promote" || args[0] == "owner")))
                throw new ArgumentException("Uso: AccountsAdmin.exe setup | promote correo | owner correo");
            if (args[0] != "setup" && args[0] != "promote" && args[0] != "owner")
                throw new ArgumentException("Comando no permitido.");
            using (var db = new AccountsContext())
            {
                Database.SetInitializer(new CreateDatabaseIfNotExists<AccountsContext>());
                db.Database.Initialize(false);
                using (var transaction = db.Database.BeginTransaction())
                {
                var locked = db.Database.SqlQuery<int>(@"DECLARE @result int; EXEC @result=sys.sp_getapplock
                    @Resource=N'MusicStore.RoleAdministration',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=5000; SELECT @result;").Single();
                if (locked < 0) throw new InvalidOperationException("Otra operación de permisos está en curso.");
                using (var roles = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db)))
                {
                    foreach (var role in new[] { "Usuario", "Administrator", "Owner" })
                        if (!roles.RoleExists(role)) Check(roles.Create(new IdentityRole(role)));
                    if (args[0] == "promote" || args[0] == "owner")
                    {
                        using (var users = AccountsContext.CreateManager(db))
                        {
                            var user = users.FindByEmail(args[1].Trim());
                            if (user == null) throw new InvalidOperationException("Registra primero esta cuenta en la aplicación.");
                            if (args[0] == "owner")
                            {
                                var ownerId = db.Roles.Where(r => r.Name == "Owner").Select(r => r.Id).Single();
                                if (db.Users.Any(u => u.Id != user.Id && u.Roles.Any(r => r.RoleId == ownerId)))
                                    throw new InvalidOperationException("Ya existe un propietario. Esta herramienta no transfiere la propiedad.");
                                if (!users.IsInRole(user.Id, "Owner")) Check(users.AddToRole(user.Id, "Owner"));
                            }
                            if (!users.IsInRole(user.Id, "Administrator")) Check(users.AddToRole(user.Id, "Administrator"));
                            Check(users.UpdateSecurityStamp(user.Id));
                            Console.WriteLine(args[0] == "owner" ? "Propietario asignado. Cierra sesión y vuelve a entrar." : "Administrador asignado. Cierra sesión y vuelve a entrar.");
                        }
                    }
                    else Console.WriteLine("Base de cuentas preparada. Roles Usuario, Administrator y Owner disponibles.");
                }
                transaction.Commit();
                }
            }
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
    }
    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors));
    }
}

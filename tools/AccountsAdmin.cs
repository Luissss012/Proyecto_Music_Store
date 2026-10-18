using System;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using MusicStore.Models;

// Local-only administration tool. No HTTP endpoint can grant administrator access.
public class AccountsAdmin
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length != 1 && !(args.Length == 2 && args[0] == "promote"))
                throw new ArgumentException("Uso: AccountsAdmin.exe setup | promote correo");
            if (args[0] != "setup" && args[0] != "promote")
                throw new ArgumentException("Comando no permitido.");
            using (var db = new AccountsContext())
            {
                Database.SetInitializer(new CreateDatabaseIfNotExists<AccountsContext>());
                db.Database.Initialize(false);
                using (var roles = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db)))
                {
                    foreach (var role in new[] { "Usuario", "Administrator" })
                        if (!roles.RoleExists(role)) Check(roles.Create(new IdentityRole(role)));
                    if (args[0] == "promote")
                    {
                        using (var users = AccountsContext.CreateManager(db))
                        {
                            var user = users.FindByEmail(args[1].Trim());
                            if (user == null) throw new InvalidOperationException("Registra primero esta cuenta en la aplicación.");
                            if (!users.IsInRole(user.Id, "Administrator")) Check(users.AddToRole(user.Id, "Administrator"));
                            Check(users.UpdateSecurityStamp(user.Id));
                            Console.WriteLine("Administrador asignado. Cierra sesión y vuelve a entrar.");
                        }
                    }
                    else Console.WriteLine("Base de cuentas preparada. Roles Usuario y Administrator disponibles.");
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

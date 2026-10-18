using System;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using MusicStore.Models;
using MusicStore.Services;

namespace MusicStore.Controllers
{
    [Authorize(Roles = "Owner")]
    public class UsersController : AdminControllerBase
    {
        private readonly AccountsContext db = new AccountsContext();
        [HttpGet]
        public ActionResult Index(string search = "", int page = 1)
        {
            if (page < 1 || page > 10000 || (search != null && search.Length > 254)) return new HttpStatusCodeResult(400);
            search = (search ?? "").Trim();
            var currentId = User.Identity.GetUserId();
            var adminId = db.Roles.Where(r => r.Name == "Administrator").Select(r => r.Id).Single();
            var ownerId = db.Roles.Where(r => r.Name == "Owner").Select(r => r.Id).FirstOrDefault();
            var items = db.Users.Where(u => search == "" || u.Email.Contains(search)).OrderBy(u => u.Email).ThenBy(u => u.Id)
                .Skip((page - 1) * 25).Take(26).Select(u => new UserRow { Id = u.Id, Email = u.Email,
                    IsAdministrator = u.Roles.Any(r => r.RoleId == adminId), IsOwner = u.Roles.Any(r => r.RoleId == ownerId), IsCurrentUser = u.Id == currentId }).ToList();
            return View(new UserList { Items = items.Take(25).ToList(), Page = page, HasNext = items.Count > 25, Search = search });
        }
        [HttpGet]
        public ActionResult ChangeRole(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 128) return new HttpStatusCodeResult(400);
            if (id == User.Identity.GetUserId()) return new HttpStatusCodeResult(400, "No puedes cambiar tus propios permisos.");
            var user = db.Users.Find(id);
            if (user == null) return HttpNotFound();
            var ownerId = db.Roles.Where(r => r.Name == "Owner").Select(r => r.Id).FirstOrDefault();
            if (user.Roles.Any(r => r.RoleId == ownerId)) return new HttpStatusCodeResult(403);
            var adminId = db.Roles.Where(r => r.Name == "Administrator").Select(r => r.Id).Single();
            ViewBag.Email = user.Email;
            return View(new RoleChangeForm { Id = id, Stamp = user.SecurityStamp, MakeAdministrator = !user.Roles.Any(r => r.RoleId == adminId) });
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ChangeRole([Bind(Include = "Id,Stamp,MakeAdministrator,Password")] RoleChangeForm form)
        {
            if (!ModelState.IsValid) { TempData["AdminError"] = "Formulario incompleto o inválido. Abre nuevamente la confirmación."; return RedirectToAction("Index"); }
            var error = RoleAdministration.Change(User.Identity.GetUserId(), form);
            // Password is never placed in TempData, logs, or a rendered form.
            TempData[error == null ? "AdminMessage" : "AdminError"] = error ?? "Permisos actualizados. La cuenta deberá volver a iniciar sesión.";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public ActionResult Audit()
        {
            return View(db.Database.SqlQuery<RoleAuditRow>(@"SELECT TOP (100) a.Id, a.CreatedUtc, a.Granted,
                COALESCE(actor.Email,a.ActorId) AS Actor, COALESCE(target.Email,a.TargetId) AS Target
                FROM dbo.AdminRoleAudit a LEFT JOIN dbo.AspNetUsers actor ON actor.Id=a.ActorId
                LEFT JOIN dbo.AspNetUsers target ON target.Id=a.TargetId ORDER BY a.Id DESC").ToList());
        }
        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
    public class RoleAuditRow
    {
        public long Id { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string Actor { get; set; }
        public string Target { get; set; }
        public bool Granted { get; set; }
    }
}

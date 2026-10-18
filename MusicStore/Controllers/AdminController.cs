using System.Web.Mvc;
namespace MusicStore.Controllers
{
    public class AdminController : AdminControllerBase
    {
        [HttpGet]
        public ActionResult Index() { return View(); }
    }
}

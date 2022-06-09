using System.Net.Http;
using System.Web.Mvc;

namespace Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}

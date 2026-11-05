using System.Web.Mvc;

namespace HARTPOS.Controllers
{
    /// <summary>
    /// Referral Orders is not used in this POS. The pages are switched off and the
    /// permission has been removed (see Service/SchemaUpdates.cs).
    /// </summary>
    [Authorize]
    public class ReferralOrdersController : Controller
    {
        public ActionResult OrderList() => HttpNotFound();

        [HttpPost]
        public ActionResult GetPagedOrders() => HttpNotFound();
    }
}

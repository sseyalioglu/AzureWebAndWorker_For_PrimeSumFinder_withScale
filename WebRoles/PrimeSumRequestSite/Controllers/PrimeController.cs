using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DemoEntities;
using Microsoft.ServiceBus.Messaging;

namespace PrimeSumRequestSite.Controllers
{
    public class PrimeController : Controller
    {
        //
        // GET: /Prime/

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(PrimeSumRequest theRequest)
        {
            if (ModelState.IsValid)
            {
                var sumRequest = new BrokeredMessage(theRequest);
                SbQueueConnector.PrimeSumQueueClient.Send(sumRequest);
            }

            return View();
        }

    }
}

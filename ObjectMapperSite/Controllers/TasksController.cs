using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ObjectMapper;

namespace ObjectMapperSite.Controllers
{
    using ObjectMapperSite.Models;

    public class TasksController : ConnectionController
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            var cmd = Connection.CreateMappedSelectCommand<Task>();
            var rdr = cmd.ExecuteReader();

            return this.View(rdr.MapToEnumerable<Task>());
        }

    }
}

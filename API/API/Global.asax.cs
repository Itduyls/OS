using API.Models;
using Helper;
using Newtonsoft.Json;
using System;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace CMS
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
           
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            //
            try
            {
                string json = System.IO.File.ReadAllText(HttpContext.Current.Server.MapPath("~/Config/Config.json"));
                var cog = JsonConvert.DeserializeObject<settings>(json);
                if (cog != null)
                {
                    helper.debug = cog.debug;
                    helper.wlog = cog.wlog;
                    helper.logCongtent = cog.logCongtent;
                    helper.milisec = cog.milisec;
                    helper.timeout = cog.timeout;
                }
            }
            catch { }
        }
        protected void Application_EndRequest(Object sender, EventArgs e)
        {
            HttpContext context = HttpContext.Current;
            if (context.Response.Status.Substring(0, 3).Equals("401"))
            {
                context.Response.ClearContent();
                context.Response.Write("<script language=\"javascript\">" +
                             "self.location='../Error/401.html';</script>");
            }
        }
    }
}

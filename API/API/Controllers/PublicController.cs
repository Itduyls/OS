using API.Models;
using Helper;
using Microsoft.ApplicationBlocks.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace API.Controllers
{
    public class PublicController : ApiController
    {
        private readonly string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;

        [HttpPost]
        public async Task<HttpResponseMessage> ListMenu()
        {
            try
            {
              
                
                DateTime sdate = DateTime.Now;
                var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, "SQL_Menu_ListPublic").Tables);
                var tables = await task;
                DateTime edate = DateTime.Now;
               
                string JSONresult = JsonConvert.SerializeObject(tables);
                return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, err = "0" });
            }
            catch (DbEntityValidationException e)
            {
                string contents = helper.getCatchError(e, null);
                if (!helper.debug)
                {
                    contents = helper.logCongtent;
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                if (!helper.debug)
                {
                    contents = helper.logCongtent;
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }

        }

        [HttpPost]
        public async Task<HttpResponseMessage> ListSlideShowImage(cms_slideshow slideShowIn)
        {
            try
            {
       
                string JSONresult = "";
                DateTime sdate = DateTime.Now;
                using (DBEntities db = new DBEntities())
                {
                    var slideShow = db.cms_slideshow.AsNoTracking().Where(s => s.slideshow_id == slideShowIn.slideshow_id && s.is_type == slideShowIn.is_type).FirstOrDefault<cms_slideshow>();
                    if (slideShow != null)
                    {
                        var sqlpas = new SqlParameter("@SlideShow_ID", slideShow.slideshow_id);
                        var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, CommandType.Text, "SELECT is_filepath,des from cms_slideshow_image where slideshow_id=@SlideShow_ID ", sqlpas).Tables);

                        var tables = await task;
                        DateTime edate = DateTime.Now;

                        JSONresult = JsonConvert.SerializeObject(tables);
                    }
                }
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, err = "0" });
                
            }
            catch (DbEntityValidationException e)
            {
                string contents = helper.getCatchError(e, null);
                if (!helper.debug)
                {
                    contents = helper.logCongtent;
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                if (!helper.debug)
                {
                    contents = helper.logCongtent;
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }

        }
    }
}

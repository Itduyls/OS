using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using API.Models;
using Helper;
using Newtonsoft.Json;
namespace API.Controllers
{
    public class api_tableController : ApiController
    {
        [Authorize]
        public string getipaddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "localhost";
        }
        [HttpPut]
        public async Task<HttpResponseMessage> Update_Table(api_table api_Table)
        {
            var identity = User.Identity as ClaimsIdentity;
            string fdlang = "";
            IEnumerable<Claim> claims = identity.Claims;
            string ip = getipaddress();
            string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
            string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
            string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
            bool ad = claims.Where(p => p.Type == "ad").FirstOrDefault()?.Value == "True";
            string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
            List<string> delfiles = new List<string>();
            try
            {
                if (identity == null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = "Bạn không có quyền truy cập chức năng này!", err = "1" });
                }
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = "Bạn không có quyền truy cập chức năng này!", err = "1" });
            }
            try
            {
                using (DBEntities db = new DBEntities())
                {
                    api_Table.created_by = name;
                    api_Table.created_date = DateTime.Now;
                    api_Table.created_ip = ip;
                    api_Table.created_token_id = tid;
                    db.Entry(api_Table).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    #region add cms_logs
                    if (helper.wlog)
                    {

                        cms_logs log = new cms_logs();
                        log.log_title = "Sửa mô tả " + api_Table.table_name;
                        log.log_content = JsonConvert.SerializeObject(new { data = api_Table });
                        log.log_module = "Api_table";
                        log.id_key = api_Table.table_id.ToString();
                        log.created_date = DateTime.Now;
                        log.created_by = uid;
                        log.created_token_id = tid;
                        log.created_ip = ip;
                        db.cms_logs.Add(log);
                        db.SaveChanges();

                    }
                    #endregion
                    return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                }

            }
            catch (DbEntityValidationException e)
            {
                string contents = helper.getCatchError(e, null);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdlang, contents }), domainurl + "api_Table/Update_api_Table", ip, tid, "Lỗi khi cập nhật Bảng", 0, "api_Table");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdlang, contents }), domainurl + "api_Table/Update_api_Table", ip, tid, "Lỗi khi cập nhật Bảng", 0, "api_Table");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }
        [HttpPut]
        public async Task<HttpResponseMessage> Update_Groupname(groupName groupname)
        {
            var identity = User.Identity as ClaimsIdentity;
            string fdlang = "";
            IEnumerable<Claim> claims = identity.Claims;
            string ip = getipaddress();
            string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
            string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
            string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
            bool ad = claims.Where(p => p.Type == "ad").FirstOrDefault()?.Value == "True";
            string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
            List<string> delfiles = new List<string>();
            try
            {
                if (identity == null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = "Bạn không có quyền truy cập chức năng này!", err = "1" });
                }
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = "Bạn không có quyền truy cập chức năng này!", err = "1" });
            }
            try
            {
                using (DBEntities db = new DBEntities())
                {
                    var tables = db.api_table.AsNoTracking().Where(s => s.group_name == groupname.groupNameOld).ToArray();
                    foreach (var item in tables)
                    {
                        item.group_name = groupname.groupNameNew;
                        db.Entry(item).State = EntityState.Modified;
                    }
                    await db.SaveChangesAsync();

                    
                    return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                }

            }
            catch (DbEntityValidationException e)
            {
                string contents = helper.getCatchError(e, null);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdlang, contents }), domainurl + "api_Table/Update_api_Table", ip, tid, "Lỗi khi cập nhật Bảng", 0, "api_Table");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdlang, contents }), domainurl + "api_Table/Update_api_Table", ip, tid, "Lỗi khi cập nhật Bảng", 0, "api_Table");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }
        [HttpPut]
        public async Task<HttpResponseMessage> Update_Colum(api_col api_Col)
        {
            var identity = User.Identity as ClaimsIdentity;
            string fdlang = "";
            IEnumerable<Claim> claims = identity.Claims;
            string ip = getipaddress();
            string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
            string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
            string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
            bool ad = claims.Where(p => p.Type == "ad").FirstOrDefault()?.Value == "True";
            string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
            List<string> delfiles = new List<string>();
            try
            {
                if (identity == null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = "Bạn không có quyền truy cập chức năng này!", err = "1" });
                }
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = "Bạn không có quyền truy cập chức năng này!", err = "1" });
            }
            try
            {
                using (DBEntities db = new DBEntities())
                {
                    api_Col.created_by = name;
                    api_Col.created_date = DateTime.Now;
                    api_Col.created_ip = ip;
                    api_Col.created_token_id = tid;
                    db.Entry(api_Col).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    #region add cms_logs
                    if (helper.wlog)
                    {

                        cms_logs log = new cms_logs();
                        log.log_title = "Sửa mô tả " + api_Col.col_name;
                        log.log_content = JsonConvert.SerializeObject(new { data = api_Col });
                        log.log_module = "Api_table";
                        log.id_key = api_Col.col_id.ToString();
                        log.created_date = DateTime.Now;
                        log.created_by = uid;
                        log.created_token_id = tid;
                        log.created_ip = ip;
                        db.cms_logs.Add(log);
                        db.SaveChanges();

                    }
                    #endregion
                    return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                }

            }
            catch (DbEntityValidationException e)
            {
                string contents = helper.getCatchError(e, null);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdlang, contents }), domainurl + "dispatch_Books/Update_ca_places", ip, tid, "Lỗi khi cập nhật Công văn", 0, "dispatch_Books");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdlang, contents }), domainurl + "dispatch_Books/Update_ca_places", ip, tid, "Lỗi khi cập nhật Công văn", 0, "dispatch_Books");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }

    }
}
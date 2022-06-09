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
    [Authorize]
    public class doc_followsController : ApiController
    {

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


        [HttpPost]
        public async Task<HttpResponseMessage> Add_follow(doc_follows Follow)
        {
            var identity = User.Identity as ClaimsIdentity;
            IEnumerable<Claim> claims = identity.Claims;
            string ip = getipaddress();
            string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
            string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
            string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
            string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
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
                    db.doc_follows.Add(Follow);
                    await db.SaveChangesAsync();

                    #region add cms_logs
                    if (helper.wlog)
                    {

                        cms_logs log = new cms_logs();
                        log.log_title = "Thêm khẩn cấp " + Follow.receive_by_name;
                        log.log_content = JsonConvert.SerializeObject(new { data = Follow });
                        log.log_module = "Khẩn cấp";
                        log.id_key = Follow.follow_id.ToString();
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
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = contents }), domainurl + "Follow/Add_Follow", ip, tid, "Lỗi khi thêm khẩn cấp", 0, "Follow");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = contents }), domainurl + "Follow/Add_Follow", ip, tid, "Lỗi khi thêm khẩn cấp", 0, "Follow");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }
        [HttpPut]
        public async Task<HttpResponseMessage> Update_follow(doc_follows Follow)
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

                    db.Entry(Follow).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    #region add cms_logs
                    if (helper.wlog)
                    {

                        cms_logs log = new cms_logs();
                        log.log_title = "Sửa khẩn cấp " + Follow.receive_by_name;
                        log.log_content = JsonConvert.SerializeObject(new { data = Follow });
                        log.log_module = "Khẩn cấp";
                        log.id_key = Follow.follow_id.ToString();
                        log.modified_date = DateTime.Now;
                        log.modified_by = uid;
                        log.modified_token_id = tid;
                        log.modified_ip = ip;
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
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdlang, contents }), domainurl + "Follow/Update_follow", ip, tid, "Lỗi khi cập nhật Khẩn cấp", 0, "Follow");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdlang, contents }), domainurl + "Follow/Update_follow", ip, tid, "Lỗi khi cập nhật Khẩn cấp", 0, "Follow");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }



        [HttpDelete]
        public async Task<HttpResponseMessage> Delete_follow([FromBody] List<string> id)
        {
            var identity = User.Identity as ClaimsIdentity;
            IEnumerable<Claim> claims = identity.Claims;
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
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                bool ad = claims.Where(p => p.Type == "ad").FirstOrDefault()?.Value == "True";
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                try
                {
                    using (DBEntities db = new DBEntities())
                    {
                        var das = await db.doc_follows.Where(a => id.Contains(a.follow_id)).ToListAsync();
                        List<string> paths = new List<string>();
                        if (das != null)
                        {
                            List<doc_follows> del = new List<doc_follows>();
                            foreach (var da in das)
                            {
                                if (ad)
                                {
                                    del.Add(da);

                                }
                                #region add cms_logs
                                if (helper.wlog)
                                {

                                    cms_logs log = new cms_logs();
                                    log.log_title = "Xóa khẩn cấp" + da.receive_by_name;

                                    log.log_module = "Khẩn cấp";
                                    log.id_key = da.follow_id.ToString();
                                    log.modified_date = DateTime.Now;
                                    log.modified_by = uid;
                                    log.modified_token_id = tid;
                                    log.modified_ip = ip;
                                    db.cms_logs.Add(log);
                                    db.SaveChanges();

                                }
                                #endregion
                            }
                            if (del.Count == 0)
                            {
                                return Request.CreateResponse(HttpStatusCode.OK, new { err = "1", ms = "Bạn không có quyền xóa dữ liệu." });
                            }
                            db.doc_follows.RemoveRange(del);
                        }
                        await db.SaveChangesAsync();
                        foreach (string strPath in paths)
                        {
                            bool exists = File.Exists(strPath);
                            if (exists)
                                System.IO.File.Delete(strPath);
                        }

                        return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                    }
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = id, contents }), domainurl + "Follow/Delete_follow", ip, tid, "Lỗi khi xoá khẩn cấp", 0, "Follow");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = id, contents }), domainurl + "Follow/Delete_follow", ip, tid, "Lỗi khi xoá khẩn cấp", 0, "Follow");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }

            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }

        //[HttpPut]
        //public async Task<HttpResponseMessage> Update_Follow_Status(Trangthai trangthai)
        //{
        //    var identity = User.Identity as ClaimsIdentity;
        //    IEnumerable<Claim> claims = identity.Claims;
        //    try
        //    {
        //        if (identity == null)
        //        {
        //            return Request.CreateResponse(HttpStatusCode.OK, new { ms = "Bạn không có quyền truy cập chức năng này!", err = "1" });
        //        }
        //    }
        //    catch
        //    {
        //        return Request.CreateResponse(HttpStatusCode.OK, new { ms = "Bạn không có quyền truy cập chức năng này!", err = "1" });
        //    }
        //    try
        //    {
        //        string ip = getipaddress();
        //        string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
        //        string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
        //        string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
        //        bool ad = claims.Where(p => p.Type == "ad").FirstOrDefault()?.Value == "True";
        //        string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
        //        try
        //        {
        //            using (DBEntities db = new DBEntities())
        //            {
        //                var das = db.doc_follows.Where(a => (a.follow_id == trangthai.TextID)).FirstOrDefault<doc_follows>();
        //                if (das != null)
        //                {

        //                    das.s = !trangthai.BitTrangthai;

        //                    #region add cms_logs
        //                    if (helper.wlog)
        //                    {

        //                        cms_logs log = new cms_logs();
        //                        log.log_title = "Sửa khẩn cấp khẩn cấp" + das.follow_name;

        //                        log.log_module = "Khẩn cấp";
        //                        log.id_key = das.follow_id.ToString();
        //                        log.date_created = DateTime.Now;
        //                        log.user_created = uid;
        //                        log.token_created_id = tid;
        //                        log.ip_update = ip;
        //                        db.cms_logs.Add(log);
        //                        db.SaveChanges();

        //                    }
        //                    #endregion
        //                    await db.SaveChangesAsync();
        //                }

        //                return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
        //            }
        //        }
        //        catch (DbEntityValidationException e)
        //        {
        //            string contents = helper.getCatchError(e, null);
        //            helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = trangthai.IntID, contents }), domainurl + "Follow/Update_Follow_Status", ip, tid, "Lỗi khi cập nhật khẩn cấp Khẩn cấp", 0, "Follow");
        //            if (!helper.debug)
        //            {
        //                contents = "";
        //            }
        //            return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
        //        }
        //        catch (Exception e)
        //        {
        //            string contents = helper.ExceptionMessage(e);
        //            helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = trangthai.IntID, contents }), domainurl + "Follow/Update_Follow_Status", ip, tid, "Lỗi khi cập nhật khẩn cấp Khẩn cấp", 0, "Follow");
        //            if (!helper.debug)
        //            {
        //                contents = "";
        //            }
        //            return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.BadRequest);
        //    }
        //}
    }
}
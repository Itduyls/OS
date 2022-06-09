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
    public class ca_stampsController : ApiController
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
        public async Task<HttpResponseMessage> Add_stamp()
        {
            var identity = User.Identity as ClaimsIdentity;
            string fdstamp = "";
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
                    if (!Request.Content.IsMimeMultipartContent())
                    {
                        throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
                    }

                    string root = HttpContext.Current.Server.MapPath("~/Portals");
                    string strPath = root + "/Stamp";
                    bool exists = Directory.Exists(strPath);
                    if (!exists)
                        Directory.CreateDirectory(strPath);
                    var provider = new MultipartFormDataStreamProvider(root);

                    // Read the form data and return an async task.
                    var task = Request.Content.ReadAsMultipartAsync(provider).
                    ContinueWith<HttpResponseMessage>(t =>
                    {
                        if (t.IsFaulted || t.IsCanceled)
                        {
                            Request.CreateErrorResponse(HttpStatusCode.InternalServerError, t.Exception);
                        }
                        fdstamp = provider.FormData.GetValues("stamp").SingleOrDefault();
                        doc_ca_stamps stamp = JsonConvert.DeserializeObject<doc_ca_stamps>(fdstamp);
                        // This illustrates how to get thefile names.
                        FileInfo fileInfo = null;
                        MultipartFileData ffileData = null;
                        string newFileName = "";
                        foreach (MultipartFileData fileData in provider.FileData)
                        {
                            string fileName = "";
                            if (string.IsNullOrEmpty(fileData.Headers.ContentDisposition.FileName))
                            {
                                fileName = Guid.NewGuid().ToString();
                            }
                            fileName = fileData.Headers.ContentDisposition.FileName;
                            if (fileName.StartsWith("\"") && fileName.EndsWith("\""))
                            {
                                fileName = fileName.Trim('"');
                            }
                            if (fileName.Contains(@"/") || fileName.Contains(@"\"))
                            {
                                fileName = Path.GetFileName(fileName);
                            }
                            newFileName = Path.Combine(root + "/Stamp", fileName);
                            fileInfo = new FileInfo(newFileName);
                            if (fileInfo.Exists)
                            {
                                fileName = fileInfo.Name.Replace(fileInfo.Extension, "");
                                fileName = fileName + (new Random().Next(0, 10000)) + fileInfo.Extension;

                                newFileName = Path.Combine(root + "/Stamp", fileName);
                            }
                            stamp.image = "/Portals/Stamp/" + fileName;
                            ffileData = fileData;
                        }           
                        db.doc_ca_stamps.Add(stamp);
                        db.SaveChanges();
                        //Add ảnh
                        if (fileInfo != null)
                        {
                            if (!Directory.Exists(fileInfo.Directory.FullName))
                            {
                                Directory.CreateDirectory(fileInfo.Directory.FullName);
                            }
                            File.Move(ffileData.LocalFileName, newFileName);
                            helper.ResizeImage(newFileName, 640, 640, 90);
                        }
                        #region add cms_logs
                        if (helper.wlog)
                        {

                            cms_logs log = new cms_logs();
                            log.log_title = "Thêm tem " + stamp.stamp_name;
                            log.log_content = JsonConvert.SerializeObject(new { data = stamp });
                            log.log_module = "Tem";
                            log.id_key = stamp.stamp_id.ToString();

                            log.created_date = DateTime.Now;
                            log.created_by = uid;
                            log.created_token_id = tid;
                            log.created_ip = ip;
                            db.cms_logs.Add(log);
                            db.SaveChanges();

                        }
                        #endregion
                        return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                    });
                    return await task;
                }

            }
            catch (DbEntityValidationException e)
            {
                string contents = helper.getCatchError(e, null);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdstamp, contents }), domainurl + "doc_ca_stamps/Add_stamp", ip, tid, "Lỗi khi thêm Stamp", 0, "Stamp");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdstamp, contents }), domainurl + "doc_ca_stamps/Add_stamp", ip, tid, "Lỗi khi thêm Stamp", 0, "Stamp  ");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }
        [HttpPut]
        public async Task<HttpResponseMessage> Update_Stamp()
        {
            var identity = User.Identity as ClaimsIdentity;
            string fdstamp = "";
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
                    if (!Request.Content.IsMimeMultipartContent())
                    {
                        throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
                    }

                    string root = HttpContext.Current.Server.MapPath("~/Portals");
                    string strPath = root + "/Stamp";
                    bool exists = Directory.Exists(strPath);
                    if (!exists)
                        Directory.CreateDirectory(strPath);
                    var provider = new MultipartFormDataStreamProvider(root);

                    // Read the form data and return an async task.
                    var task = Request.Content.ReadAsMultipartAsync(provider).
                    ContinueWith<HttpResponseMessage>(t =>
                    {
                        if (t.IsFaulted || t.IsCanceled)
                        {
                            Request.CreateErrorResponse(HttpStatusCode.InternalServerError, t.Exception);
                        }
                        fdstamp = provider.FormData.GetValues("stamp").SingleOrDefault();
                        doc_ca_stamps stamp = JsonConvert.DeserializeObject<doc_ca_stamps>(fdstamp);
                        var stampOld = db.doc_ca_stamps.AsNoTracking().Where(s => s.stamp_id == stamp.stamp_id).FirstOrDefault<doc_ca_stamps>();
                        if (stampOld.image != null && stampOld.image != "")
                        {
                            string fileOld = stampOld.image.Substring(8);
                            delfiles.Add(root + fileOld);
                        }

                        // This illustrates how to get thefile names.
                        FileInfo fileInfo = null;
                        MultipartFileData ffileData = null;
                        string newFileName = "";
                        foreach (MultipartFileData fileData in provider.FileData)
                        {
                            string fileName = "";


                            if (string.IsNullOrEmpty(fileData.Headers.ContentDisposition.FileName))
                            {
                                fileName = Guid.NewGuid().ToString();
                            }
                            fileName = fileData.Headers.ContentDisposition.FileName;
                            if (fileName.StartsWith("\"") && fileName.EndsWith("\""))
                            {
                                fileName = fileName.Trim('"');
                            }
                            if (fileName.Contains(@"/") || fileName.Contains(@"\"))
                            {
                                fileName = Path.GetFileName(fileName);
                            }
                            newFileName = Path.Combine(root + "/Stamp", fileName);
                            fileInfo = new FileInfo(newFileName);
                            if (fileInfo.Exists)
                            {
                                fileName = fileInfo.Name.Replace(fileInfo.Extension, "");
                                fileName = fileName + (new Random().Next(0, 10000)) + fileInfo.Extension;

                                newFileName = Path.Combine(root + "/Stamp", fileName);
                            }

                            stamp.image = "/Portals/Stamp/" + fileName;
                            ffileData = fileData;
                        }
                        if (stamp.is_default == true)
                        {
                            var das1 = db.doc_ca_stamps.Where(a => (a.is_default == true)).FirstOrDefault<doc_ca_stamps>();
                            if (das1 != null && das1.stamp_id != stamp.stamp_id)
                            {

                                das1.is_default = false;

                            }
                        }
                        db.Entry(stamp).State = EntityState.Modified;
                        db.SaveChanges();
                        //Add ảnh
                        if (fileInfo != null)
                        {
                            foreach (string fpath in delfiles)
                            {
                                if (File.Exists(fpath))
                                    File.Delete(fpath);
                            }
                            if (!Directory.Exists(fileInfo.Directory.FullName))
                            {
                                Directory.CreateDirectory(fileInfo.Directory.FullName);
                            }
                            File.Move(ffileData.LocalFileName, newFileName);
                            helper.ResizeImage(newFileName, 640, 640, 90);
                        }
                        #region add cms_logs
                        if (helper.wlog)
                        {

                            cms_logs log = new cms_logs();
                            log.log_title = "Sửa tem " + stamp.stamp_name;
                            log.log_content = JsonConvert.SerializeObject(new { data = stamp });
                            log.log_module = "Tem";
                            log.id_key = stamp.stamp_id.ToString();

                            log.modified_date = DateTime.Now;
                            log.modified_by = uid;
                            log.modified_token_id = tid;
                            log.modified_ip = ip;
                            db.cms_logs.Add(log);
                            db.SaveChanges();

                        }
                        #endregion
                        return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                    });
                    return await task;
                }

            }
            catch (DbEntityValidationException e)
            {
                string contents = helper.getCatchError(e, null);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdstamp, contents }), domainurl + "doc_ca_stamps/Update_Stamp", ip, tid, "Lỗi khi cập nhật Stamp", 0, "Stamp");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdstamp, contents }), domainurl + "doc_ca_stamps/Update_Stamp", ip, tid, "Lỗi khi cập nhật Stamp", 0, "Stamp");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }



        [HttpDelete]
        public async Task<HttpResponseMessage> Delete_stamp([FromBody] List<int> id)
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
                        var das = await db.doc_ca_stamps.Where(a => id.Contains(a.stamp_id)).ToListAsync();
                        List<string> paths = new List<string>();
                        if (das != null)
                        {
                            List<doc_ca_stamps> del = new List<doc_ca_stamps>();
                            foreach (var da in das)
                            {
                                if (ad)
                                {
                                    del.Add(da);
                                    if (!string.IsNullOrWhiteSpace(da.image))
                                        paths.Add(HttpContext.Current.Server.MapPath("~/Portals") + da.image.Substring(8));
                                }
                                #region add cms_logs
                                if (helper.wlog)
                                {

                                    cms_logs log = new cms_logs();
                                    log.log_title = "Xóa tem " + da.stamp_name;

                                    log.log_module = "Tem";
                                    log.id_key = da.stamp_id.ToString();
                                    log.created_date = DateTime.Now;
                                    log.created_by = uid;
                                    log.created_token_id = tid;
                                    log.created_ip = ip;
                                    db.cms_logs.Add(log);
                                    db.SaveChanges();

                                }
                                #endregion
                            }
                            if (del.Count == 0)
                            {
                                return Request.CreateResponse(HttpStatusCode.OK, new { err = "1", ms = "Bạn không có quyền xóa dữ liệu." });
                            }
                            db.doc_ca_stamps.RemoveRange(del);
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
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = id, contents }), domainurl + "doc_ca_stamps/Delete_stamp", ip, tid, "Lỗi khi xoá tem", 0, "Stamp");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = id, contents }), domainurl + "doc_ca_stamps/Delete_stamp", ip, tid, "Lỗi khi xoá tem", 0, "Stamp");
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
        

        [HttpPut]
        public async Task<HttpResponseMessage> Update_TrangthaiStamp(Trangthai trangthai)
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
                        var das = db.doc_ca_stamps.Where(a => (a.stamp_id == trangthai.IntID)).FirstOrDefault<doc_ca_stamps>();
                        if (das != null)
                        {

                            das.status = !trangthai.BitTrangthai;

                            #region add cms_logs
                            if (helper.wlog)
                            {

                                cms_logs log = new cms_logs();
                                log.log_title = "Sửa trạng thái tem" + das.stamp_name;

                                log.log_module = "Tem";
                                log.id_key = das.stamp_id.ToString();
                                log.modified_date = DateTime.Now;
                                log.modified_by = uid;
                                log.modified_token_id = tid;
                                log.modified_ip = ip;
                                db.cms_logs.Add(log);
                                db.SaveChanges();

                            }
                            #endregion
                            await db.SaveChangesAsync();
                        }

                        return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                    }
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = trangthai.IntID, contents }), domainurl + "doc_ca_stamps/Update_TrangthaiStamp", ip, tid, "Lỗi khi cập nhật trạng thái Stamps", 0, "doc_ca_stamps");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = trangthai.IntID, contents }), domainurl + "doc_ca_stamps/Update_TrangthaiStamp", ip, tid, "Lỗi khi cập nhật trạng thái Stamps", 0, "doc_ca_stamps");
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

        [HttpPut]
        public async Task<HttpResponseMessage> Update_DefaultStamp(Ismain ismain)
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
                        var das1 = db.doc_ca_stamps.Where(a => (a.is_default == true & a.stamp_id!=ismain.IntID)).ToListAsync();
                        if (das1 != null)
                        {
                            foreach (var item in await das1)
                            {
                                item.is_default = false;
                            }
                        }
                        var das = db.doc_ca_stamps.Where(a => (a.stamp_id == ismain.IntID&a.is_default!=true)).FirstOrDefault<doc_ca_stamps>();
                        if (das != null)
                        {

                            das.is_default = !ismain.BitMain;

                            #region add cms_logs
                            if (helper.wlog)
                            {
                                cms_logs log = new cms_logs();
                                log.log_title = "Sửa trạng thái tem" + das.stamp_name;
                                log.log_module = "Tem";
                                log.id_key = das.stamp_id.ToString();
                                log.modified_date = DateTime.Now;
                                log.modified_by = uid;
                                log.modified_token_id = tid;
                                log.modified_ip = ip;
                                db.cms_logs.Add(log);
                                db.SaveChanges();

                            }
                            #endregion
                            await db.SaveChangesAsync();
                        }

                        return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                    }
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = ismain.IntID, contents }), domainurl + "doc_ca_stamps/Update_DefaultStamp", ip, tid, "Lỗi khi cập nhật trạng thái Stamps", 0, "doc_ca_stamps");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = ismain.IntID, contents }), domainurl + "doc_ca_stamps/Update_DefaultStamp", ip, tid, "Lỗi khi cập nhật trạng thái Stamps", 0, "doc_ca_stamps");
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
    }
    
}
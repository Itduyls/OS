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
    public class ca_placesController : ApiController
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
        public async Task<HttpResponseMessage> Add_ca_places(ca_places ca_Places)
        {
            var identity = User.Identity as ClaimsIdentity;
            string fdlang = "";
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
                    db.ca_places.Add(ca_Places);
                    await db.SaveChangesAsync();

                    #region add cms_logs
                    if (helper.wlog)
                    {

                        cms_logs log = new cms_logs();
                        log.log_title = "Thêm địa danh " + ca_Places.name;
                        log.log_content = JsonConvert.SerializeObject(new { data = ca_Places });
                        log.log_module = "Địa danh";
                        log.id_key = ca_Places.place_id.ToString();
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
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdlang, contents }), domainurl + "ca_places/Add_ca_places", ip, tid, "Lỗi khi thêm Địa danh", 0, "ca_places");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdlang, contents }), domainurl + "ca_places/Add_ca_places", ip, tid, "Lỗi khi thêm Địa danh", 0, "ca_places  ");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }
        [HttpPut]
        public async Task<HttpResponseMessage> Update_ca_places(ca_places ca_Places)
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

                    db.Entry(ca_Places).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    #region add cms_logs
                    if (helper.wlog)
                    {

                        cms_logs log = new cms_logs();
                        log.log_title = "Sửa địa danh " + ca_Places.name;
                        log.log_content = JsonConvert.SerializeObject(new { data = ca_Places });
                        log.log_module = "Địa danh";
                        log.id_key = ca_Places.place_id.ToString();
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
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdlang, contents }), domainurl + "ca_places/Update_ca_places", ip, tid, "Lỗi khi cập nhật Địa danh", 0, "ca_places");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdlang, contents }), domainurl + "ca_places/Update_ca_places", ip, tid, "Lỗi khi cập nhật Địa danh", 0, "ca_places");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }



        [HttpDelete]
        public async Task<HttpResponseMessage> Delete_ca_places([FromBody] List<int> id)
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
                        var das = await db.ca_places.Where(a => id.Contains(a.place_id)).ToListAsync();
                        List<string> paths = new List<string>();
                        if (das != null)
                        {
                            List<ca_places> del = new List<ca_places>();
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
                                    log.log_title = "Xóa địa danh" + da.name;

                                    log.log_module = "Địa danh";
                                    log.id_key = da.place_id.ToString();
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
                            db.ca_places.RemoveRange(del);
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
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = id, contents }), domainurl + "ca_places/Delete_ca_places", ip, tid, "Lỗi khi xoá địa danh", 0, "ca_places");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = id, contents }), domainurl + "ca_places/Delete_ca_places", ip, tid, "Lỗi khi xoá địa danh", 0, "ca_places");
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
        //#region Excel
        //[HttpPost]
        //public async Task<HttpResponseMessage> ExportExcel()
        //{
        //    string ListErr = "";
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
        //    string ip = getipaddress();
        //    string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
        //    string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
        //    string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
        //    bool ad = claims.Where(p => p.Type == "ad").FirstOrDefault()?.Value == "True";
        //    string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
        //    try
        //    {
        //        using (DBEntities db = new DBEntities())
        //        {
        //            if (!Request.Content.IsMimeMultipartContent())
        //            {
        //                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
        //            }

        //            string root = HttpContext.Current.Server.MapPath("~/Portals");
        //            string strPath = root + "/Lang";
        //            bool exists = Directory.Exists(strPath);
        //            if (!exists)
        //                Directory.CreateDirectory(strPath);
        //            var provider = new MultipartFormDataStreamProvider(root);

        //            // Read the form data and return an async task.
        //            var task = Request.Content.ReadAsMultipartAsync(provider).
        //            ContinueWith<HttpResponseMessage>(t =>
        //            {
        //                if (t.IsFaulted || t.IsCanceled)
        //                {
        //                    Request.CreateErrorResponse(HttpStatusCode.InternalServerError, t.Exception);
        //                }
        //                foreach (MultipartFileData fileData in provider.FileData)
        //                {
        //                    string fileName = "";
        //                    if (string.IsNullOrEmpty(fileData.Headers.ContentDisposition.FileName))
        //                    {
        //                        fileName = Guid.NewGuid().ToString();
        //                    }
        //                    fileName = fileData.Headers.ContentDisposition.FileName;
        //                    if (fileName.StartsWith("\"") && fileName.EndsWith("\""))
        //                    {
        //                        fileName = fileName.Trim('"');
        //                    }
        //                    if (fileName.Contains(@"/") || fileName.Contains(@"\"))
        //                    {
        //                        fileName = Path.GetFileName(fileName);
        //                    }
        //                    if (!fileName.ToLower().Contains(".xls"))
        //                    {
        //                        ListErr = "File Excel không đúng định dạng! Kiểm tra lại mẫu Import";
        //                        return Request.CreateResponse(HttpStatusCode.OK, new { err = "1", ms = ListErr });
        //                    }
        //                    var newFileName = Path.Combine(root + "/Import", fileName);
        //                    var fileInfo = new FileInfo(newFileName);
        //                    if (fileInfo.Exists)
        //                    {
        //                        fileName = fileInfo.Name.Replace(fileInfo.Extension, "");
        //                        fileName = fileName + (new Random().Next(0, 10000)) + fileInfo.Extension;

        //                        newFileName = Path.Combine(root + "/Import", fileName);
        //                    }
        //                    if (!Directory.Exists(fileInfo.Directory.FullName))
        //                    {
        //                        Directory.CreateDirectory(fileInfo.Directory.FullName);
        //                    }
        //                    File.Move(fileData.LocalFileName, newFileName);
        //                    FileInfo temp = new FileInfo(newFileName);
        //                    using (ExcelPackage pck = new ExcelPackage(temp))
        //                    {
        //                        List<cms_lang> dvs = new List<cms_lang>();
        //                        ExcelWorksheet ws = pck.Workbook.Worksheets.First();
        //                        List<string> cols = new List<string>();
        //                        var dvcs = db.cms_lang.Select(a => new { a.lang_id, a.name }).ToList();
        //                        for (int i = 5; i <= ws.Dimension.End.Row; i++)
        //                        {
        //                            if (ws.Cells[i, 1].Value == null)
        //                            {
        //                                break;
        //                            }
        //                            cms_lang dv = new cms_lang();
        //                            for (int j = 1; j <= ws.Dimension.End.Column; j++)
        //                            {
        //                                if (ws.Cells[3, j].Value == null)
        //                                {
        //                                    break;
        //                                }
        //                                var column = ws.Cells[3, j].Value;
        //                                var vl = ws.Cells[i, j].Value;
        //                                if (column != null && vl != null)
        //                                {
        //                                    PropertyInfo propertyInfo = db.cms_lang.GetType().GetProperty(column.ToString());
        //                                    propertyInfo.SetValue(db.cms_lang, Convert.ChangeType(vl,
        //                                    propertyInfo.PropertyType), null);
        //                                }
        //                            }
        //                            if (dvcs.Count(a => a.lang_id == dv.lang_id || a.name == dv.name) > 0)
        //                                break;

        //                            dvs.Add(dv);
        //                        }
        //                        if (dvs.Count > 0)
        //                        {
        //                            db.cms_lang.AddRange(dvs);
        //                            db.SaveChangesAsync();
        //                            File.Delete(newFileName);
        //                        }
        //                    }
        //                }
        //                #region add cms_logs
        //                if (helper.wlog)
        //                {

        //                    cms_logs log = new cms_logs();
        //                    log.log_title = "Xuất Excel ngôn ngữ ";

        //                    log.log_module = "Ngôn ngữ";

        //                    log.date_created = DateTime.Now;
        //                    log.user_created = uid;
        //                    log.token_created_id = tid;
        //                    log.ip_update = ip;
        //                    db.cms_logs.Add(log);
        //                    db.SaveChanges();

        //                }
        //                #endregion
        //                return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
        //            });
        //            return await task;
        //        }

        //    }
        //    catch (DbEntityValidationException e)
        //    {
        //        string contents = helper.getCatchError(e, null);
        //        helper.saveLog(uid, name, JsonConvert.SerializeObject(new { contents }), domainurl + "cms_lang/ImportExcel", ip, tid, "Lỗi khi import cms_lang", 0, "cms_lang");
        //        if (!helper.debug)
        //        {
        //            contents = "";
        //        }
        //        return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
        //    }
        //    catch (Exception e)
        //    {
        //        string contents = helper.ExceptionMessage(e);
        //        helper.saveLog(uid, name, JsonConvert.SerializeObject(new { contents }), domainurl + "cms_lang/ImportExcel", ip, tid, "Lỗi khi import cms_lang", 0, "cms_lang");
        //        if (!helper.debug)
        //        {
        //            contents = "";
        //        }
        //        return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
        //    }
        //}
        //#endregion

        [HttpPut]
        public async Task<HttpResponseMessage> Update_StatusPlace(Trangthai trangthai)
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
                        var das = db.ca_places.Where(a => (a.place_id == trangthai.IntID)).FirstOrDefault<ca_places>();
                        if (das != null)
                        {

                            das.status = !trangthai.BitTrangthai;

                            #region add cms_logs
                            if (helper.wlog)
                            {

                                cms_logs log = new cms_logs();
                                log.log_title = "Sửa trạng thái địa danh" + das.name;

                                log.log_module = "Địa danh";
                                log.id_key = das.place_id.ToString();
                                log.created_date = DateTime.Now;
                                log.created_by = uid;
                                log.created_token_id = tid;
                                log.created_ip = ip;
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
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = trangthai.IntID, contents }), domainurl + "ca_places/Update_StatusPlace", ip, tid, "Lỗi khi cập nhật trạng thái Địa danh", 0, "ca_places");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = trangthai.IntID, contents }), domainurl + "ca_places/Update_StatusPlace", ip, tid, "Lỗi khi cập nhật trạng thái Địa danh", 0, "ca_places");
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
using API.Models;
using Helper;
using Microsoft.ApplicationBlocks.Data;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace Controllers
{
  
    [Authorize]
    public class ModulesController : ApiController
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
        public async Task<HttpResponseMessage> Add_Module()
        {
            var identity = User.Identity as ClaimsIdentity;
            string fdmodel = "";
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
                    string strPath = root + "/Module";
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
                     fdmodel = provider.FormData.GetValues("model").SingleOrDefault();
                     sys_modules model = JsonConvert.DeserializeObject<sys_modules>(fdmodel);
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
                         newFileName = Path.Combine(root + "/Module", fileName);
                         fileInfo = new FileInfo(newFileName);
                         if (fileInfo.Exists)
                         {
                             fileName = fileInfo.Name.Replace(fileInfo.Extension, "");
                             fileName = fileName + (new Random().Next(0, 10000)) + fileInfo.Extension;

                             newFileName = Path.Combine(root + "/Module", fileName);
                         }
                         model.image = "/Portals/Module/" + fileName;
                         ffileData = fileData;
                     }
                     model.modified_date = DateTime.Now;
                     model.modified_by = uid;
                     model.modified_token_id = tid;
                     model.modified_ip = ip;
                     db.sys_modules.Add(model);
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
                     return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                 });
                    return await task;
                }

            }
            catch (DbEntityValidationException e)
            {
                string contents = helper.getCatchError(e, null);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdmodel, contents }), domainurl + "Modules/Add_Module", ip, tid, "Lỗi khi thêm Module", 0, "Module");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdmodel, contents }), domainurl + "Modules/Add_Module", ip, tid, "Lỗi khi thêm Module", 0, "Module");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }

        [HttpPut]
        public async Task<HttpResponseMessage> Update_Module()
        {
            var identity = User.Identity as ClaimsIdentity;
            string fdmodel = "";
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
                    string strPath = root + "/Module";
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
                        fdmodel = provider.FormData.GetValues("model").SingleOrDefault();
                        sys_modules model = JsonConvert.DeserializeObject<sys_modules>(fdmodel);
                        // This illustrates how to get thefile names.
                        FileInfo fileInfo = null;
                        MultipartFileData ffileData = null;
                        string newFileName = "";
                        foreach (MultipartFileData fileData in provider.FileData)
                        {
                            delfiles.Add(root + model.image);
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
                            newFileName = Path.Combine(root + "/Module", fileName);
                            fileInfo = new FileInfo(newFileName);
                            if (fileInfo.Exists)
                            {
                                fileName = fileInfo.Name.Replace(fileInfo.Extension, "");
                                fileName = fileName + (new Random().Next(0, 10000)) + fileInfo.Extension;

                                newFileName = Path.Combine(root + "/Module", fileName);
                            }
                            model.image = "/Portals/Module/" + fileName;
                            ffileData = fileData;
                        }
                        model.modified_date = DateTime.Now;
                        model.modified_by = uid;
                        model.modified_token_id = tid;
                        model.modified_ip = ip;
                        db.Entry(model).State = EntityState.Modified;
                        db.SaveChanges();
                        //Add ảnh
                        if (fileInfo != null)
                        {
                            foreach (string fpath in delfiles)
                            {
                                File.Delete(fpath);
                            }
                            if (!Directory.Exists(fileInfo.Directory.FullName))
                            {
                                Directory.CreateDirectory(fileInfo.Directory.FullName);
                            }
                            File.Move(ffileData.LocalFileName, newFileName);
                            helper.ResizeImage(newFileName, 640, 640, 90);
                        }
                        return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                    });
                    return await task;
                }

            }
            catch (DbEntityValidationException e)
            {
                string contents = helper.getCatchError(e, null);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdmodel, contents }), domainurl + "Modules/Update_Module", ip, tid, "Lỗi khi cập nhật Module", 0, "Module");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdmodel, contents }), domainurl + "Modules/Update_Module", ip, tid, "Lỗi khi cập nhật Module", 0, "Module");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }

        [HttpDelete]
        public async Task<HttpResponseMessage> Del_Module([FromBody] List<int> ids)
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
                        var das = await db.sys_modules.Where(a => ids.Contains(a.module_id)).ToListAsync();
                        List<string> paths = new List<string>();
                        if (das != null)
                        {
                            List<sys_modules> del = new List<sys_modules>();
                            foreach (var da in das)
                            {
                                if (da.modified_by == uid || ad)
                                {
                                    del.Add(da);
                                    if (!string.IsNullOrWhiteSpace(da.image))
                                        paths.Add(HttpContext.Current.Server.MapPath("~/") + da.image);
                                }
                            }
                            if (del.Count == 0)
                            {
                                return Request.CreateResponse(HttpStatusCode.OK, new { err = "1", ms = "Bạn không có quyền xóa menu này." });
                            }
                            db.sys_modules.RemoveRange(del);
                        }
                        await db.SaveChangesAsync();
                        foreach (string strPath in paths)
                        {
                            bool exists = Directory.Exists(strPath);
                            if (exists)
                                System.IO.File.Delete(strPath);
                        }
                        return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                    }
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = ids, contents }), domainurl + "Modules/Del_Module", ip, tid, "Lỗi khi xoá Module", 0, "Module");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = ids, contents }), domainurl + "Modules/Del_Module", ip, tid, "Lỗi khi xoá Module", 0, "Module");
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
        public async Task<HttpResponseMessage> Update_TrangthaiModule([FromBody] List<int> ids, [FromBody] List<bool> tts)
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
                        var das = await db.sys_modules.Where(a => ids.Contains(a.module_id)).ToListAsync();
                        if (das != null)
                        {
                            List<sys_modules> del = new List<sys_modules>();
                            for (int i = 0; i < das.Count; i++)
                            {
                                var da = das[i];
                                if (da.modified_by == uid || ad)
                                {
                                    da.status = tts[i];
                                }
                            }
                            await db.SaveChangesAsync();
                        }
                        return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                    }
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = ids, contents }), domainurl + "Modules/Update_TrangthaiModule", ip, tid, "Lỗi khi cập nhật trạng thái Module", 0, "Module");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = ids, contents }), domainurl + "Modules/Update_TrangthaiModule", ip, tid, "Lỗi khi cập nhật trạng thái Module", 0, "Module");
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

        #region Excel
        [HttpPost]
        public async Task<HttpResponseMessage> ImportExcel()
        {
            string ListErr = "";
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
                    if (!Request.Content.IsMimeMultipartContent())
                    {
                        throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
                    }

                    string root = HttpContext.Current.Server.MapPath("~/Portals");
                    string strPath = root + "/Module";
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
                           if (!fileName.ToLower().Contains(".xls"))
                           {
                               ListErr = "File Excel không đúng định dạng! Kiểm tra lại mẫu Import";
                               return Request.CreateResponse(HttpStatusCode.OK, new { err = "1", ms = ListErr });
                           }
                           var newFileName = Path.Combine(root + "/Import", fileName);
                           var fileInfo = new FileInfo(newFileName);
                           if (fileInfo.Exists)
                           {
                               fileName = fileInfo.Name.Replace(fileInfo.Extension, "");
                               fileName = fileName + (new Random().Next(0, 10000)) + fileInfo.Extension;

                               newFileName = Path.Combine(root + "/Import", fileName);
                           }
                           if (!Directory.Exists(fileInfo.Directory.FullName))
                           {
                               Directory.CreateDirectory(fileInfo.Directory.FullName);
                           }
                           File.Move(fileData.LocalFileName, newFileName);
                           FileInfo temp = new FileInfo(newFileName);
                           using (ExcelPackage pck = new ExcelPackage(temp))
                           {
                               List<sys_modules> dvs = new List<sys_modules>();
                               ExcelWorksheet ws = pck.Workbook.Worksheets.First();
                               List<string> cols = new List<string>();
                               var dvcs = db.sys_modules.Select(a => new { a.module_id, a.module_name }).ToList();
                               for (int i = 5; i <= ws.Dimension.End.Row; i++)
                               {
                                   if (ws.Cells[i, 1].Value == null)
                                   {
                                       break;
                                   }
                                   sys_modules dv = new sys_modules();
                                   for (int j = 1; j <= ws.Dimension.End.Column; j++)
                                   {
                                       if (ws.Cells[3, j].Value == null)
                                       {
                                           break;
                                       }
                                       var column = ws.Cells[3, j].Value;
                                       var vl = ws.Cells[i, j].Value;
                                       if (column != null && vl != null)
                                       {
                                           PropertyInfo propertyInfo = db.sys_modules.GetType().GetProperty(column.ToString());
                                           propertyInfo.SetValue(db.sys_modules, Convert.ChangeType(vl,
                                           propertyInfo.PropertyType), null);
                                       }
                                   }
                                   if (dvcs.Count(a => a.module_id == dv.module_id || a.module_name == dv.module_name) > 0)
                                       break;
                                   dv.modified_ip = ip;
                                   dv.modified_by = uid;
                                   dv.modified_date = DateTime.Now;
                                   dv.modified_token_id = tid;
                                   dvs.Add(dv);
                               }
                               if (dvs.Count > 0)
                               {
                                   db.sys_modules.AddRange(dvs);
                                   db.SaveChangesAsync();
                                   File.Delete(newFileName);
                               }
                           }
                       }
                       return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                   });
                    return await task;
                }

            }
            catch (DbEntityValidationException e)
            {
                string contents = helper.getCatchError(e, null);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { contents }), domainurl + "Modules/ImportExcel", ip, tid, "Lỗi khi import Module", 0, "Module");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { contents }), domainurl + "Modules/ImportExcel", ip, tid, "Lỗi khi import Module", 0, "Module");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }
        #endregion
    }
}
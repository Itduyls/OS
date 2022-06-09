using API.Models;
using Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
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
    public class RolesController : ApiController
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
        public async Task<HttpResponseMessage> Add_RoleModules([FromBody] List<sys_role_modules> modules)
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
                    //if (!Request.Content.IsMimeMultipartContent())
                    //{
                    //    throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
                    //}
                    var newmodules = modules.Where(a => a.role_module_id == -1);
                    var updatemodules = modules.Where(a => a.role_module_id != -1);
                    foreach (var md in newmodules)
                    {
                        md.modified_date = DateTime.Now;
                        md.modified_by = uid;
                        md.modified_token_id = tid;
                        md.modified_ip = ip;
                    }
                    db.sys_role_modules.AddRange(newmodules);
                    foreach (var md in updatemodules)
                    {
                        md.modified_date = DateTime.Now;
                        md.modified_by = uid;
                        md.modified_token_id = tid;
                        md.modified_ip = ip;
                        db.Entry(md).State = EntityState.Modified;
                    }
                    #region  add Log
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = modules, contents = "" }), domainurl + "Roles/Add_RoleModules", ip, tid, "Phân quyền Module cho Role " + modules.FirstOrDefault()?.role_id, 1, "Role");
                    #endregion
                    await db.SaveChangesAsync();
                    return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                }

            }
            catch (DbEntityValidationException e)
            {
                string contents = helper.getCatchError(e, null);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = modules, contents }), domainurl + "Roles/Add_RoleModules", ip, tid, "Lỗi khi phân quyền Module", 0, "Role");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = modules, contents }), domainurl + "Roles/Add_RoleModules", ip, tid, "Lỗi khi phân quyền Module", 0, "Role");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Delete_UserModules()
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
                    var rus = await db.sys_role_modules.Where(a => a.user_id == uid).ToListAsync();
                    if (rus.Count > 0)
                    {
                        db.sys_role_modules.RemoveRange(rus);
                    }
                    #region  add Log
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = uid, contents = "" }), domainurl + "Roles/Delete_UserModules", ip, tid, "Reset quyền cho tài khoản " + uid, 1, "Role");
                    #endregion
                    await db.SaveChangesAsync();
                    return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                }

            }
            catch (DbEntityValidationException e)
            {
                string contents = helper.getCatchError(e, null);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = uid, contents }), domainurl + "Roles/Delete_UserModules", ip, tid, "Lỗi khi Reset quyền cho tài khoản " + uid, 0, "Role");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = uid, contents }), domainurl + "Roles/Delete_UserModules", ip, tid, "Lỗi khi Reset quyền cho tài khoản " + uid, 0, "Role");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }


        [HttpPost]
        public async Task<HttpResponseMessage> Add_Roles(sys_roles model)
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
                    db.sys_roles.Add(model);
                    #region  add Log
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = model, contents = "" }), domainurl + "Roles/Add_Roles", ip, tid, "Thêm mới Role " + model.role_name, 1, "Role");
                    #endregion
                    await db.SaveChangesAsync();
                    return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                }

            }
            catch (DbEntityValidationException e)
            {
                string contents = helper.getCatchError(e, null);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = model, contents }), domainurl + "Roles/Add_Roles", ip, tid, "Lỗi khi thêm Role", 0, "Role");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = model, contents }), domainurl + "Roles/Add_Roles", ip, tid, "Lỗi khi thêm Role", 0, "Role");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }

        [HttpPut]
        public async Task<HttpResponseMessage> Update_Roles(sys_roles model)
        {
            var identity = User.Identity as ClaimsIdentity;

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
                if (identity == null || !ad)
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
                    db.Entry(model).State = EntityState.Modified;
                    #region  add Log
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = model, contents = "" }), domainurl + "Roles/Update_Roles", ip, tid, "Chỉnh sửa Role " + model.role_name, 1, "Role");
                    #endregion
                    await db.SaveChangesAsync();
                    return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                }

            }
            catch (DbEntityValidationException e)
            {
                string contents = helper.getCatchError(e, null);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = model, contents }), domainurl + "Roles/Update_Roles", ip, tid, "Lỗi khi cập nhật Role", 0, "Role");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = model, contents }), domainurl + "Roles/Update_Roles", ip, tid, "Lỗi khi cập nhật Role", 0, "Role");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }

        [HttpDelete]
        public async Task<HttpResponseMessage> Del_Roles([FromBody] List<string> ids)
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
                        var das = await db.sys_roles.Where(a => ids.Contains(a.role_id)).ToListAsync();
                        if (das != null)
                        {
                            List<sys_roles> del = new List<sys_roles>();
                            foreach (var da in das)
                            {
                                if (ad)
                                {
                                    del.Add(da);
                                    #region  add Log
                                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = da, contents = "" }), domainurl + "Roles/Del_Roles", ip, tid, "Xoá Role " + da.role_name, 1, "Role");
                                    #endregion
                                }
                            }
                            if (del.Count == 0)
                            {
                                return Request.CreateResponse(HttpStatusCode.OK, new { err = "1", ms = "Bạn không có quyền xóa Role này." });
                            }
                            db.sys_roles.RemoveRange(del);
                        }
                        await db.SaveChangesAsync();
                        return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                    }
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = ids, contents }), domainurl + "Roles/Del_Roles", ip, tid, "Lỗi khi xoá Role", 0, "Role");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = ids, contents }), domainurl + "Roles/Del_Roles", ip, tid, "Lỗi khi xoá Role", 0, "Role");
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
        public async Task<HttpResponseMessage> Update_TrangthaiRoles([FromBody] JObject data)
        {
            List<string> ids = data["ids"].ToObject<List<string>>();
            List<bool> tts = data["tts"].ToObject<List<bool>>();
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
                        var das = await db.sys_roles.Where(a => ids.Contains(a.role_id)).ToListAsync();
                        if (das != null)
                        {
                            List<sys_roles> del = new List<sys_roles>();
                            for (int i = 0; i < das.Count; i++)
                            {
                                var da = das[i];
                                if (ad)
                                {
                                    #region  add Log
                                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = da, contents = "" }), domainurl + "Roles/Update_TrangthaiRoles", ip, tid, "Cập nhật trạng thái Role " + da.role_name, 1, "Role");
                                    #endregion
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
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = ids, contents }), domainurl + "Roles/Update_TrangthaiRoles", ip, tid, "Lỗi khi cập nhật trạng thái Role", 0, "Role");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = ids, contents }), domainurl + "Roles/Update_TrangthaiRoles", ip, tid, "Lỗi khi cập nhật trạng thái Role", 0, "Role");
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
                    string strPath = root + "/Roles";
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
                                List<sys_roles> dvs = new List<sys_roles>();
                                ExcelWorksheet ws = pck.Workbook.Worksheets.First();
                                List<string> cols = new List<string>();
                                var dvcs = db.sys_roles.Select(a => new { a.role_id, a.role_name }).ToList();
                                for (int i = 5; i <= ws.Dimension.End.Row; i++)
                                {
                                    if (ws.Cells[i, 1].Value == null)
                                    {
                                        break;
                                    }
                                    sys_roles dv = new sys_roles();
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
                                            PropertyInfo propertyInfo = db.sys_roles.GetType().GetProperty(column.ToString());
                                            propertyInfo.SetValue(db.sys_roles, Convert.ChangeType(vl,
                                            propertyInfo.PropertyType), null);
                                        }
                                    }
                                    if (dvcs.Count(a => a.role_id == dv.role_id || a.role_name == dv.role_name) > 0)
                                        break;
                                    dvs.Add(dv);
                                }
                                if (dvs.Count > 0)
                                {
                                    db.sys_roles.AddRange(dvs);
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
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { contents }), domainurl + "Roles/ImportExcel", ip, tid, "Lỗi khi import Role", 0, "Role");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { contents }), domainurl + "Roles/ImportExcel", ip, tid, "Lỗi khi import Role", 0, "Role");
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
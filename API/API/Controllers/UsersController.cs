using API.Models;
using Helper;
using Newtonsoft.Json;
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
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace Controllers
{
    [Authorize]
    public class UsersController : ApiController
    {
        string[] trangthais = new string[] { "Khoá", "Kích hoạt", "Đợi xác thực", "Khoá sai Pass quá 5 lần" };
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
        public async Task<HttpResponseMessage> Add_Users()
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
                    string strPath = root + "/Users";
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
                        var md = provider.FormData.GetValues("model").SingleOrDefault();
                        fdmodel = provider.FormData.GetValues("model").SingleOrDefault();
                        sys_users model = JsonConvert.DeserializeObject<sys_users>(fdmodel);
                        if (db.sys_users.Count(a => a.user_id == model.user_id) > 0)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, new { ms = "Đã có tài khoản người dùng này trong hệ thống rồi!", err = "1" });
                        }
                        // This illustrates how to get thefile names.
                        string depass = Codec.EncryptString(model.is_password, helper.passkey);
                        model.is_password = depass;
                        model.key_encript = Convert.ToBase64String(Encoding.UTF8.GetBytes(helper.passkey));
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
                            fileName = helper.convertToUnSign3(fileName);
                            newFileName = Path.Combine(root + "/Users", fileName);
                            fileInfo = new FileInfo(newFileName);
                            if (fileInfo.Exists)
                            {
                                fileName = fileInfo.Name.Replace(fileInfo.Extension, "");
                                fileName = fileName + (new Random().Next(0, 10000)) + fileInfo.Extension;

                                newFileName = Path.Combine(root + "/Users", fileName);
                            }
                            if (!Directory.Exists(fileInfo.Directory.FullName))
                            {
                                Directory.CreateDirectory(fileInfo.Directory.FullName);
                            }
                            model.avatar = "/Portals/Users/" + fileName;
                            ffileData = fileData;
                        }
                      
                        model.token_id = tid;
                        model.ip = ip;
                      
                        model.wrong_pass_count = 0;
                        if (!ad)
                        {
                            model.is_admin = false;
                        }
                        db.sys_users.Add(model);
                        db.SaveChanges();
                        #region  add Log
                        helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdmodel, contents = "" }), domainurl + "Users/Add_Users", ip, tid, "Thêm mới User " + model.user_id, 1, "Users");
                        #endregion
                        File.Move(ffileData.LocalFileName, newFileName);
                        helper.ResizeImage(newFileName, 640, 640, 90);
                        return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                    });
                    return await task;
                }

            }
            catch (DbEntityValidationException e)
            {
                string contents = helper.getCatchError(e, null);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdmodel, contents }), domainurl + "Users/Add_Users", ip, tid, "Lỗi khi thêm người dùng", 0, "Users");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdmodel, contents }), domainurl + "Users/Add_Users", ip, tid, "Lỗi khi thêm người dùng", 0, "Users");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }

        [HttpPut]
        public async Task<HttpResponseMessage> Update_Users()
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
            List<string> ltFiles = new List<string>();
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
                    string strPath = root + "/Users";
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
                        var md = provider.FormData.GetValues("model").SingleOrDefault();
                        fdmodel = provider.FormData.GetValues("model").SingleOrDefault();
                        string noidung = provider.FormData.GetValues("noidung") != null ? provider.FormData.GetValues("noidung").SingleOrDefault() : "";
                        sys_users model = JsonConvert.DeserializeObject<sys_users>(fdmodel);
                        var um = db.sys_users.AsNoTracking().FirstOrDefault(x => x.user_id == model.user_id);
                        string depass = Codec.EncryptString(model.is_password, helper.passkey);
                        if (um?.is_password != model.is_password)
                            model.is_password = depass;
                        #region nội dung thay đổi
                        if (noidung == "")
                        {
                            if (um.full_name != model.full_name)
                            {
                                noidung += "Chỉnh sửa tên tài khoản\n";
                            }
                            if (provider.FileData.Count > 0)
                            {
                                noidung += "Chỉnh sửa ảnh tài khoản\n";
                            }
                            if (um.role_id != model.role_id)
                            {
                                noidung += "Chỉnh sửa nhóm tài khoản\n";
                            }
                            if (um?.is_password != depass)
                            {
                                noidung += "Chỉnh sửa mật khẩu\n";
                            }
                        }
                        #endregion
                        FileInfo fileInfo = null;
                        MultipartFileData ffileData = null;
                        string newFileName = "";
                        // This illustrates how to get thefile names.
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
                            fileName = helper.convertToUnSign3(fileName);
                            newFileName = Path.Combine(root + "/Users", fileName);
                            fileInfo = new FileInfo(newFileName);
                            if (fileInfo.Exists)
                            {
                                fileName = fileInfo.Name.Replace(fileInfo.Extension, "");
                                fileName = fileName + (new Random().Next(0, 10000)) + fileInfo.Extension;

                                newFileName = Path.Combine(root + "/Users", fileName);
                            }
                            if (model.avatar != null)
                                delfiles.Add(root + model.avatar);
                            model.avatar = "/Portals/Users/" + fileName;
                            ffileData = fileData;
                            if (!Directory.Exists(fileInfo.Directory.FullName))
                            {
                                Directory.CreateDirectory(fileInfo.Directory.FullName);
                            }
                            File.Move(fileData.LocalFileName, newFileName);
                            helper.ResizeImage(newFileName, 640, 640, 90);
                            ltFiles.Add(newFileName);
                        }
                   
                        model.token_id = tid;
                        model.ip = ip;
                        db.Entry(model).State = EntityState.Modified;
                        #region  add Log
                        helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdmodel, contents = noidung }), domainurl + "Users/Update_Users", ip, tid, "Cập nhật User " + model.user_id, 1, "Users");
                        #endregion
                        try
                        {
                            db.SaveChanges();
                            //Add ảnh
                            foreach (string fpath in delfiles)
                            {
                                if (File.Exists(fpath))
                                    File.Delete(fpath);
                            }
                            return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                        }
                        catch (DbEntityValidationException e)
                        {
                            foreach (string fpath in ltFiles)
                            {
                                if (File.Exists(fpath))
                                    File.Delete(fpath);
                            }
                            string contents = helper.getCatchError(e, null);
                            helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdmodel, contents }), domainurl + "Users/Update_Users", ip, tid, "Lỗi khi cập nhật người dùng", 0, "Users");
                            if (!helper.debug)
                            {
                                contents = "";
                            }
                            return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                        }
                        catch (Exception e)
                        {
                            foreach (string fpath in ltFiles)
                            {
                                if (File.Exists(fpath))
                                    File.Delete(fpath);
                            }
                            string contents = helper.ExceptionMessage(e);
                            helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdmodel, contents }), domainurl + "Users/Update_Users", ip, tid, "Lỗi khi cập nhật người dùng", 0, "Users");
                            if (!helper.debug)
                            {
                                contents = "";
                            }
                            return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                        }
                    });
                    return await task;
                }

            }
            catch (DbEntityValidationException e)
            {
                string contents = helper.getCatchError(e, null);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdmodel, contents }), domainurl + "Users/Update_Users", ip, tid, "Lỗi khi cập nhật người dùng", 0, "Users");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdmodel, contents }), domainurl + "Users/Update_Users", ip, tid, "Lỗi khi cập nhật người dùng", 0, "Users");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }

        [HttpDelete]
        public async Task<HttpResponseMessage> Del_Users([FromBody] List<string> ids)
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
                        var das = await db.sys_users.Where(a => ids.Contains(a.user_id)).ToListAsync();
                        List<string> paths = new List<string>();
                        if (das != null)
                        {
                            List<sys_users> del = new List<sys_users>();
                            foreach (var da in das)
                            {
                                if ( ad)
                                {
                                    del.Add(da);
                                    #region  add Log
                                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = da, contents = "" }), domainurl + "Users/Del_Users", ip, tid, "Xoá User " + da.user_id, 1, "Users");
                                    #endregion
                                    if (!string.IsNullOrWhiteSpace(da.avatar))
                                        paths.Add(HttpContext.Current.Server.MapPath("~/") + da.avatar);
                                }
                            }
                            if (del.Count == 0)
                            {
                                return Request.CreateResponse(HttpStatusCode.OK, new { err = "1", ms = "Bạn không có quyền xóa người dùng này." });
                            }
                            db.sys_users.RemoveRange(del);
                        }
                        await db.SaveChangesAsync();
                        foreach (string strPath in paths)
                        {
                            bool exists = System.IO.Directory.Exists(strPath);
                            if (exists)
                                System.IO.File.Delete(strPath);
                        }
                        return Request.CreateResponse(HttpStatusCode.OK, new { err = "0" });
                    }
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = ids, contents }), domainurl + "Users/Del_Users", ip, tid, "Lỗi khi xoá người dùng", 0, "Users");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = ids, contents }), domainurl + "Users/Del_Users", ip, tid, "Lỗi khi xoá người dùng", 0, "Users");
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
        public async Task<HttpResponseMessage> Update_TrangthaiUsers([FromBody] List<string> ids, [FromBody] List<int> tts)
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
                        var das = await db.sys_users.Where(a => ids.Contains(a.user_id)).ToListAsync();
                        if (das != null)
                        {
                            List<sys_users> del = new List<sys_users>();
                            for (int i = 0; i < das.Count; i++)
                            {
                                var da = das[i];
                                if ( ad)
                                {
                                    #region  add Log
                                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = da, contents = "" }), domainurl + "Users/Update_TrangthaiUsers", ip, tid, "Cập nhật trạng thái người dùng <b>" + da.user_id + "</b> từ <i>" + trangthais[da.status] + "</i> thành <i>" + trangthais[tts[i]] + "</i>", 1, "Users");
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
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = ids, contents }), domainurl + "Users/Update_TrangthaiUsers", ip, tid, "Lỗi khi cập nhật trạng thái người dùng", 0, "Users");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = ids, contents }), domainurl + "Users/Update_TrangthaiUsers", ip, tid, "Lỗi khi cập nhật trạng thái người dùng", 0, "Users");
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
                    string strPath = root + "/Users";
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
                                List<sys_users> dvs = new List<sys_users>();
                                ExcelWorksheet ws = pck.Workbook.Worksheets.First();
                                List<string> cols = new List<string>();
                                var dvcs = db.sys_users.Select(a => new { a.user_id, a.full_name }).ToList();
                                for (int i = 5; i <= ws.Dimension.End.Row; i++)
                                {
                                    if (ws.Cells[i, 1].Value == null)
                                    {
                                        break;
                                    }
                                    sys_users dv = new sys_users();
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
                                            PropertyInfo propertyInfo = db.sys_users.GetType().GetProperty(column.ToString());
                                            propertyInfo.SetValue(db.sys_users, Convert.ChangeType(vl,
                                            propertyInfo.PropertyType), null);
                                        }
                                    }
                                    if (dvcs.Count(a => a.user_id == dv.user_id || a.full_name == dv.full_name) > 0)
                                        break;
                                    dv.ip = ip;
                             
                                    dv.token_id = tid;
                                    dvs.Add(dv);
                                }
                                if (dvs.Count > 0)
                                {
                                    db.sys_users.AddRange(dvs);
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
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { contents }), domainurl + "Users/ImportExcel", ip, tid, "Lỗi khi import người dùng", 0, "Users");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { contents }), domainurl + "Users/ImportExcel", ip, tid, "Lỗi khi import người dùng", 0, "Users");
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
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using API.Models;
using Helper;
using Newtonsoft.Json;

namespace API.Controllers
{
    public class api_projectController : ApiController
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
        public async Task<HttpResponseMessage> Add_api_project()
        {
            var identity = User.Identity as ClaimsIdentity;
            string fdproject = "";
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
                    string strPath = root + "/Project";
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
                        fdproject = provider.FormData.GetValues("project").SingleOrDefault();
                        api_project project = JsonConvert.DeserializeObject<api_project>(fdproject);
                        HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                        if (project.project_des != null)
                        {
                            doc.LoadHtml(project.project_des);

                            var imgs = doc.DocumentNode.SelectNodes("//img");

                            if (imgs != null)
                            {

                                foreach (var img in imgs)
                                {

                                    var pathFolderDes = "/Portals/Project";
                                    var checkBase64 = img.Attributes["src"].Value.Substring(img.Attributes["src"].Value.LastIndexOf("base64,") + 7);
                                    checkBase64 = checkBase64.Trim();
                                    if ((checkBase64.Length % 4 == 0) && Regex.IsMatch(checkBase64, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None))
                                    {
                                        byte[] bytes = Convert.FromBase64String(img.Attributes["src"].Value.Substring(img.Attributes["src"].Value.LastIndexOf("base64,") + 7));
                                        bool existsFolder = System.IO.Directory.Exists(strPath + pathFolderDes);
                                        if (!existsFolder)
                                        {
                                            System.IO.Directory.CreateDirectory(strPath);
                                        }

                                        var index1 = img.Attributes["src"].Value.LastIndexOf("data:image/") + 11;
                                        var index2 = img.Attributes["src"].Value.IndexOf("base64,");
                                        var typeFileHL = "." + img.Attributes["src"].Value.Substring(index1, index2 - index1 - 1);
                                        var pathShow = "/" + helper.GenKey() + typeFileHL;



                                        using (var imageFile = new FileStream(strPath + pathShow, FileMode.Create))
                                        {
                                            imageFile.Write(bytes, 0, bytes.Length);
                                            imageFile.Flush();
                                        }

                                        project.project_des = project.project_des.Replace(img.Attributes["src"].Value, domainurl + "/Portals/Project" + pathShow);
                                    }
                                }
                            }
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
                            newFileName = Path.Combine(root + "/Project", fileName);
                            fileInfo = new FileInfo(newFileName);
                            if (fileInfo.Exists)
                            {
                                fileName = fileInfo.Name.Replace(fileInfo.Extension, "");
                                fileName = fileName + (new Random().Next(0, 10000)) + fileInfo.Extension;

                                newFileName = Path.Combine(root + "/Project", fileName);
                            }
                            project.project_logo = "/Portals/Project/" + fileName;
                            ffileData = fileData;
                        }
                        if (project.status == null)
                        {
                            project.status = 0;
                        }
                        db.api_project.Add(project);
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
                            log.log_title = "Thêm dự án " + project.project_name;
                            log.log_content = JsonConvert.SerializeObject(new { data = project });
                            log.log_module = "Project";
                            log.id_key = project.project_id.ToString();

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
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdproject, contents }), domainurl + "api_project/Add_project", ip, tid, "Lỗi khi thêm project", 0, "project");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdproject, contents }), domainurl + "api_project/Add_project", ip, tid, "Lỗi khi thêm project", 0, "project  ");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }
        [HttpPut]
        public async Task<HttpResponseMessage> Update_api_project()
        {
            var identity = User.Identity as ClaimsIdentity;
            string fdproject = "";
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
                    string strPath = root + "/Project";
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
                        fdproject = provider.FormData.GetValues("project").SingleOrDefault();
                        api_project project = JsonConvert.DeserializeObject<api_project>(fdproject);
                        HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                        if (project.project_des != null)
                        {
                            doc.LoadHtml(project.project_des);
                            var imgs = doc.DocumentNode.SelectNodes("//img");
                            if (imgs != null)
                            {
                                foreach (var img in imgs)
                                {
                                    var pathFolderDes = "/Portals/Project";
                                    var checkBase64 = img.Attributes["src"].Value.Substring(img.Attributes["src"].Value.LastIndexOf("base64,") + 7);
                                    checkBase64 = checkBase64.Trim();
                                    if ((checkBase64.Length % 4 == 0) && Regex.IsMatch(checkBase64, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None))
                                    {
                                        byte[] bytes = Convert.FromBase64String(img.Attributes["src"].Value.Substring(img.Attributes["src"].Value.LastIndexOf("base64,") + 7));
                                        bool existsFolder = System.IO.Directory.Exists(strPath + pathFolderDes);
                                        if (!existsFolder)
                                        {
                                            System.IO.Directory.CreateDirectory(strPath);
                                        }
                                        var index1 = img.Attributes["src"].Value.LastIndexOf("data:image/") + 11;
                                        var index2 = img.Attributes["src"].Value.IndexOf("base64,");
                                        var typeFileHL = "." + img.Attributes["src"].Value.Substring(index1, index2 - index1 - 1);
                                        var pathShow = "/" + helper.GenKey() + typeFileHL;
                                        using (var imageFile = new FileStream(strPath + pathShow, FileMode.Create))
                                        {
                                            imageFile.Write(bytes, 0, bytes.Length);
                                            imageFile.Flush();
                                        }
                                        project.project_des = project.project_des.Replace(img.Attributes["src"].Value, domainurl + "/Portals/Project" + pathShow);
                                    }
                                }
                            }
                            var projectOld = db.api_project.AsNoTracking().Where(s => s.project_id == project.project_id).FirstOrDefault<api_project>();
                            doc.LoadHtml(projectOld.project_des);
                            imgs = doc.DocumentNode.SelectNodes("//img");
                            if (imgs != null)
                            {
                                foreach (var img in imgs)
                                {
                                    var imgdel = img.Attributes["src"].Value;
                                    imgdel = root + imgdel.Substring(imgdel.IndexOf("Portals/Project") + 7);
                                    if (!project.project_des.Contains(img.Attributes["src"].Value))
                                    {
                                        delfiles.Add(imgdel);
                                    }

                                }
                            }


                            if (projectOld.project_logo != null && projectOld.project_logo != "" && projectOld.project_logo != project.project_logo)
                            {
                                string fileOld = projectOld.project_logo.Substring(8);
                                delfiles.Add(root + fileOld);
                            }
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
                            newFileName = Path.Combine(root + "/Project", fileName);
                            fileInfo = new FileInfo(newFileName);
                            if (fileInfo.Exists)
                            {
                                fileName = fileInfo.Name.Replace(fileInfo.Extension, "");
                                fileName = fileName + (new Random().Next(0, 10000)) + fileInfo.Extension;

                                newFileName = Path.Combine(root + "/Project", fileName);
                            }

                            project.project_logo = "/Portals/Project/" + fileName;
                            ffileData = fileData;
                        }
                       
                        db.Entry(project).State = EntityState.Modified;
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
                            log.log_title = "Sửa dự án " + project.project_name;
                            log.log_content = JsonConvert.SerializeObject(new { data = project });
                            log.log_module = "Project";
                            log.id_key = project.project_id.ToString();

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
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdproject, contents }), domainurl + "api_project/Update_project", ip, tid, "Lỗi khi cập nhật project", 0, "project");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdproject, contents }), domainurl + "api_project/Update_project", ip, tid, "Lỗi khi cập nhật project", 0, "project");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }



        [HttpDelete]
        public async Task<HttpResponseMessage> Delete_api_project([FromBody] List<int> id)
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
                        var das = await db.api_project.Where(a => id.Contains(a.project_id)).ToListAsync();

                        List<string> paths = new List<string>();
                        if (das != null)
                        {
                            List<api_project> del = new List<api_project>();
                            foreach (var da in das)
                            {
                                if (ad)
                                {
                                    var das1 = await db.api_project.Where(a => a.parent_id==da.project_id).ToListAsync();
                                    foreach (var item in das1)
                                    {
                                        del.Add(item);
                                    }
                                    del.Add(da);
                                    if (!string.IsNullOrWhiteSpace(da.project_logo))
                                        paths.Add(HttpContext.Current.Server.MapPath("~/Portals") + da.project_logo.Substring(8));
                                }
                                #region add cms_logs
                                if (helper.wlog)
                                {

                                    cms_logs log = new cms_logs();
                                    log.log_title = "Xóa dự án " + da.project_name;

                                    log.log_module = "Project";
                                    log.id_key = da.project_id.ToString();
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
                            db.api_project.RemoveRange(del);
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
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = id, contents }), domainurl + "api_project/Delete_project", ip, tid, "Lỗi khi xoá dự án", 0, "project");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = id, contents }), domainurl + "api_project/Delete_project", ip, tid, "Lỗi khi xoá dự án", 0, "project");
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
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
    public class api_pluginController : ApiController
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
        public async Task<HttpResponseMessage> Add_plugin()
        {
            var identity = User.Identity as ClaimsIdentity;
            string fdplugin = "";
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
                    string strPath = root + "/Plugin";
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
                        fdplugin = provider.FormData.GetValues("plugin").SingleOrDefault();
                        api_plugin plugin = JsonConvert.DeserializeObject<api_plugin>(fdplugin);
                        HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                        doc.LoadHtml(plugin.des);

                        var imgs = doc.DocumentNode.SelectNodes("//img");

                        if (imgs != null)
                        {

                            foreach (var img in imgs)
                            {

                                var pathFolderDes = "/Portals/Plugin";
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

                                    plugin.des = plugin.des.Replace(img.Attributes["src"].Value, domainurl + "/Portals/Plugin" + pathShow);
                                    helper.ResizeImage(domainurl + "/Portals/Plugin" + pathShow, 640, 640, 90);
                                }
                            }
                        }
                        // This illustrates how to get thefile names.
                        FileInfo fileInfo = null;
                        MultipartFileData ffileData = null;
                        string newFileName = "";
                        string listImages = "";
                        string detached = "";
                        string detached1 = "";
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
                            newFileName = Path.Combine(root + "/Plugin", fileName);
                            fileInfo = new FileInfo(newFileName);
                            if (fileInfo.Exists)
                            {
                                fileName = fileInfo.Name.Replace(fileInfo.Extension, "");
                                fileName = fileName + (new Random().Next(0, 10000)) + fileInfo.Extension;

                                newFileName = Path.Combine(root + "/Plugin", fileName);
                            }
                            if (helper.IsImageFileName(newFileName))
                            {
                                listImages += detached + "/Portals/Plugin/" + fileName;
                                detached = ",";
                                ffileData = fileData;
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
                            }
                            else
                            {
                                plugin.file_url = detached1 + "/Portals/Plugin/" + fileName;
                                detached1 = ",";
                                ffileData = fileData;
                                //Add file zip
                                if (fileInfo != null)
                                {
                                    if (!Directory.Exists(fileInfo.Directory.FullName))
                                    {
                                        Directory.CreateDirectory(fileInfo.Directory.FullName);
                                    }
                                    File.Move(ffileData.LocalFileName, newFileName);

                                }
                            }
                        }
                        plugin.images = listImages;
                        plugin.created_date = DateTime.Now;
                        plugin.created_by = name;
                        plugin.created_ip = ip;
                        plugin.created_token_id = tid;
                        db.api_plugin.Add(plugin);
                        db.SaveChanges();

                        #region add cms_logs
                        if (helper.wlog)
                        {

                            cms_logs log = new cms_logs();
                            log.log_title = "Thêm thư viện " + plugin.plugin_name;
                            log.log_content = JsonConvert.SerializeObject(new { data = plugin });
                            log.log_module = "Thư viện";
                            log.id_key = plugin.plugin_id.ToString();

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
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdplugin, contents }), domainurl + "api_plugin/Add_plugin", ip, tid, "Lỗi khi thêm Plugin", 0, "Plugin");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdplugin, contents }), domainurl + "api_plugin/Add_plugin", ip, tid, "Lỗi khi thêm Plugin", 0, "Plugin  ");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }
        [HttpPut]
        public async Task<HttpResponseMessage> Update_plugin()
        {
            var identity = User.Identity as ClaimsIdentity;
            string fdplugin = "";
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
                    string strPath = root + "/Plugin";
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
                        fdplugin = provider.FormData.GetValues("plugin").SingleOrDefault();
                        api_plugin plugin = JsonConvert.DeserializeObject<api_plugin>(fdplugin);
                        HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                        doc.LoadHtml(plugin.des);
                        var imgs = doc.DocumentNode.SelectNodes("//img");
                        if (imgs != null)
                        {
                            foreach (var img in imgs)
                            {
                                var pathFolderDes = "/Portals/Plugin";
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
                                    plugin.des = plugin.des.Replace(img.Attributes["src"].Value, domainurl + "/Portals/Plugin" + pathShow);
                                }
                            }
                        }
                        var newsOld = db.api_plugin.AsNoTracking().Where(s => s.plugin_id == plugin.plugin_id).FirstOrDefault<api_plugin>();
                        doc.LoadHtml(newsOld.des);
                        imgs = doc.DocumentNode.SelectNodes("//img");
                        if (imgs != null)
                        {
                            foreach (var img in imgs)
                            {
                                var imgdel = img.Attributes["src"].Value;
                                imgdel = root + imgdel.Substring(imgdel.IndexOf("Portals/Plugin") + 7);
                                if (!plugin.des.Contains(img.Attributes["src"].Value))
                                {
                                    delfiles.Add(imgdel);
                                }

                            }
                        }
                        var pluginOld = db.api_plugin.AsNoTracking().Where(s => s.plugin_id == plugin.plugin_id).FirstOrDefault<api_plugin>();
                        if (pluginOld.images != null && pluginOld.images != "")
                        {
                            string[] listImg = pluginOld.images.Split(',');
                            for(int i = 0; i < listImg.Length; i++)
                            {
                                string fileOld = listImg[i].Substring(8);
                                delfiles.Add(root + fileOld);
                            }
                           
                        }
                        if (pluginOld.file_url != null && pluginOld.file_url != "")
                        {
                            string[] listFile = pluginOld.file_url.Split(',');
                            for (int i = 0; i < listFile.Length; i++)
                            {
                                string fileZ = listFile[i].Substring(8);
                                delfiles.Add(root + fileZ);
                            }
                        }
                        // This illustrates how to get thefile names.
                        FileInfo fileInfo = null;
                        MultipartFileData ffileData = null;
                        string newFileName = "";
                        string listImages = "";
                        string detached = "";
                        string detached1 = "";
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
                            newFileName = Path.Combine(root + "/Plugin", fileName);
                            fileInfo = new FileInfo(newFileName);
                            if (fileInfo.Exists)
                            {
                                fileName = fileInfo.Name.Replace(fileInfo.Extension, "");
                                fileName = fileName + (new Random().Next(0, 10000)) + fileInfo.Extension;

                                newFileName = Path.Combine(root + "/Plugin", fileName);
                            }
                            if (helper.IsImageFileName(newFileName))
                            {
                                listImages += detached + "/Portals/Plugin/" + fileName;
                                detached = ",";
                                ffileData = fileData;
                                //Add ảnh
                                if (fileInfo != null)
                                {
                                    if (!Directory.Exists(fileInfo.Directory.FullName))
                                    {
                                        Directory.CreateDirectory(fileInfo.Directory.FullName);
                                    }
                                    File.Move(ffileData.LocalFileName, newFileName);
                                    helper.ResizeImage(newFileName, 640, 640, 90);

                                    foreach (string fpath in delfiles)
                                    {
                                        if (File.Exists(fpath))
                                            File.Delete(fpath);
                                    }

                                }
                                foreach (string fpath in delfiles)
                                {
                                    if (File.Exists(fpath))
                                        File.Delete(fpath);
                                }
                                
                            }
                            else
                            {
                                plugin.file_url = detached1 + "/Portals/Plugin/" + fileName;
                                detached1 = ",";
                                ffileData = fileData;
                                //Add file zip
                                if (fileInfo != null)
                                {
                                    if (!Directory.Exists(fileInfo.Directory.FullName))
                                    {
                                        Directory.CreateDirectory(fileInfo.Directory.FullName);
                                    }
                                    File.Move(ffileData.LocalFileName, newFileName);

                                }
                                foreach (string fpath in delfiles)
                                {
                                    if (File.Exists(fpath))
                                        File.Delete(fpath);
                                }
                            }

                        }
                        plugin.images = listImages;
                        plugin.images = listImages;
                        plugin.created_date = DateTime.Now;
                        plugin.created_by = name;
                        plugin.created_ip = ip;
                        plugin.created_token_id = tid;
                        db.Entry(plugin).State = EntityState.Modified;
                        db.SaveChanges();
                        //Add ảnh
                        //if (fileInfo != null)
                        //{
                        //    foreach (string fpath in delfiles)
                        //    {
                        //        if (File.Exists(fpath))
                        //            File.Delete(fpath);
                        //    }
                        //    if (!Directory.Exists(fileInfo.Directory.FullName))
                        //    {
                        //        Directory.CreateDirectory(fileInfo.Directory.FullName);
                        //    }
                        //    File.Move(ffileData.LocalFileName, newFileName);
                        //    helper.ResizeImage(newFileName, 640, 640, 90);
                        //}
                        #region add cms_logs
                        if (helper.wlog)
                        {

                            cms_logs log = new cms_logs();
                            log.log_title = "Sửa thư viện " + plugin.plugin_name;
                            log.log_content = JsonConvert.SerializeObject(new { data = plugin });
                            log.log_module = "Thư viện";
                            log.id_key = plugin.plugin_id.ToString();

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
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdplugin, contents }), domainurl + "api_plugin/Update_Plugin", ip, tid, "Lỗi khi cập nhật Plugin", 0, "Plugin");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
            catch (Exception e)
            {
                string contents = helper.ExceptionMessage(e);
                helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = fdplugin, contents }), domainurl + "api_plugin/Update_Plugin", ip, tid, "Lỗi khi cập nhật Plugin", 0, "Plugin");
                if (!helper.debug)
                {
                    contents = "";
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
            }
        }



        [HttpDelete]
        public async Task<HttpResponseMessage> Delete_plugin([FromBody] List<int> id)
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
                        var das = await db.api_plugin.Where(a => id.Contains(a.plugin_id)).ToListAsync();
                        List<string> paths = new List<string>();
                        if (das != null)
                        {
                            List<api_plugin> del = new List<api_plugin>();
                            foreach (var da in das)
                            {
                                if (ad)
                                {
                                    del.Add(da);
                                    if (!string.IsNullOrWhiteSpace(da.images))
                                        paths.Add(HttpContext.Current.Server.MapPath("~/Portals") + da.images.Substring(8));
                                }
                                #region add cms_logs
                                if (helper.wlog)
                                {

                                    cms_logs log = new cms_logs();
                                    log.log_title = "Xóa thư viện " + da.plugin_name;

                                    log.log_module = "Thư viện";
                                    log.id_key = da.plugin_id.ToString();
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
                            db.api_plugin.RemoveRange(del);
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
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = id, contents }), domainurl + "api_plugin/Delete_plugin", ip, tid, "Lỗi khi xoá tem", 0, "Plugin");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = id, contents }), domainurl + "api_plugin/Delete_plugin", ip, tid, "Lỗi khi xoá thư viện", 0, "Plugin");
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
        public async Task<HttpResponseMessage> Update_TrangthaiPlugin(Trangthai trangthai)
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
                        var das = db.api_plugin.Where(a => (a.plugin_id == trangthai.IntID)).FirstOrDefault<api_plugin>();
                        if (das != null)
                        {

                            das.status = !trangthai.BitTrangthai;

                            #region add cms_logs
                            if (helper.wlog)
                            {

                                cms_logs log = new cms_logs();
                                log.log_title = "Sửa trạng thái thư viện" + das.plugin_name;

                                log.log_module = "Thư viện";
                                log.id_key = das.plugin_id.ToString();
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
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = trangthai.IntID, contents }), domainurl + "api_plugin/Update_TrangthaiPlugin", ip, tid, "Lỗi khi cập nhật trạng thái Plugins", 0, "api_plugin");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = trangthai.IntID, contents }), domainurl + "api_plugin/Update_TrangthaiPlugin", ip, tid, "Lỗi khi cập nhật trạng thái Plugins", 0, "api_plugin");
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
using API.Models;
using Helper;
using Microsoft.ApplicationBlocks.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace Controllers
{
  
    public class SQLController : ApiController
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
        #region DynamicSQL
        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> FilterSQLOFFSET([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                try
                {
                    int OFFSET = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    sql = @"
                        Select WebAcess_ID,Users_ID,FullName,IsTime,IsEndTime,FromIP,FromDivice from Sys_WebAcess
                    ";
                    string WhereSQL = "";
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " like N'" + m.value + "%'";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " like N'%" + m.value + "'";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " like N'%" + m.value + "%'";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + "  not like N'%" + m.value + "%'";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " = N'" + m.value + "'";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + "  <> N'" + m.value + "'";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }
                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + OFFSET + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterSQLSys_WebAcess", ip, tid, "Lỗi khi gọi FilterSQLSys_WebAcess", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterSQLSys_WebAcess", ip, tid, "Lỗi khi gọi proc FilterSQLSys_WebAcess", 0, "SQL");
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

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> FilterSQLSys_WebAcess([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = " Select count(WebAcess_ID)  as totalRecords from Sys_WebAcess";
                try
                {
                    //int OFFSET = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    sql = @"
                        Select TOP(" + filterSQL.PageSize + @") WebAcess_ID,Users_ID,FullName,IsTime,IsEndTime,FromIP,FromDivice from Sys_WebAcess
                    ";
                    string WhereSQL = "";
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"" + m.value + "*\"') ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "\"') ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " NOT Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }
                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (WebAcess_ID" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1")
                        {
                            WhereSQL = " (WebAcess_ID" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "SearchName like N'%" + filterSQL.Search.ToUpper() + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO;
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterSQLSys_WebAcess", ip, tid, "Lỗi khi gọi FilterSQLSys_WebAcess", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterSQLSys_WebAcess", ip, tid, "Lỗi khi gọi proc FilterSQLSys_WebAcess", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> FilterSQLSys_Logs([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = " Select count(id)  as totalRecords from Sys_Logs";
                try
                {
                    //int OFFSET = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    sql = @"
                        Select TOP(" + filterSQL.PageSize + @") id,title,controller,Users_ID,FullName,logdate,IP,loai,module,Trangthai from Sys_Logs
                    ";
                    string WhereSQL = "";
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"" + m.value + "*\"') ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "\"') ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " NOT Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }
                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1")
                        {
                            WhereSQL = " (id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "SearchName like N'%" + filterSQL.Search.ToUpper() + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO;
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterSQLSys_Logs", ip, tid, "Lỗi khi gọi FilterSQLSys_Logs", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterSQLSys_Logs", ip, tid, "Lỗi khi gọi proc FilterSQLSys_Logs", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> FilterSQLTest_Case([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = " Select count(Case_ID)  as totalRecords from Test_Case w";
                try
                {
                    //int OFFSET = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    sql = @"
                        Select TOP(" + filterSQL.PageSize + @") Case_ID,Case_Name,w.Trangthai,NgayTest,IsPass,IsPassStep,SoTest,SoTestPass,FullName,u.Users_ID,w.IPtao from Test_Case w inner join Sys_Users u on w.Nguoitao=u.Users_ID
                    ";
                    string WhereSQL = "";
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"" + m.value + "*\"') ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "\"') ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " NOT Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }
                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (Case_ID" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1")
                        {
                            WhereSQL = " (Case_ID" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "Case_Name like N'%" + filterSQL.Search + "%'  or " + "Tukhoa like N'%" + filterSQL.Search + "%'";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO;
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterSQLTest_Case", ip, tid, "Lỗi khi gọi FilterSQLTest_Case", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterSQLTest_Case", ip, tid, "Lỗi khi gọi proc FilterSQLTest_Case", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }


        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> FilterSQLSQL_Log([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = " Select count(id)  as totalRecords from SQL_Log";
                try
                {
                    //int OFFSET = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    sql = @"
                        Select TOP(" + filterSQL.PageSize + @") id,title,controller,Users_ID,FullName,StartDate,IP,SoMiliGiay from SQL_Log
                    ";
                    string WhereSQL = "";
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"" + m.value + "*\"') ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "\"') ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " NOT Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }
                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1")
                        {
                            WhereSQL = " (id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "SearchName like N'%" + filterSQL.Search.ToUpper() + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO;
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterSQLSys_Logs", ip, tid, "Lỗi khi gọi FilterSQLSys_Logs", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterSQLSys_Logs", ip, tid, "Lỗi khi gọi proc FilterSQLSys_Logs", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }
        #endregion

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> FilterSQL_CMS_Lang([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = " Select count(id)  as totalRecords from CMS_Lang";
                try
                {
                    int Start = (filterSQL.PageNo * filterSQL.PageSize-filterSQL.PageSize);
                    sql = @"
                        Select cl.*  from CMS_Lang as cl
                    ";
                    string WhereSQL = "";
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                             {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator+ " " + field.key+ " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "\"') ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " NOT Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1")
                        {
                            WhereSQL = " (id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "SearchName like N'%" + filterSQL.Search.ToUpper() + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterSQLSys_Logs", ip, tid, "Lỗi khi gọi FilterSQLSys_Logs", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterSQLSys_Logs", ip, tid, "Lỗi khi gọi proc FilterSQLSys_Logs", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> FilterCMS_Logs([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = " Select count(Log_Id)  as totalRecords from CMS_Logs";
                try
                {
                    int Start = (filterSQL.PageNo * filterSQL.PageSize - filterSQL.PageSize);
                    sql = @"
                        Select cl.*  from CMS_Logs as cl
                    ";
                    string WhereSQL = "";
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "\"') ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " NOT Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (Log_Id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1")
                        {
                            WhereSQL = " (Log_ID" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "LogTitle like N'%" + filterSQL.Search.ToUpper() + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterCMS_Logs", ip, tid, "Lỗi khi gọi FilterCMS_Logs", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterCMS_Logs", ip, tid, "Lỗi khi gọi proc FilterCMS_Logs", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> FilterCMS_News([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id != null) {
                     sqlCount = " Select count(News_ID)  as totalRecords from CMS_News Where Menu_ID=" + filterSQL.id;
                }
                else
                {
                     sqlCount = " Select count(News_ID)  as totalRecords from CMS_News";

                }
                try
                {
                    int Start = (filterSQL.PageNo * filterSQL.PageSize - filterSQL.PageSize);
                    sql = @"
                        Select cn.*  from CMS_News as cn
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "Menu_ID=" + filterSQL.id;
                    }
                        if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "\"') ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " NOT Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    //if (filterSQL.next)//Trang tiếp
                    //{
                    //    if (filterSQL.id != null)
                    //    {
                    //        WhereSQL = " (News_ID" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                    //    }
                    //}
                    //else//Trang trước
                    //{
                    //    if (filterSQL.id != "-1")
                    //    {
                    //        WhereSQL = " (News_ID" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                    //    }
                    //}
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "News_Name like N'%" + filterSQL.Search.ToUpper() + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL+ "And IsLang=(SELECT ID FROM CMS_Lang cl1 WHERE cl1.IsMain=1)";
                        sqlCount += " WHERE " + WhereSQL + "And IsLang=(SELECT ID FROM CMS_Lang cl1 WHERE cl1.IsMain=1)";
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterCMS_News", ip, tid, "Lỗi khi gọi FilterCMS_News", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterCMS_News", ip, tid, "Lỗi khi gọi proc FilterCMS_News", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> FilterCMS_House([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id != null)
                {
                    sqlCount = " Select count(House_ID)  as totalRecords from CMS_House Where Menu_ID=" + filterSQL.id;
                }
                else
                {
                    sqlCount = " Select count(House_ID)  as totalRecords from CMS_House";

                }
                try
                {
                    int Start = (filterSQL.PageNo * filterSQL.PageSize - filterSQL.PageSize);
                    sql = @"
                        Select ch.*  from CMS_House as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "Menu_ID=" + filterSQL.id;
                    }
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "\"') ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " NOT Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    //if (filterSQL.next)//Trang tiếp
                    //{
                    //    if (filterSQL.id != null)
                    //    {
                    //        WhereSQL = " (News_ID" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                    //    }
                    //}
                    //else//Trang trước
                    //{
                    //    if (filterSQL.id != "-1")
                    //    {
                    //        WhereSQL = " (News_ID" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                    //    }
                    //}
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "Name like N'%" + filterSQL.Search.ToUpper() + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL + "And IsLang=(SELECT ID FROM CMS_Lang cl1 WHERE cl1.IsMain=1)";
                        sqlCount += " WHERE " + WhereSQL + "And IsLang=(SELECT ID FROM CMS_Lang cl1 WHERE cl1.IsMain=1)";
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterCMS_House", ip, tid, "Lỗi khi gọi FilterCMS_House", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/FilterCMS_News", ip, tid, "Lỗi khi gọi proc FilterCMS_News", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Places([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id != null)
                {
                    sqlCount = " Select count(place_id)  as totalRecords from ca_places Where place_id=" + filterSQL.id;
                }
                else
                {
                    sqlCount = " Select count(place_id)  as totalRecords from ca_places";

                }
                try
                {
                    int Start = (filterSQL.PageNo * filterSQL.PageSize - filterSQL.PageSize);
                    sql = @"
                        Select ch.*  from ca_places as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "place_id=" + filterSQL.id;
                    }
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "\"') ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " NOT Contains(" + field.key + ",'\"*" + m.value + "*\"') ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    //if (filterSQL.next)//Trang tiếp
                    //{
                    //    if (filterSQL.id != null)
                    //    {
                    //        WhereSQL = " (News_ID" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                    //    }
                    //}
                    //else//Trang trước
                    //{
                    //    if (filterSQL.id != "-1")
                    //    {
                    //        WhereSQL = " (News_ID" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                    //    }
                    //}
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "name like N'%" + filterSQL.Search.ToUpper() + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Places", ip, tid, "Lỗi khi gọi Filter_Places", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Places", ip, tid, "Lỗi khi gọi proc Filter_Places", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Email_Groups([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id != null)
                {
                    sqlCount = " Select count(email_group_id)  as totalRecords from doc_ca_email_groups Where email_group_id=" + filterSQL.id;
                }
                else
                {
                    sqlCount = " Select count(email_group_id)  as totalRecords from doc_ca_email_groups";

                }
                try
                {
                    int Start = 0;
                    if (filterSQL.PageNo > 0)
                    {
                         Start = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    }
                    sql = @"
                        Select *,(SELECT count(dce.email_group_id) FROM doc_ca_emails AS dce where dce.email_group_id=ch.email_group_id) email_count  from doc_ca_email_groups as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "email_group_id=" + filterSQL.id;
                    }
                     if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "' ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "%' ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " NOT LIKE '%" + m.value + "%' ";    
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (email_group_id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1")
                        {
                            WhereSQL = " (email_group_id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "email_group_name like N'%" + filterSQL.Search + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Email_Groups", ip, tid, "Lỗi khi gọi Filter_Email_Groups", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Email_Groups", ip, tid, "Lỗi khi gọi proc Filter_Email_Groups", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Email([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id != null)
                {
                    sqlCount = " Select count(email_id)  as totalRecords from doc_ca_emails Where email_id=" + filterSQL.id;
                }
                else
                {
                    sqlCount = " Select count(email_id)  as totalRecords from doc_ca_emails";

                }
                try
                {
                    int Start = 0;
                    if (filterSQL.PageNo > 0)
                    {
                        Start = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    }
                    sql = @"
                        Select * from doc_ca_emails as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "email_id=" + filterSQL.id;
                    }
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "' ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "%' ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " NOT LIKE '%" + m.value + "%' ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (email_id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1")
                        {
                            WhereSQL = " (email_id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "email_name like N'%" + filterSQL.Search + "%'"+ "or full_name like N'%" +filterSQL.Search + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Email", ip, tid, "Lỗi khi gọi Filter_Email", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Email", ip, tid, "Lỗi khi gọi proc Filter_Email", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Dispatch([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id != null)
                {
                    sqlCount = " Select count(dispatch_book_id)  as totalRecords from doc_ca_dispatch_books Where dispatch_book_id=" + filterSQL.id;
                }
                else
                {
                    sqlCount = " Select count(dispatch_book_id)  as totalRecords from doc_ca_dispatch_books";

                }
                try
                {
                    int  Start = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    
                    sql = @"
                        Select * from doc_ca_dispatch_books as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "dispatch_book_id=" + filterSQL.id;
                    }
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "' ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "%' ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " NOT LIKE '%" + m.value + "%' ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (dispatch_book_id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1" && filterSQL.id != null && filterSQL.id != null)
                        {
                            WhereSQL = " (dispatch_book_id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "dispatch_book_name like N'%" + filterSQL.Search + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Dispatch", ip, tid, "Lỗi khi gọi Filter_Dispatch", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Dispatch", ip, tid, "Lỗi khi gọi proc Filter_Dispatch", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Field([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id == null)
                {
                    sqlCount = " Select count(field_id)  as totalRecords from doc_ca_fields";
                }
           
                try
                {
                    int Start = 0;
                    if (filterSQL.PageNo > 0)
                    {
                        Start = (filterSQL.PageNo) * filterSQL.PageSize;
                    }
                    sql = @"
                        Select * from doc_ca_fields as ch
                    ";
                    string WhereSQL = "";
         
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "' ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "%' ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " NOT LIKE '%" + m.value + "%' ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (field_id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1" && filterSQL.id != null)
                        {
                            WhereSQL = " (field_id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "field_name like N'%" + filterSQL.Search + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Field", ip, tid, "Lỗi khi gọi Filter_Field", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Field", ip, tid, "Lỗi khi gọi proc Filter_Field", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Groups([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id == null)
                {
                    sqlCount = " Select count(doc_group_id)  as totalRecords from doc_ca_groups";
                }

                try
                {
                    int Start = 0;
                    if (filterSQL.PageNo > 0)
                    {
                        Start = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    }
                    sql = @"
                        Select * from doc_ca_groups as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "doc_group_id=" + filterSQL.id;
                    }
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "' ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "%' ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " NOT LIKE '%" + m.value + "%' ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (doc_group_id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1" && filterSQL.id != null)
                        {
                            WhereSQL = " (doc_group_id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "doc_group_name like N'%" + filterSQL.Search + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Groups", ip, tid, "Lỗi khi gọi Filter_Groups", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Groups", ip, tid, "Lỗi khi gọi proc Filter_Groups", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Issue_Places([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id == null)
                {
                    sqlCount = " Select count(issue_place_id)  as totalRecords from doc_ca_issue_places";
                }

                try
                {
                    int Start = 0;
                    if (filterSQL.PageNo > 0)
                    {
                        Start = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    }
                    sql = @"
                        Select * from doc_ca_issue_places as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "issue_place_id=" + filterSQL.id;
                    }
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "' ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "%' ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " NOT LIKE '%" + m.value + "%' ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (issue_place_id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1" && filterSQL.id != null)
                        {
                            WhereSQL = " (issue_place_id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "issue_place_name like N'%" + filterSQL.Search + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Issue_Places", ip, tid, "Lỗi khi gọi Filter_Issue_Places", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Issue_Places", ip, tid, "Lỗi khi gọi proc Filter_Issue_Places", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Receive([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id == null)
                {
                    sqlCount = " Select count(receive_place_id)  as totalRecords from doc_ca_receive_places";
                }

                try
                {
                    int Start = 0;
                    if (filterSQL.PageNo > 0)
                    {
                        Start = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    }
                    sql = @"
                        Select * from doc_ca_receive_places as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "receive_place_id=" + filterSQL.id;
                    }
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "' ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "%' ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " NOT LIKE '%" + m.value + "%' ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (receive_place_id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1" && filterSQL.id != null)
                        {
                            WhereSQL = " (receive_place_id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "receive_place_name like N'%" + filterSQL.Search + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Receive", ip, tid, "Lỗi khi gọi Filter_Receive", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Receive", ip, tid, "Lỗi khi gọi proc Filter_Receive", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Send_ways([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id == null)
                {
                    sqlCount = " Select count(send_way_id)  as totalRecords from doc_ca_send_ways";
                }

                try
                {
                    int Start = 0;
                    if (filterSQL.PageNo > 0)
                    {
                        Start = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    }
                    sql = @"
                        Select * from doc_ca_send_ways as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "send_way_id=" + filterSQL.id;
                    }
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "' ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "%' ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " NOT LIKE '%" + m.value + "%' ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (send_way_id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1" && filterSQL.id != null)
                        {
                            WhereSQL = " (send_way_id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "send_way_name like N'%" + filterSQL.Search + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Send_ways", ip, tid, "Lỗi khi gọi Filter_Send_ways", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Send_ways", ip, tid, "Lỗi khi gọi proc Filter_Send_ways", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Security([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id == null)
                {
                    sqlCount = " Select count(security_id)  as totalRecords from doc_ca_security";
                }

                try
                {
                    int Start = 0;
                    if (filterSQL.PageNo > 0)
                    {
                        Start = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    }
                    sql = @"
                        Select * from doc_ca_security as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "security_id=" + filterSQL.id;
                    }
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "' ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "%' ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " NOT LIKE '%" + m.value + "%' ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (security_id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1" && filterSQL.id != null)
                        {
                            WhereSQL = " (security_id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "security_name like N'%" + filterSQL.Search + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Security", ip, tid, "Lỗi khi gọi Filter_Security", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Security", ip, tid, "Lỗi khi gọi proc Filter_Security", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Signer([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id == null)
                {
                    sqlCount = " Select count(signer_id)  as totalRecords from doc_ca_signers";
                }

                try
                {
                    int Start = 0;
                    if (filterSQL.PageNo > 0)
                    {
                        Start = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    }
                    sql = @"
                        Select * from doc_ca_signers as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "signer_id=" + filterSQL.id;
                    }
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "' ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "%' ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " NOT LIKE '%" + m.value + "%' ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (signer_id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1" && filterSQL.id != null)
                        {
                            WhereSQL = " (signer_id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "signer_name like N'%" + filterSQL.Search + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Signer", ip, tid, "Lỗi khi gọi Filter_Signer", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Signer", ip, tid, "Lỗi khi gọi proc Filter_Signer", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Urgency([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id == null)
                {
                    sqlCount = " Select count(urgency_id)  as totalRecords from doc_ca_urgency";
                }

                try
                {
                    int Start = 0;
                    if (filterSQL.PageNo > 0)
                    {
                        Start = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    }
                    sql = @"
                        Select * from doc_ca_urgency as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "urgency_id=" + filterSQL.id;
                    }
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "' ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "%' ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " NOT LIKE '%" + m.value + "%' ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (urgency_id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1" && filterSQL.id != null)
                        {
                            WhereSQL = " (urgency_id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "urgency_name like N'%" + filterSQL.Search + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Urgency", ip, tid, "Lỗi khi gọi Filter_Urgency", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Urgency", ip, tid, "Lỗi khi gọi proc Filter_Urgency", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Stamp([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id == null)
                {
                    sqlCount = " Select count(stamp_id)  as totalRecords from doc_ca_stamps";
                }

                try
                {
                    int Start = 0;
                    if (filterSQL.PageNo > 0)
                    {
                        Start = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    }
                    sql = @"
                        Select * from doc_ca_stamps as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "stamp_id=" + filterSQL.id;
                    }
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "' ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "%' ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " NOT LIKE '%" + m.value + "%' ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (stamp_id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1" && filterSQL.id != null)
                        {
                            WhereSQL = " (stamp_id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "stamp_name like N'%" + filterSQL.Search + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Stamp", ip, tid, "Lỗi khi gọi Filter_Stamp", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Stamp", ip, tid, "Lỗi khi gọi proc Filter_Stamp", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Status([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id == null)
                {
                    sqlCount = " Select count(status_id)  as totalRecords from doc_ca_status";
                }

                try
                {
                    int Start = 0;
                    if (filterSQL.PageNo > 0)
                    {
                        Start = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    }
                    sql = @"
                        Select * from doc_ca_status as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "status_id=" + filterSQL.id;
                    }
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "' ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "%' ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " NOT LIKE '%" + m.value + "%' ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (status_id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1" && filterSQL.id != null)
                        {
                            WhereSQL = " (status_id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "status_name like N'%" + filterSQL.Search + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Status", ip, tid, "Lỗi khi gọi Filter_Status", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Status", ip, tid, "Lỗi khi gọi proc Filter_Status", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Tag([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id == null)
                {
                    sqlCount = " Select count(tag_id)  as totalRecords from doc_ca_tags";
                }

                try
                {
                    int Start = 0;
                    if (filterSQL.PageNo > 0)
                    {
                        Start = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    }
                    sql = @"
                        Select * from doc_ca_tags as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "tag_id=" + filterSQL.id;
                    }
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "' ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "%' ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " NOT LIKE '%" + m.value + "%' ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (tag_id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1" && filterSQL.id != null)
                        {
                            WhereSQL = " (tag_id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "tag_name like N'%" + filterSQL.Search + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Tag", ip, tid, "Lỗi khi gọi Filter_Tag", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Tag", ip, tid, "Lỗi khi gọi proc Filter_Tag", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Type([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id == null)
                {
                    sqlCount = " Select count(doc_type_id)  as totalRecords from doc_ca_types";
                }

                try
                {
                    int Start = 0;
                    if (filterSQL.PageNo > 0)
                    {
                        Start = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    }
                    sql = @"
                        Select * from doc_ca_types as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "doc_type_id=" + filterSQL.id;
                    }
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "' ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "%' ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " NOT LIKE '%" + m.value + "%' ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (doc_type_id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1" && filterSQL.id != null)
                        {
                            WhereSQL = " (doc_type_id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "doc_type_name like N'%" + filterSQL.Search + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Type", ip, tid, "Lỗi khi gọi Filter_Type", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Type", ip, tid, "Lỗi khi gọi proc Filter_Type", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Doc_Positions([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id == null)
                {
                    sqlCount = " Select count(position_id)  as totalRecords from doc_ca_positions";
                }

                try
                {
                    int Start = 0;
                    if (filterSQL.PageNo > 0)
                    {
                        Start = (filterSQL.PageNo-1) * filterSQL.PageSize;
                    }
                    sql = @"
                        Select * from doc_ca_positions as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "position_id=" + filterSQL.id;
                    }
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "' ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "%' ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " NOT LIKE '%" + m.value + "%' ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (position_id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1" && filterSQL.id != null)
                        {
                            WhereSQL = " (position_id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "position_name like N'%" + filterSQL.Search + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Doc_Positions", ip, tid, "Lỗi khi gọi Filter_Doc_Positions", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Doc_Positions", ip, tid, "Lỗi khi gọi proc Filter_Doc_Positions", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<HttpResponseMessage> Filter_Plugin([FromBody] FilterSQL filterSQL)
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
                string Connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                string sql = "";
                string sqlCount = "";
                if (filterSQL.id == null)
                {
                    sqlCount = " Select count(plugin_id)  as totalRecords from api_plugin";
                }

                try
                {
                    int Start = 0;
                    if (filterSQL.PageNo > 0)
                    {
                        Start = (filterSQL.PageNo - 1) * filterSQL.PageSize;
                    }
                    sql = @"
                        Select * from api_plugin as ch
                    ";
                    string WhereSQL = "";
                    if (filterSQL.id != null)
                    {
                        WhereSQL = "plugin_id=" + filterSQL.id;
                    }
                    if (filterSQL.fieldSQLS != null && filterSQL.fieldSQLS.Count > 0)
                    {
                        foreach (var field in filterSQL.fieldSQLS)
                        {
                            if (field.filteroperator == "in")
                            {
                                WhereSQL += (WhereSQL != "" ? " And " : " ") + field.key + " in(" + String.Join(",", field.filterconstraints.Select(a => "'" + a.value + "'").ToList()) + ")";
                            }
                            else
                            {
                                foreach (var m in field.filterconstraints.Where(a => a.value != null))
                                {
                                    switch (m.matchMode)
                                    {
                                        case "lt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " < " + m.value + ")";
                                            break;
                                        case "gt":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >" + m.value + ")";
                                            break;
                                        case "lte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " <= " + m.value + ")";
                                            break;
                                        case "gte":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " >= " + m.value + ")";
                                            break;
                                        case "startsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '" + m.value + "%' ";
                                            break;
                                        case "endsWith":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "' ";
                                            break;
                                        case "contains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " LIKE '%" + m.value + "%' ";
                                            break;
                                        case "notContains":
                                            WhereSQL += " " + field.filteroperator + " " + field.key + " NOT LIKE '%" + m.value + "%' ";
                                            break;
                                        case "equals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + " = N'" + m.value + "')";
                                            break;
                                        case "notEquals":
                                            WhereSQL += " " + field.filteroperator + " (" + field.key + "  <> N'" + m.value + "')";
                                            break;
                                        case "dateIs":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) = CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateIsNot":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) <> CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateBefore":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) < CAST('" + m.value + "' as date)";
                                            break;
                                        case "dateAfter":
                                            WhereSQL += " " + field.filteroperator + " CAST(" + field.key + " as date) > CAST('" + m.value + "' as date)";
                                            break;

                                    }
                                }
                            }
                        }
                    }

                    if (WhereSQL.StartsWith(" and "))
                    {
                        WhereSQL = WhereSQL.Substring(4);
                    }
                    else if (WhereSQL.StartsWith(" or "))
                    {
                        WhereSQL = WhereSQL.Substring(3);
                    }

                    if (filterSQL.next)//Trang tiếp
                    {
                        if (filterSQL.id != null)
                        {
                            WhereSQL = " (plugin_id" + (filterSQL.sqlO.Contains("DESC") ? "<" : ">") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    else//Trang trước
                    {
                        if (filterSQL.id != "-1" && filterSQL.id != null)
                        {
                            WhereSQL = " (plugin_id" + (filterSQL.sqlO.Contains("DESC") ? ">" : "<") + filterSQL.id + ") " + (WhereSQL.Trim() != "" ? " And " + WhereSQL : "");
                        }
                    }
                    //Search
                    if (!string.IsNullOrWhiteSpace(filterSQL.Search))
                    {
                        WhereSQL = (WhereSQL.Trim() != "" ? (WhereSQL + " And  ") : "") + "plugin_name like N'%" + filterSQL.Search + "%' collate Latin1_General_100_Bin2";
                    }
                    if (WhereSQL.Trim() != "")
                    {
                        sql += " WHERE " + WhereSQL;
                        sqlCount += " WHERE " + WhereSQL;
                    }
                    if (!filterSQL.next)//Đảo Sort
                    {
                        sql = "Select * from (" + sql + " ORDER BY " + (filterSQL.sqlO.Contains("DESC") ? filterSQL.sqlO.Replace("DESC", "ASC") : filterSQL.sqlO.Replace("ASC", "DESC")) + ") as tbn ";
                    }
                    sql += @"
                        ORDER BY " + filterSQL.sqlO + @"
                        OFFSET " + Start + " ROWS FETCH NEXT " + filterSQL.PageSize + " ROWS ONLY";
                    if (filterSQL.id == null)
                    {
                        sql += sqlCount;
                    }
                    sql = sql.Replace("\r", " ").Replace("\n", " ").Replace("   ", " ").Trim();
                    sql = Regex.Replace(sql, @"\s+", " ").Trim();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, System.Data.CommandType.Text, sql).Tables);
                    var tables = await task;
                    string JSONresult = JsonConvert.SerializeObject(tables);
                    return Request.CreateResponse(HttpStatusCode.OK, new { data = JSONresult, sql, err = "0" });
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Stamp", ip, tid, "Lỗi khi gọi Filter_Stamp", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = sql, contents }), domainurl + "/SQL/Filter_Stamp", ip, tid, "Lỗi khi gọi proc Filter_Stamp", 0, "SQL");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1", sql });
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

        }
    }
}
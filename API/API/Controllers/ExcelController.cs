using API.Models;
using Helper;
using Microsoft.ApplicationBlocks.Data;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace Controllers
{
  
    [Authorize]
    public class ExcelController : ApiController
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
        public async Task<HttpResponseMessage> ExportExcel([FromBody] sqlProc proc)
        {
            using (DBEntities db = new DBEntities())
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
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                bool ad = claims.Where(p => p.Type == "ad").FirstOrDefault()?.Value == "True";
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                try
                {
                    string Connection = db.Database.Connection.ConnectionString;
                    using (ExcelPackage exportPackge = new ExcelPackage())
                    {
                        String FileName = String.Empty;
                        FileName = proc.excelname + "_" + DateTime.Now.ToString("ddMMyyyy");
                        ExcelWorksheet exWorkSheet = exportPackge.Workbook.Worksheets.Add(FileName);
                        #region tbody
                        var sqlpas = new List<SqlParameter>();
                        if (proc != null && proc.par != null)
                        {
                            foreach (sqlPar p in proc.par)
                            {
                                sqlpas.Add(new SqlParameter("@" + p.par, p.va));
                            }
                        }
                        var arrpas = sqlpas.ToArray();
                        var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, proc.proc, arrpas).Tables[0]);
                        var table = await task;
                        #region thead
                        var len = table.Columns.Count;
                        int start = 3;
                        ExcelRange topcell1 = exWorkSheet.Cells[1, 1, 1, len];
                        exWorkSheet.Row(1).Height = 50;
                        exWorkSheet.Row(2).Height = 20;
                        exWorkSheet.Row(3).Height = 25;
                        exWorkSheet.Row(4).Height = 25;
                        topcell1.Style.Font.Bold = true;
                        topcell1.Style.Font.Size = 16;
                        topcell1.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        topcell1.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        topcell1.Merge = true;
                        topcell1.Style.WrapText = true;
                        ExcelRange topcell2 = exWorkSheet.Cells[2, 1, 2, len];
                        topcell2.Value = DateTime.Now.ToString("HH:mm dd/MM/yyyy");
                        topcell2.Style.Font.Italic = true;
                        topcell2.Style.Font.Bold = false;
                        topcell2.Style.Font.Size = 12;
                        topcell2.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        topcell2.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        topcell2.Merge = true;
                        topcell2.Style.WrapText = true;
                        for (int i = 1; i <= len; i++)
                        {
                            var col = table.Columns[i - 1];
                            ExcelRange cell = exWorkSheet.Cells[start, i];
                            cell.Value = col.ColumnName.Split('|')[0];
                            exWorkSheet.Row(start).Hidden = true;
                            exWorkSheet.Row(start).Style.Font.Color.SetColor(Color.Transparent);
                        }
                        start++;
                        for (int i = 1; i <= len; i++)
                        {
                            var col = table.Columns[i - 1];
                            ExcelRange cell = exWorkSheet.Cells[start, i];
                            cell.Value = col.ColumnName.Split('|')[1];
                            cell.Style.Indent = 1;
                            cell.Style.Font.Bold = true;
                            exWorkSheet.Column(i).Style.Font.Size = 12;
                            exWorkSheet.Column(i).Style.WrapText = true;
                            exWorkSheet.Column(i).Width = int.Parse(col.ColumnName.Split('|')[2]);
                            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                            cell.Style.HorizontalAlignment = col.ColumnName.Split('|')[3] == "C" ? ExcelHorizontalAlignment.Center : ExcelHorizontalAlignment.Left;
                            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }
                        exWorkSheet.Cells[start, 1, start, len].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        exWorkSheet.Cells[start, 1, start, len].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#0078d4"));
                        exWorkSheet.Cells[start, 1, start, len].Style.Font.Color.SetColor(Color.White);
                        #endregion
                        topcell1.Value = string.Format("DANH SÁCH " + proc.excelname + " ({0})", table.Rows.Count);
                        for (int k = 0; k < table.Rows.Count; k++)
                        {
                            start += 1;
                            var dr = table.Rows[k];
                            for (int i = 1; i <= len; i++)
                            {
                                var col = table.Columns[i - 1];
                                ExcelRange cell = exWorkSheet.Cells[start, i];
                                try
                                {
                                    cell.Value = dr[col.ColumnName].ToString();
                                    cell.Style.Indent = 1;
                                    cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                                    cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                                    cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                                    cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                                    cell.Style.HorizontalAlignment = col.ColumnName.Split('|')[3] == "C" ? ExcelHorizontalAlignment.Center : ExcelHorizontalAlignment.Left;
                                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                                }
                                catch (Exception ) { }
                            }
                        }
                        #endregion
                        Byte[] fileBytes = exportPackge.GetAsByteArray();
                        string fname = helper.convertToUnSign3(proc.excelname + uid);
                        string path = HttpContext.Current.Server.MapPath("~/Portals/Export/" + fname + ".xlsx");
                        File.WriteAllBytes(path, fileBytes);
                        return Request.CreateResponse(HttpStatusCode.OK, new { path = "/Portals/Export/" + fname + ".xlsx", err = "0" });
                    }
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = proc, contents }), domainurl + "Excel/ExportExcel", ip, tid, "Lỗi khi kết xuất file " + proc.excelname, 0, "Excel");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = proc, contents }), domainurl + "Excel/ExportExcel", ip, tid, "Lỗi khi kết xuất file " + proc.excelname, 0, "Excel");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
            }
        }
        [HttpPost]
        public async Task<HttpResponseMessage> ExportExcelTable([FromBody] sqlProc proc)
        {
            using (DBEntities db = new DBEntities())
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
                string ip = getipaddress();
                string name = claims.Where(p => p.Type == "fname").FirstOrDefault()?.Value;
                string tid = claims.Where(p => p.Type == "tid").FirstOrDefault()?.Value;
                string uid = claims.Where(p => p.Type == "uid").FirstOrDefault()?.Value;
                bool ad = claims.Where(p => p.Type == "ad").FirstOrDefault()?.Value == "True";
                string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                try
                {
                    string Connection = db.Database.Connection.ConnectionString;
                    using (ExcelPackage exportPackge = new ExcelPackage())
                    {
                        String FileName = String.Empty;
                        FileName = proc.excelname + "_" + DateTime.Now.ToString("ddMMyyyy");
                        ExcelWorksheet exWorkSheet = exportPackge.Workbook.Worksheets.Add(FileName);
                        #region tbody
                        var sqlpas = new List<SqlParameter>();
                        if (proc != null && proc.par != null)
                        {
                            foreach (sqlPar p in proc.par)
                            {
                                sqlpas.Add(new SqlParameter("@" + p.par, p.va));
                            }
                        }
                        var arrpas = sqlpas.ToArray();
                        var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, proc.proc, arrpas).Tables[0]);
                        var table = await task;
                        #region thead
                        var len = table.Columns.Count;
                        int start = 3;
                        ExcelRange topcell1 = exWorkSheet.Cells[1, 1, 1, len];
                        exWorkSheet.Row(1).Height = 50;
                        exWorkSheet.Row(2).Height = 20;
                        exWorkSheet.Row(3).Height = 25;
                        exWorkSheet.Row(4).Height = 25;
                        topcell1.Style.Font.Bold = true;
                        topcell1.Style.Font.Size = 16;
                        topcell1.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        topcell1.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        topcell1.Merge = true;
                        topcell1.Style.WrapText = true;
                        ExcelRange topcell2 = exWorkSheet.Cells[2, 1, 2, len];
                        topcell2.Value = DateTime.Now.ToString("HH:mm dd/MM/yyyy");
                        topcell2.Style.Font.Italic = true;
                        topcell2.Style.Font.Bold = false;
                        topcell2.Style.Font.Size = 12;
                        topcell2.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        topcell2.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        topcell2.Merge = true;
                        topcell2.Style.WrapText = true;
                        for (int i = 1; i <= len; i++)
                        {
                            var col = table.Columns[i - 1];
                            ExcelRange cell = exWorkSheet.Cells[start, i];
                            cell.Value = col.ColumnName.Split('|')[0];
                            exWorkSheet.Row(start).Hidden = true;
                            exWorkSheet.Row(start).Style.Font.Color.SetColor(Color.Transparent);
                        }
                        start++;
                        for (int i = 1; i <= len; i++)
                        {
                            var col = table.Columns[i - 1];
                            ExcelRange cell = exWorkSheet.Cells[start, i];
                            cell.Value = col.ColumnName.Split('|')[1];
                            cell.Style.Indent = 1;
                            cell.Style.Font.Bold = true;
                            exWorkSheet.Column(i).Style.Font.Size = 12;
                            exWorkSheet.Column(i).Style.WrapText = true;
                            exWorkSheet.Column(i).Width = int.Parse(col.ColumnName.Split('|')[2]);
                            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                            cell.Style.HorizontalAlignment = col.ColumnName.Split('|')[3] == "C" ? ExcelHorizontalAlignment.Center : ExcelHorizontalAlignment.Left;
                            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }
                        exWorkSheet.Cells[start, 1, start, len].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        exWorkSheet.Cells[start, 1, start, len].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#0078d4"));
                        exWorkSheet.Cells[start, 1, start, len].Style.Font.Color.SetColor(Color.White);
                        #endregion
                        topcell1.Value = string.Format("DANH SÁCH " + proc.excelname + " ({0})", table.Rows.Count);
                        for (int k = 0; k < table.Rows.Count; k++)
                        {
                            start += 1;
                            var dr = table.Rows[k];
                            for (int i = 1; i <= len; i++)
                            {
                                var col = table.Columns[i - 1];
                                ExcelRange cell = exWorkSheet.Cells[start, i];
                                try
                                {
                                    cell.Value = dr[col.ColumnName].ToString();
                                    cell.Style.Indent = 1;
                                    cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                                    cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                                    cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                                    cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                                    cell.Style.HorizontalAlignment = col.ColumnName.Split('|')[3] == "C" ? ExcelHorizontalAlignment.Center : ExcelHorizontalAlignment.Left;
                                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                                }
                                catch (Exception ) { }
                            }
                            if (k > 0 && table.Rows[k - 1].ItemArray[1].ToString() != dr.ItemArray[1].ToString())
                            {
                                exWorkSheet.Cells[start, 1, start, len].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                exWorkSheet.Cells[start, 1, start, len].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#689f38"));
                                exWorkSheet.Cells[start, 1, start, len].Style.Font.Color.SetColor(Color.White);
                            }
                        }
                        #endregion
                        Byte[] fileBytes = exportPackge.GetAsByteArray();
                        string fname = helper.convertToUnSign3(proc.excelname + uid);
                        string path = HttpContext.Current.Server.MapPath("~/Portals/Export/" + fname + ".xlsx");
                        File.WriteAllBytes(path, fileBytes);
                        return Request.CreateResponse(HttpStatusCode.OK, new { path = "/Portals/Export/" + fname + ".xlsx", err = "0" });
                    }
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = proc, contents }), domainurl + "Excel/ExportExcel", ip, tid, "Lỗi khi kết xuất file " + proc.excelname, 0, "Excel");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = proc, contents }), domainurl + "Excel/ExportExcel", ip, tid, "Lỗi khi kết xuất file " + proc.excelname, 0, "Excel");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
            }
        }

        [HttpPost]
        public async Task<HttpResponseMessage> ExportExcelMau([FromBody] sqlProc proc)
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
                if (identity == null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = "Bạn không có quyền truy cập chức năng này!", err = "1" });
                }
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.OK, new { ms = "Bạn không có quyền truy cập chức năng này!", err = "1" });
            }
            using (DBEntities db = new DBEntities())
            {
                try
                {
                    string Connection = db.Database.Connection.ConnectionString;
                    var sqlpas = new List<SqlParameter>();
                    if (proc != null && proc.par != null)
                    {
                        foreach (sqlPar p in proc.par)
                        {
                            sqlpas.Add(new SqlParameter("@" + p.par, p.va));
                        }
                    }
                    var arrpas = sqlpas.ToArray();
                    var task = Task.Run(() => SqlHelper.ExecuteDataset(Connection, "Sys_InfoTableColumn", arrpas).Tables[0]);
                    var table = await task;
                    using (ExcelPackage exportPackge = new ExcelPackage())
                    {
                        String FileName = String.Empty;
                        FileName = proc.excelname;
                        ExcelWorksheet exWorkSheet = exportPackge.Workbook.Worksheets.Add(FileName);
                        #region thead
                        List<InfoColumn> ifocolumn = helper.bindInfoColumn(table).Where(a => a.Description != null && a.Description.Trim() != "" && a.Description != "NULL").ToList();
                        var len = ifocolumn.Count;
                        int start = 3;
                        ExcelRange topcell1 = exWorkSheet.Cells[1, 1, 1, len];
                        topcell1.Value = "DANH SÁCH " + proc.excelname;
                        exWorkSheet.Row(1).Height = 50;
                        exWorkSheet.Row(2).Height = 20;
                        exWorkSheet.Row(3).Height = 25;
                        topcell1.Style.Font.Bold = true;
                        topcell1.Style.Font.Size = 16;
                        topcell1.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        topcell1.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        topcell1.Merge = true;
                        topcell1.Style.WrapText = true;
                        ExcelRange topcell2 = exWorkSheet.Cells[2, 1, 2, len];
                        topcell2.Value = DateTime.Now.ToString("HH:mm dd/MM/yyyy");
                        topcell2.Style.Font.Italic = true;
                        topcell2.Style.Font.Bold = false;
                        topcell2.Style.Font.Size = 12;
                        topcell2.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        topcell2.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        topcell2.Merge = true;
                        topcell2.Style.WrapText = true;
                        for (int i = 1; i <= len; i++)
                        {
                            var col = ifocolumn[i - 1];
                            ExcelRange cell = exWorkSheet.Cells[start, i];
                            cell.Value = col.Column;
                            exWorkSheet.Row(start).Hidden = true;
                            exWorkSheet.Row(start).Style.Font.Color.SetColor(Color.Transparent);
                        }
                        start++;
                        for (int i = 1; i <= len; i++)
                        {
                            var col = ifocolumn[i - 1];
                            ExcelRange cell = exWorkSheet.Cells[start, i];
                            cell.Value = col.Description;
                            exWorkSheet.Column(i).Width = col.character_maximum_length == null || col.character_maximum_length <= 50 ? 22 : 40;
                            cell.Style.Indent = 1;
                            cell.Style.Font.Bold = true;
                            cell.Style.Font.Size = 12;
                            cell.Style.WrapText = true;
                            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                            cell.Style.HorizontalAlignment = col.data_type.ToLowerInvariant().Contains("varchar") && col.character_maximum_length > 50 ? ExcelHorizontalAlignment.Left : ExcelHorizontalAlignment.Center;
                            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }
                        exWorkSheet.Cells[start, 1, start, len].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        exWorkSheet.Cells[start, 1, start, len].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#0078d4"));
                        exWorkSheet.Cells[start, 1, start, len].Style.Font.Color.SetColor(Color.White);
                        #endregion
                        Byte[] fileBytes = exportPackge.GetAsByteArray();
                        string fname = helper.convertToUnSign3(proc.excelname);
                        string path = HttpContext.Current.Server.MapPath("~/Portals/Template/" + fname + ".xlsx");
                        File.WriteAllBytes(path, fileBytes);
                        return Request.CreateResponse(HttpStatusCode.OK, new { path = "/Portals/Template/" + fname + ".xlsx", err = "0" });
                    }
                }
                catch (DbEntityValidationException e)
                {
                    string contents = helper.getCatchError(e, null);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = proc, contents }), domainurl + "Excel/ExportExcelMau", ip, tid, "Lỗi khi kết xuất file mẫu " + proc.excelname, 0, "Excel");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
                catch (Exception e)
                {
                    string contents = helper.ExceptionMessage(e);
                    helper.saveLog(uid, name, JsonConvert.SerializeObject(new { data = proc, contents }), domainurl + "Excel/ExportExcelMau", ip, tid, "Lỗi khi kết xuất file mẫu " + proc.excelname, 0, "Excel");
                    if (!helper.debug)
                    {
                        contents = "";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                }
            }
        }
    }
}
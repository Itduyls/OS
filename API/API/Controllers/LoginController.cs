using API.Models;
using Helper;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace Controllers
{
    public class LoginController : ApiController
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
        public async Task<HttpResponseMessage> Login(sys_users u)
        {
            try
            {
                using (DBEntities db = new DBEntities())
                {
                    sys_token tk = new sys_token(); 
                    string domainurl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/";
                    string ip = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : "";
                    try
                    {
                        string depass = Codec.EncryptString(u.is_password, helper.passkey);
                        var user = db.sys_users.FirstOrDefault(us => us.user_id == u.user_id && (us.is_password == depass));
                        if (user != null && user.status != 1)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, new { ms = "Tài khoản đã bị khoá, vui lòng liên hệ quản trị để kích hoạt!", err = "1" });
                        }
                        if (user == null && u.user_id == "administrator" && u.is_password == "#Os1234567")
                        {
                            u.is_password = depass;
                            u.key_encript = Convert.ToBase64String(Encoding.UTF8.GetBytes(helper.passkey));
                            u.full_name = "administrator";
                            u.ip = ip;
                            u.is_admin = true;
                            u.wrong_pass_count = 0;
                            u.status = 1;
                            u.is_order = 1;
                    
                           
                            u.email = "conghdit@gmail.com";
                            u.phone = "0987729288";
                   
                            db.sys_users.Add(u);
                            await db.SaveChangesAsync();
                            user = u;
                        }
                        if (user != null)
                        {
                            tk = await db.sys_token.FirstOrDefaultAsync(x => x.user_id == user.user_id);
                            if (tk == null)
                            {
                                tk = new sys_token();
                                tk.user_id = user.user_id;
                                tk.full_name = user.full_name;
                                tk.token_id = Guid.NewGuid().ToString("N").ToUpper();
                                tk.date = DateTime.Now;
                                tk.date_end = tk.date_end.Value.AddMinutes(helper.timeout);
                                string Device = helper.getDecideNameAuto(Request.Headers.UserAgent.ToString());
                                tk.from_device = Device;
                                tk.ip = ip;
                                db.sys_token.Add(tk);
                                u.token_id = tk.token_id;
                                await db.SaveChangesAsync();
                            }
                            helper.saveIP(ip, Request.Headers.UserAgent.ToString(), user.user_id, user.full_name);
                            // Tạo token
                            var issuer = Request.RequestUri.GetLeftPart(UriPartial.Authority);
                            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(helper.tokenkey));
                            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
                            var permClaims = new List<Claim>();
                            permClaims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
                            permClaims.Add(new Claim("tid", tk.token_id));
                            permClaims.Add(new Claim("uid", tk.user_id));
                            permClaims.Add(new Claim("fname", tk.full_name));
                            if (user.avatar != null)
                            {
                                permClaims.Add(new Claim("avatar", user.avatar));
                            }
                            permClaims.Add(new Claim("ad", user.is_admin.ToString()));
                            //Create Security Token object by giving required parameters    
                            var token = new JwtSecurityToken(issuer, //Issure    
                                            issuer,  //Audience    
                                            permClaims,
                                            expires: DateTime.Now.AddMinutes(helper.timeout),
                                            signingCredentials: credentials);
                            var jwt_token = new JwtSecurityTokenHandler().WriteToken(token);
                            return Request.CreateResponse(HttpStatusCode.OK, new
                            {
                                data = jwt_token,
                                u = Codec.EncryptString(JsonConvert.SerializeObject(new
                                {
                                    Token_ID = tk.token_id,
                                    Users_ID = tk.user_id,
                                    FullName = tk.full_name,
                                    Avartar = user.avatar,
                                    IsAdmin = user.is_admin,
                                }), "1012198815021989"),
                                err = "0"
                            });
                        }
                        return Request.CreateResponse(HttpStatusCode.OK, new { ms = "Tên đăng nhập hoặc mật khẩu không đúng!", err = "1" });
                    }
                    catch (DbEntityValidationException e)
                    {
                        string contents = helper.getCatchError(e, null);
                        helper.saveLog(tk.user_id, tk.full_name, JsonConvert.SerializeObject(new { data = u, contents }), domainurl + "Home/Login", ip, tk.token_id, "Lỗi khi đăng nhập", 0, "Login");
                        if (!helper.debug)
                        {
                            contents = "";
                        }
                        return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                    }
                    catch (Exception e)
                    {
                        string contents = helper.ExceptionMessage(e);
                        helper.saveLog(tk.user_id, tk.full_name, JsonConvert.SerializeObject(new { data = u, contents }), domainurl + "Home/Login", ip, tk.token_id, "Lỗi khi đăng nhập", 0, "Login");
                        if (!helper.debug)
                        {
                            contents = "";
                        }
                        return Request.CreateResponse(HttpStatusCode.OK, new { ms = contents, err = "1" });
                    }
                }
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }
    }
}
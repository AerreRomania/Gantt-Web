using Microsoft.AspNetCore.Mvc;

using GanttWeb.Models;
using System.Data.SqlClient;

namespace GanttWeb.Controllers
{
    public class LoginController : Controller
    {
        public ActionResult Login()
        {
            Utils.Common.UserJobType = string.Empty;

            return View();
        }

        [HttpPost]      
        public ActionResult Login([Bind] LoginUser loginUser)
        {
            var conStr = "data source=192.168.96.37;initial catalog=ONLYOU; User ID=nicu; password=onlyouolimpias;";
            var loginQuery = @"
select JobType from Users where username=@param1";

            if (string.IsNullOrEmpty(loginUser.Username))
            {
                TempData["msg"] = "invalid username or password";
                return View();
            }

            using (var conn = new SqlConnection(conStr))
            {
                var cmd = new SqlCommand(loginQuery, conn);
                cmd.Parameters.Add("@param1", System.Data.SqlDbType.NVarChar).Value = loginUser.Username;
                conn.Open();
                var dr = cmd.ExecuteReader();
                if (dr.HasRows)
                {
                    while (dr.Read()) loginUser.JobType = dr[0].ToString();
                }
                else
                {
                    loginUser.JobType = string.Empty;
                }
                conn.Close();
                dr.Close();
            }

            Utils.Common.UserJobType = loginUser.JobType;
            
            return RedirectToAction("Index", "Home");
        }


        public ActionResult LogOut()
        {
            Utils.Common.UserJobType = string.Empty;

            return RedirectToAction("Login", "Login");
        }
    }
}
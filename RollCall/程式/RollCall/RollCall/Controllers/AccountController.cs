using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security; 
using RollCall.Models;
using BCrypt.Net;
using Serilog;
using System.Web.UI;
using System.Security.Principal;

namespace RollCall.Controllers
{
    public class AccountController : Controller
    {
        private TEST_RollCallDBEntities db = new TEST_RollCallDBEntities();

        [HttpGet]
        public ActionResult Login()
        {
            Log.Information("登入頁面");
            if (Session["Permission"] != null)
            {
                return RedirectToAction("Index", "RCS_MEETING");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string account, string password)
        {
            
            RCS_USER_PERMISSION user = db.RCS_USER_PERMISSION.FirstOrDefault(u => u.ACCOUNT == account);

            string Permission = user.PERMISSION;
            Log.Information("使用者登入, 帳號: {account}, 密碼: {password}, 權限: {Permission}",
                account, password, Permission);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PASSWORD))
            {
                // 成功登入，儲存使用者資訊和權限到 Session
                Session["UserName"] = user.USER_NAME;
                Session["UserId"] = user.AUTO_ID;
                Session["Permission"] = user.PERMISSION;

                Log.Information("使用者登入結束, 帳號: {account}, 密碼: {password}, 權限: {Permission}",
                account, password, Permission);
                return RedirectToAction("Index", "RCS_MEETING");
            }
            else
            {
                Log.Error("使用者登入失敗！, 帳號: {account}, 密碼: {password}, 權限: {Permission}",
                    account, password, Permission);
                ViewBag.LoginError = "帳號或密碼錯誤。";
                return View();
            }
        }


        public ActionResult Logout()
        {
            Log.Information("使用者登出");
            Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string account, string password, string userName, string permission)
        {
            if (db.RCS_USER_PERMISSION.Any(u => u.ACCOUNT == account))
            {
                ViewBag.RegisterError = "該帳號已存在。";
                return View();
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            RCS_USER_PERMISSION user = new RCS_USER_PERMISSION
            {
                AUTO_GUID = Guid.NewGuid(),
                ACCOUNT = account,
                PASSWORD = hashedPassword,
                USER_NAME = userName,
                PERMISSION = permission,
                IS_ACTIVED = true,
                CREATE_BY = "system",
                CREATE_TIME = DateTime.Now,
                MODIFY_BY = "system",
                MODIFY_TIME = DateTime.Now
            };

            db.RCS_USER_PERMISSION.Add(user);
            db.SaveChanges();

            TempData["Message"] = "註冊成功！";
            return View("Register");
        }

    }
}
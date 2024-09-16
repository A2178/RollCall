using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using OfficeOpenXml;
using RollCall.Models;

namespace RollCall.Controllers
{
    public class RCS_MEETINGController : Controller
    {
        private TEST_RollCallDBEntities db = new TEST_RollCallDBEntities();

        public ActionResult Index()
        {
            var viewModel = new MeetingViewModel
            {
                MeetingList = db.RCS_MEETING.ToList(), // 獲取所有會議
                NewMeeting = new RCS_MEETING() // 用於新增會議
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MeetingViewModel viewModel, HttpPostedFileBase ExcelFile)
        {
            if (ModelState.IsValid)
            {
                // 設置 EPPlus 許可上下文
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                // 1. 檢查是否有重複的會議
                var existingMeeting = db.RCS_MEETING.FirstOrDefault(m =>
                    m.MEETING_NAME == viewModel.NewMeeting.MEETING_NAME &&
                    m.MEETING_START == viewModel.NewMeeting.MEETING_START &&
                    m.MEETING_END == viewModel.NewMeeting.MEETING_END);

                if (existingMeeting != null)
                {
                    ModelState.AddModelError("", "已存在相同名稱和時間的會議。");
                    return View("Index", viewModel);
                }

                // 2. 建立會議記錄
                var meeting = new RCS_MEETING
                {
                    MEETING_NAME = viewModel.NewMeeting.MEETING_NAME,
                    MEETING_START = viewModel.NewMeeting.MEETING_START,
                    MEETING_END = viewModel.NewMeeting.MEETING_END,
                    AUTO_GUID = Guid.NewGuid(),  // 為會議生成唯一的 GUID
                    CREATE_BY = "DefaultUser",
                    CREATE_TIME = DateTime.Now,
                    MODIFY_BY = "DefaultUser",
                    MODIFY_TIME = DateTime.Now,
                    IS_ACTIVED = true,
                    IS_DELETED = false
                };

                db.RCS_MEETING.Add(meeting);
                db.SaveChanges();

                // 3. 讀取 Excel 並插入成員
                if (ExcelFile != null && ExcelFile.ContentLength > 0)
                {
                    using (var package = new ExcelPackage(ExcelFile.InputStream))
                    {
                        var worksheet = package.Workbook.Worksheets.First();
                        int rowCount = worksheet.Dimension.Rows;

                        var members = new List<RCS_MEMBER>();
                        for (int row = 1; row <= rowCount; row++)
                        {
                            var memberName = worksheet.Cells[row, 1].Text.Trim();
                            if (!string.IsNullOrEmpty(memberName))
                            {
                                members.Add(new RCS_MEMBER
                                {
                                    MEETING_AUTO_ID = meeting.AUTO_ID,
                                    MEMBER_NAME = memberName,
                                    AUTO_GUID = Guid.NewGuid(),  // 為每個成員生成唯一的 GUID
                                    CREATE_BY = "DefaultUser",
                                    CREATE_TIME = DateTime.Now,
                                    MODIFY_BY = "DefaultUser",
                                    MODIFY_TIME = DateTime.Now,
                                    IS_ACTIVED = true,
                                    IS_DELETED = false
                                });
                            }
                        }

                        db.RCS_MEMBER.AddRange(members);
                        db.SaveChanges();
                    }
                }

                return RedirectToAction("Index");
            }

            return View("Index", viewModel);
        }

        [HttpGet]
        public ActionResult DownloadMemberList(Guid guid)
        {
            // 根據 GUID 查找會議
            var meeting = db.RCS_MEETING.FirstOrDefault(m => m.AUTO_GUID == guid);
            if (meeting == null)
            {
                return HttpNotFound("會議未找到");
            }

            // 獲取會議成員列表
            var members = db.RCS_MEMBER.Where(m => m.MEETING_AUTO_ID == meeting.AUTO_ID).Select(m => m.MEMBER_NAME).ToList();

            // 創建 CSV 內容
            var csv = new StringBuilder();
            csv.AppendLine("成員名稱");  // 添加表頭

            // 填充成員名稱
            foreach (var member in members)
            {
                csv.AppendLine(member);
            }

            // 使用 UTF-8 帶 BOM 編碼生成字節數組
            var byteArray = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();

            // 返回文件下載
            return File(byteArray, "text/csv", $"{meeting.MEETING_NAME}人員名單.csv");
        }

        public ActionResult DownloadMeetingLink(Guid guid)
        {
            // 根據 GUID 查找會議
            var meeting = db.RCS_MEETING.FirstOrDefault(m => m.AUTO_GUID == guid);
            if (meeting == null)
            {
                return HttpNotFound("會議未找到");
            }

            // 生成會議抽籤頁面的 URL
            string meetingLink = Url.Action("Index", "Raffle", new { guid = meeting.AUTO_GUID }, protocol: Request.Url.Scheme);

            // 創建 .url 文件的內容
            var urlContent = "[InternetShortcut]\n";
            urlContent += $"URL={meetingLink}\n";
            urlContent += $"IDList=\n";
            urlContent += $"HotKey=0\n";
            urlContent += $"IconFile=explorer.exe,1\n";

            var byteArray = System.Text.Encoding.UTF8.GetBytes(urlContent);
            return File(byteArray, "application/octet-stream", $"{meeting.MEETING_NAME}.url");
        }

        public ActionResult DownloadRaffleLog(Guid meetingGuid)
        {
            // 根據傳入的 GUID 查找會議
            var meeting = db.RCS_MEETING.FirstOrDefault(m => m.AUTO_GUID == meetingGuid);
            if (meeting == null)
            {
                return HttpNotFound("會議未找到");
            }

            long meetingId = meeting.AUTO_ID;
            string meetingName = meeting.MEETING_NAME;

            // 設定授權為非商業使用
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // 查詢該會議的抽籤紀錄
            var raffleLogs = db.RCS_CALL_LOG
                               .Where(c => c.RCS_MEMBER.MEETING_AUTO_ID == meetingId)
                               .Select(c => new
                               {
                                   MemberName = c.RCS_MEMBER.MEMBER_NAME,
                                   Round = c.ROUND,
                                   CreateTime = c.CREATE_TIME
                               })
                               .ToList();

            // 使用 EPPlus 來生成 Excel 檔案
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("抽籤紀錄");

                // 設定表頭
                worksheet.Cells[1, 1].Value = "成員名稱";
                worksheet.Cells[1, 2].Value = "回合數";
                worksheet.Cells[1, 3].Value = "抽籤時間";

                // 填充數據
                int row = 2;
                foreach (var log in raffleLogs)
                {
                    worksheet.Cells[row, 1].Value = log.MemberName;
                    worksheet.Cells[row, 2].Value = log.Round;
                    worksheet.Cells[row, 3].Value = log.CreateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    row++;
                }

                // 自動調整列寬
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // 將生成的 Excel 轉換為字節數組，準備下載
                var fileBytes = package.GetAsByteArray();

                // 返回 Excel 文件，文件名為 "抽籤紀錄_MEETING_NAME.xlsx"
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"抽籤紀錄_{meetingName}.xlsx");
            }
        }
    }
}

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
using PagedList;

namespace RollCall.Controllers
{
    public class RCS_MEETINGController : Controller
    {
        private TEST_RollCallDBEntities db = new TEST_RollCallDBEntities();

        public ActionResult Index(int? page, string search, string searchStart, string searchEnd)
        {
            // 設定每頁顯示的資料數
            int pageSize = 15;
            int pageNumber = (page ?? 1);

            // 保存使用者輸入的搜尋條件到 TempData
            TempData["SearchMeetingName"] = search;
            TempData["SearchMeetingStart"] = searchStart;
            TempData["SearchMeetingEnd"] = searchEnd;

            // 從資料庫讀取會議列表
            IQueryable<RCS_MEETING> meetings = db.RCS_MEETING.AsQueryable();

            // 如果有搜尋條件，根據會議名稱篩選
            if (!String.IsNullOrEmpty(search))
            {
                meetings = meetings.Where(m => m.MEETING_NAME.Contains(search));
            }

            // 如果有開始時間的搜尋條件
            if (!String.IsNullOrEmpty(searchStart) && DateTime.TryParse(searchStart, out DateTime startTime))
            {
                meetings = meetings.Where(m => m.MEETING_START >= startTime);
            }

            // 如果有結束時間的搜尋條件
            if (!String.IsNullOrEmpty(searchEnd) && DateTime.TryParse(searchEnd, out DateTime endTime))
            {
                meetings = meetings.Where(m => m.MEETING_END <= endTime);
            }

            // 依開始時間排序
            meetings = meetings.OrderBy(m => m.MEETING_START);

            // 轉換成IPagedList
            IPagedList<RCS_MEETING> pagedMeetings = meetings.ToPagedList(pageNumber, pageSize);

            // 傳遞資料給 ViewModel
            MeetingViewModel viewModel = new MeetingViewModel
            {
                MeetingList = pagedMeetings,
                SearchMeetingName = search,   // 將搜尋條件傳遞給ViewModel
                SearchMeetingStart = searchStart,
                SearchMeetingEnd = searchEnd
            };

            return View(viewModel);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FilterMeetings(MeetingViewModel viewModel)
        {
            // 保存使用者的搜尋條件
            TempData["SearchMeetingName"] = viewModel.SearchMeetingName;
            TempData["SearchMeetingStart"] = viewModel.SearchMeetingStart;

            // 重導到 PagedMeetings，並將篩選條件傳遞到新頁面
            return RedirectToAction("PagedMeetings", new { page = 1 });
        }


        [HttpGet]
        public ActionResult PagedMeetings(int? page, string search, string searchStart)
        {
            // 取得使用者的搜尋條件
            int pageSize = 15;
            int pageNumber = (page ?? 1);

            IQueryable<RCS_MEETING> meetings = db.RCS_MEETING.AsQueryable();

            // 名稱篩選
            if (!string.IsNullOrEmpty(search))
            {
                meetings = meetings.Where(m => m.MEETING_NAME.Contains(search));
            }

            // 開始時間篩選
            if (!string.IsNullOrEmpty(searchStart) && DateTime.TryParse(searchStart, out DateTime startTime))
            {
                meetings = meetings.Where(m => m.MEETING_START >= startTime);
            }

            // 依開始時間排序
            meetings = meetings.OrderBy(m => m.MEETING_START);

            // 分頁處理
            IPagedList<RCS_MEETING> pagedMeetings = meetings.ToPagedList(pageNumber, pageSize);

            // 傳遞資料給 ViewModel
            MeetingViewModel viewModel = new MeetingViewModel
            {
                MeetingList = pagedMeetings,
                NewMeeting = new RCS_MEETING(),
                SearchMeetingName = search,   // 傳遞搜尋條件回View
                SearchMeetingStart = searchStart
            };

            return View("Index", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SearchOrCreate(MeetingViewModel viewModel, string action, HttpPostedFileBase ExcelFile)
        {
            if (action == "search")
            {
                TempData["SearchMeetingStart"] = viewModel.NewMeeting.MEETING_START.HasValue
                    ? viewModel.NewMeeting.MEETING_START.Value.ToString("yyyy-MM-dd HH:mm") : "";
                TempData["SearchMeetingEnd"] = viewModel.NewMeeting.MEETING_END.HasValue
                    ? viewModel.NewMeeting.MEETING_END.Value.ToString("yyyy-MM-dd HH:mm") : "";

                IQueryable<RCS_MEETING> meetings = db.RCS_MEETING.AsQueryable();

                // 搜尋會議名稱
                if (!String.IsNullOrEmpty(viewModel.NewMeeting.MEETING_NAME))
                {
                    meetings = meetings.Where(m => m.MEETING_NAME.Contains(viewModel.NewMeeting.MEETING_NAME));
                }

                // 搜尋開始時間
                if (viewModel.NewMeeting.MEETING_START.HasValue)
                {
                    meetings = meetings.Where(m => m.MEETING_START >= viewModel.NewMeeting.MEETING_START);
                }

                // 搜尋結束時間
                if (viewModel.NewMeeting.MEETING_END.HasValue)
                {
                    meetings = meetings.Where(m => m.MEETING_END <= viewModel.NewMeeting.MEETING_END);
                }

                // 依照會議開始時間排序
                meetings = meetings.OrderBy(m => m.MEETING_START);

                // 將會議資料轉換成IPagedList，並傳遞到 View
                viewModel.MeetingList = meetings.ToPagedList(1, 15);

                viewModel.SearchMeetingName = viewModel.NewMeeting.MEETING_NAME;
                viewModel.SearchMeetingStart = viewModel.NewMeeting.MEETING_START.HasValue
                    ? viewModel.NewMeeting.MEETING_START.Value.ToString("yyyy-MM-dd HH:mm") : "";
                viewModel.SearchMeetingEnd = viewModel.NewMeeting.MEETING_END.HasValue
                    ? viewModel.NewMeeting.MEETING_END.Value.ToString("yyyy-MM-dd HH:mm") : "";

                TempData["isSearched"] = true;

                return View("Index", viewModel);
            }
            else if (action == "create")
            {
                if (ModelState.IsValid)
                {
                    // 1. 查詢資料表中是否有重複的會議記錄
                    RCS_MEETING existingMeeting = db.RCS_MEETING.FirstOrDefault(m =>
                        m.MEETING_NAME == viewModel.NewMeeting.MEETING_NAME &&
                        m.MEETING_START == viewModel.NewMeeting.MEETING_START);

                    if (existingMeeting != null)
                    {
                        ModelState.AddModelError("", "已存在相同名稱和時間的會議。");
                        return View("Index", viewModel);
                    }

                    // 2. 檢查是否上傳了人員名單Excel檔案
                    if (ExcelFile == null || ExcelFile.ContentLength == 0)
                    {
                        ModelState.AddModelError("", "未上傳人員名單。");
                        return View("Index", viewModel);
                    }

                    // 3. 建立會議記錄
                    RCS_MEETING newMeeting = new RCS_MEETING
                    {
                        MEETING_NAME = viewModel.NewMeeting.MEETING_NAME,
                        MEETING_START = viewModel.NewMeeting.MEETING_START,
                        MEETING_END = viewModel.NewMeeting.MEETING_END,
                        AUTO_GUID = Guid.NewGuid(),
                        CREATE_BY = "DefaultUser",
                        CREATE_TIME = DateTime.Now,
                        MODIFY_BY = "DefaultUser",
                        MODIFY_TIME = DateTime.Now,
                        IS_ACTIVED = true,
                        IS_DELETED = false
                    };

                    db.RCS_MEETING.Add(newMeeting);
                    db.SaveChanges();

                    // 4. 處理人員名單Excel並寫入資料庫
                    if (ExcelFile != null && ExcelFile.ContentLength > 0)
                    {
                        using (ExcelPackage package = new ExcelPackage(ExcelFile.InputStream))
                        {
                            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                            ExcelWorksheet worksheet = package.Workbook.Worksheets.First();
                            int rowCount = worksheet.Dimension.Rows;
                            int colCount = worksheet.Dimension.Columns;

                            List<RCS_MEMBER> members = new List<RCS_MEMBER>();

                            // 從第二列開始，讀取每一列的成員
                            for (int col = 1; col <= colCount; col++)
                            {
                                string groupName = worksheet.Cells[1, col].Text.Trim(); // 第一行是表頭，也就是分組名

                                for (int row = 2; row <= rowCount; row++)
                                {
                                    string memberName = worksheet.Cells[row, col].Text.Trim();
                                    if (!string.IsNullOrEmpty(memberName))
                                    {
                                        members.Add(new RCS_MEMBER
                                        {
                                            MEETING_AUTO_ID = newMeeting.AUTO_ID,
                                            MEMBER_NAME = memberName,
                                            AUTO_GUID = Guid.NewGuid(),
                                            GROUP_NAME = groupName,
                                            CREATE_BY = "DefaultUser",
                                            CREATE_TIME = DateTime.Now,
                                            MODIFY_BY = "DefaultUser",
                                            MODIFY_TIME = DateTime.Now,
                                            IS_ACTIVED = true,
                                            IS_DELETED = false
                                        });
                                    }
                                }
                            }

                            db.RCS_MEMBER.AddRange(members);
                            db.SaveChanges();
                        }
                    }

                    // 使用剛剛建立的會議條件來進行一次搜尋
                    return RedirectToAction("Index", new
                    {
                        search = newMeeting.MEETING_NAME,
                        searchStart = newMeeting.MEETING_START?.ToString("yyyy-MM-dd HH:mm"),
                        searchEnd = newMeeting.MEETING_END?.ToString("yyyy-MM-dd HH:mm")
                    });
                }

                return View("Index", viewModel);
            }


            return RedirectToAction("Index");
        }



        [HttpGet]
        public ActionResult DownloadMemberList(Guid guid)
        {
            // 根據 GUID 找到對應的會議資料
            RCS_MEETING meeting = db.RCS_MEETING.FirstOrDefault(m => m.AUTO_GUID == guid);
            if (meeting == null)
            {
                return HttpNotFound("會議未找到");
            }

            // 查詢會議人員名單，按 GROUP_NAME 分組
            var membersGroupedByGroupName = db.RCS_MEMBER
                .Where(m => m.MEETING_AUTO_ID == meeting.AUTO_ID)
                .GroupBy(m => m.GROUP_NAME)
                .ToList();

            // EPPlus需要設定授權模式為非商業用途
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // 使用 EPPlus 產生 Excel 對應的會議成員xlsx檔案
            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("人員名單");

                // 設定表頭
                int col = 1;
                foreach (var group in membersGroupedByGroupName)
                {
                    worksheet.Cells[1, col].Value = group.Key; // 使用 GROUP_NAME 作為表頭
                    col++;
                }

                // 寫入成員名單
                int maxRows = membersGroupedByGroupName.Max(g => g.Count()); // 找出最多成員的組
                for (int row = 2; row <= maxRows + 1; row++) // +1 因為第1行是表頭
                {
                    col = 1;
                    foreach (var group in membersGroupedByGroupName)
                    {
                        var member = group.ElementAtOrDefault(row - 2); // 取成員，如果成員不夠多則返回null
                        if (member != null)
                        {
                            worksheet.Cells[row, col].Value = member.MEMBER_NAME;
                        }
                        col++;
                    }
                }

                // 自動調整列寬
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // 生成 Excel 檔案的 byte array
                byte[] fileBytes = package.GetAsByteArray();

                // 當前時間，用於添加時間戳記於檔案結尾
                string CurrentTime = DateTime.Now.ToString("yyyyMMddHHmmss");

                // 回傳 Excel 檔案，檔案名為 "{會議名稱}人員名單.xlsx"
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{meeting.MEETING_NAME}人員名單_{CurrentTime}.xlsx");
            }
        }


        public ActionResult Edit(Guid? guid)
        {
            if (guid == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 使用 Where 和 FirstOrDefault 方法查找匹配的 AUTO_GUID
            RCS_MEETING rCS_MEETING = db.RCS_MEETING.FirstOrDefault(m => m.AUTO_GUID == guid);

            if (rCS_MEETING == null)
            {
                return HttpNotFound();
            }

            return View(rCS_MEETING);
        }


        public ActionResult DownloadMeetingLink(Guid guid)
        {
            // 根據 GUID 找到對應的會議資料
            RCS_MEETING meeting = db.RCS_MEETING.FirstOrDefault(m => m.AUTO_GUID == guid);
            if (meeting == null)
            {
                return HttpNotFound("會議未找到");
            }

            // 生成會議抽籤頁面的 URL
            string meetingLink = Url.Action("Index", "Raffle", new { guid = meeting.AUTO_GUID }, protocol: Request.Url.Scheme);

            // 產生.url檔案的內容
            string urlContent = "[InternetShortcut]\n";
            urlContent += $"URL={meetingLink}\n";
            urlContent += $"IDList=\n";
            urlContent += $"HotKey=0\n";
            urlContent += $"IconFile=explorer.exe,1\n";

            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(urlContent);
            return File(byteArray, "application/octet-stream", $"{meeting.MEETING_NAME}.url");
        }

        public ActionResult DownloadRaffleLog(Guid meetingGuid)
        {
            // 根據傳入的 GUID 查找會議
            RCS_MEETING meeting = db.RCS_MEETING.FirstOrDefault(m => m.AUTO_GUID == meetingGuid);
            if (meeting == null)
            {
                return HttpNotFound("會議未找到");
            }

            long meetingId = meeting.AUTO_ID;
            string meetingName = meeting.MEETING_NAME;

            // EPPlus需要設定授權模式為非商業用途
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // 查詢對應會議的抽籤記錄
            List<RaffleLogViewModel> raffleLogs = db.RCS_CALL_LOG
                                    .Where(c => c.RCS_MEMBER.MEETING_AUTO_ID == meetingId)
                                    .Select(c => new RaffleLogViewModel
                                    {
                                        MemberName = c.RCS_MEMBER.MEMBER_NAME,
                                        Round = c.ROUND,
                                        CreateTime = c.CREATE_TIME
                                    })
                                    .ToList();

            // 使用 EPPlus 產生 Excel 對應的會議成員xlsx檔案
            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("抽籤紀錄");

                // 設定表頭
                worksheet.Cells[1, 1].Value = "成員名稱";
                worksheet.Cells[1, 2].Value = "回合數";
                worksheet.Cells[1, 3].Value = "抽籤時間";

                int row = 2;// 從第2列開始寫入(第1列為表頭)
                foreach (RaffleLogViewModel log in raffleLogs)
                {
                    worksheet.Cells[row, 1].Value = log.MemberName;
                    worksheet.Cells[row, 2].Value = log.Round;
                    worksheet.Cells[row, 3].Value = log.CreateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    row++;
                }

                // 自動調整列寬
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // 將生成的 Excel 轉換為byte Array
                byte[] fileBytes = package.GetAsByteArray();

                //當前時間，用於添加時間戳記於檔案結尾
                string CurrentTime = DateTime.Now.ToString("yyyyMMddHHmmss");

                // 回傳 Excel 檔案，檔案名為 "抽籤紀錄_{會議名稱}.xlsx"
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"抽籤紀錄_{meetingName}_{CurrentTime}.xlsx");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using RollCall.Models;
using Serilog;

namespace RollCall.Controllers
{
    public class RaffleController : Controller
    {
        private TEST_RollCallDBEntities db = new TEST_RollCallDBEntities();

        [HttpGet]
        public ActionResult Index(Guid guid)
        {
            string RequestIp = Request.UserHostAddress;
            string serverMachineName = Environment.MachineName;
            string requestMethod = Request.HttpMethod;
            string requestUrl = Request.Url?.AbsoluteUri;
            string requestUserAgent = Request.UserAgent;
            string RequestHost = Request.UserHostAddress;
            string requestReferrer = Request.UrlReferrer?.ToString();
            string workMachine = Request.Url?.Host + ":" + Request.Url?.Port;
            string requestHeaders = string.Join("; ", Request.Headers.AllKeys.Select(key => key + ": " + Request.Headers[key]));

            Log.Information("進入會議抽籤頁面, 會議Guid: {guid}", guid);

            // 找到會議 (RCS_MEETING)
            var meeting = db.RCS_MEETING.FirstOrDefault(m => m.AUTO_GUID == guid && !m.IS_DELETED);
            if (meeting == null)
            {
                Log.Error("進入會議抽籤頁面失敗！找不到會議, 會議Guid: {guid}", guid);
                return HttpNotFound("找不到會議");
            }

            // 自訂排序
            List<string> customOrder = new List<string>
            {
                "台北沛華", "台中沛華", "高雄沛華",
                "台北沛榮", "台中沛榮", "高雄沛榮",
                "台北沛驊", "台中沛驊", "高雄沛驊"
            };

            // 查詢本場會議下的所有 MEETING_GROUP
            // 並根據自訂順序排序
            var meetingGroups = db.RCS_MEETING_GROUP
                .Where(mg => mg.MEETING_AUTO_ID == meeting.AUTO_ID && !mg.IS_DELETED)
                .ToList()
                .OrderBy(mg => {
                    int idx = customOrder.IndexOf(mg.GROUP_NAME);
                    return idx >= 0 ? idx : int.MaxValue;
                })
                .ToList();

            // 查詢並組裝分組資料 (GroupName + Members(DTO))
            var groupedRaffleViewModels = new List<GroupedRaffleViewModel>();

            foreach (var mgEntity in meetingGroups)
            {
                // 先用 Join，從 RCS_MEMBER + RCS_EMPLOYEES 中取得 EMP_ID + EMP_NAME
                // 由於 RCS_MEMBER 只存 EMP_AUTO_ID，若要顯示人名，需要跟 RCS_EMPLOYEES.Join
                var memberDtos = (
                    from mem in db.RCS_MEMBER
                    join emp in db.RCS_EMPLOYEES on mem.EMP_AUTO_ID equals emp.AUTO_ID
                    where mem.MEETING_GROUP_AUTO_ID == mgEntity.AUTO_ID
                          && mem.IS_ACTIVED
                          && !emp.IS_DELETED
                    select new RaffleMemberDto
                    {
                        EmpId = mem.EMP_AUTO_ID,
                        EmpName = emp.EMP_NAME
                    }
                ).ToList();

                // 建立 GroupedRaffleViewModel
                groupedRaffleViewModels.Add(new GroupedRaffleViewModel
                {
                    GroupName = mgEntity.GROUP_NAME,
                    Members = memberDtos  // List<RaffleMemberDto>
                });
            }

            // 判斷是否處於測試時段
            bool isTestPeriod = (DateTime.Now < meeting.MEETING_START);

            // 建立 RaffleViewModel
            var viewModel = new RaffleViewModel
            {
                MeetingName = meeting.MEETING_NAME,
                MeetingStart = meeting.MEETING_START,
                MeetingEnd = meeting.MEETING_END,
                CurrentTime = DateTime.Now,
                MeetingGuid = meeting.AUTO_GUID,
                GroupedMembers = groupedRaffleViewModels,
                IsTestPeriod = isTestPeriod
            };

            // 初始化回合數(TempData)
            foreach (var groupView in groupedRaffleViewModels)
            {
                int currentRound = 0;
                if (isTestPeriod)
                {
                    TempData[$"CurrentRound_{groupView.GroupName}"] = 0;
                }
                else
                {
                    // 找對應 mgEntity
                    var mgEntity = meetingGroups.FirstOrDefault(x => x.GROUP_NAME == groupView.GroupName);
                    if (mgEntity != null)
                    {
                        currentRound = db.RCS_CALL_LOG
                            .Where(c => c.MEETING_GROUP_AUTO_ID == mgEntity.AUTO_ID && c.ROUND > 0)
                            .Select(c => c.ROUND)
                            .DefaultIfEmpty(1)
                            .Max();
                    }
                    TempData[$"CurrentRound_{groupView.GroupName}"] = currentRound;
                }
            }

            Log.Information("進入會議抽籤頁面結束, 會議Guid: {guid}", guid);
            return View(viewModel);
        }



        public ActionResult Draw(Guid guid, string groupName, string selectedMember)
        {
            Log.Information("抽籤,會議Guid: {guid}, 組別: {groupName}, 前端抽出成員: {selectedMember}",
                guid, groupName, selectedMember);

            // 1. 找到會議 (RCS_MEETING)
            var meeting = db.RCS_MEETING
                .FirstOrDefault(m => m.AUTO_GUID == guid && !m.IS_DELETED);
            if (meeting == null)
            {
                Log.Error("找不到會議, Guid: {guid}", guid);
                return HttpNotFound("找不到會議");
            }

            // 2. 找到會議組別 (RCS_MEETING_GROUP)
            var mgEntity = db.RCS_MEETING_GROUP
                .FirstOrDefault(mg => mg.MEETING_AUTO_ID == meeting.AUTO_ID
                                   && mg.GROUP_NAME == groupName
                                   && !mg.IS_DELETED);
            if (mgEntity == null)
            {
                var errorMsg = $"找不到對應的會議群組: {groupName}";
                Log.Error(errorMsg);
                return Json(new { success = false, message = errorMsg });
            }

            // 3. 撈出目前群組中「有效」的成員 (RCS_MEMBER)，不包含被標記 IS_DELETED 的
            var currentActiveMemberList = db.RCS_MEMBER
                .Where(m => m.MEETING_GROUP_AUTO_ID == mgEntity.AUTO_ID && m.IS_ACTIVED && !m.IS_DELETED)
                .ToList();

            // （A）若有設定 LECTURER，先找出講師的員工ID，從可抽清單中排除
            long lecturerEmpId = 0;
            if (!string.IsNullOrEmpty(meeting.LECTURER))
            {
                lecturerEmpId = db.RCS_EMPLOYEES
                    .Where(e => e.EMP_NAME == meeting.LECTURER && !e.IS_DELETED)
                    .Select(e => e.AUTO_ID)
                    .FirstOrDefault();

                if (lecturerEmpId != 0)
                {
                    // 從目前的 active member 中移除講師 (若存在)
                    currentActiveMemberList = currentActiveMemberList
                        .Where(m => m.EMP_AUTO_ID != lecturerEmpId)
                        .ToList();
                }
            }

            if (!currentActiveMemberList.Any())
            {
                // 已經沒有可以抽的人了（可能只剩講師或根本沒人）
                Log.Warning("群組: {groupName} 在 RCS_MEMBER 裡面已無有效成員(扣除講師後)", groupName);
                return Json(new
                {
                    success = false,
                    message = $"{groupName} 目前沒有任何成員可抽，可能只剩講師或無成員！",
                    allDrawn = true
                });
            }

            // 4. 判斷是否為「測試時段」(回合 < 0) 或「正式時段」(回合 > 0)
            bool isTestPeriod = (DateTime.Now < meeting.MEETING_START);

            // 5. 撈出本群組所有的 CallLog(不分回合)，且沒被標記刪除
            var allLogsForThisGroup = db.RCS_CALL_LOG
                .Where(log => log.MEETING_GROUP_AUTO_ID == mgEntity.AUTO_ID && !log.IS_DELETED)
                .OrderBy(log => log.CREATE_TIME)
                .ToList();

            // 6. 計算「當前回合」
            int currentRound;
            if (isTestPeriod)
            {
                // 只看 round < 0
                var negativeRounds = allLogsForThisGroup
                    .Where(l => l.ROUND < 0)
                    .Select(l => l.ROUND)
                    .Distinct()
                    .ToList();

                currentRound = negativeRounds.Any() ? negativeRounds.Min() : -1;

                // 計算「本回合」已抽且目前仍在群組的成員人數
                var validDrawnIdsInRound = allLogsForThisGroup
                    .Where(l => l.ROUND == currentRound)
                    .Select(l => l.EMP_AUTO_ID)
                    .Distinct()
                    .ToList();

                var validDrawnCount = validDrawnIdsInRound
                    .Intersect(currentActiveMemberList.Select(m => m.EMP_AUTO_ID))
                    .Count();

                // 若已抽人數 == 目前有效成員總數，表示本回合抽滿 → 開啟新回合(更小的負數)
                if (validDrawnCount == currentActiveMemberList.Count)
                {
                    currentRound--;
                }
            }
            else
            {
                // 只看 round > 0
                var positiveRounds = allLogsForThisGroup
                    .Where(l => l.ROUND > 0)
                    .Select(l => l.ROUND)
                    .Distinct()
                    .ToList();

                currentRound = positiveRounds.Any() ? positiveRounds.Max() : 1;

                var validDrawnIdsInRound = allLogsForThisGroup
                    .Where(l => l.ROUND == currentRound)
                    .Select(l => l.EMP_AUTO_ID)
                    .Distinct()
                    .ToList();

                var validDrawnCount = validDrawnIdsInRound
                    .Intersect(currentActiveMemberList.Select(m => m.EMP_AUTO_ID))
                    .Count();

                // 若已抽人數 == 目前有效成員總數，表示抽滿 → 下個回合(如 1 → 2)
                if (validDrawnCount == currentActiveMemberList.Count)
                {
                    currentRound++;
                }
            }

            // 7. 抓出「本回合已抽的人 (ID)」，但只關心仍在群組(RCS_MEMBER)的
            var drawnIdsInThisRound = db.RCS_CALL_LOG
                .Where(l => l.MEETING_GROUP_AUTO_ID == mgEntity.AUTO_ID
                         && l.ROUND == currentRound
                         && !l.IS_DELETED)
                .Select(l => l.EMP_AUTO_ID)
                .Distinct()
                .ToList();

            var validDrawnIdsInThisRound = drawnIdsInThisRound
                .Intersect(currentActiveMemberList.Select(m => m.EMP_AUTO_ID))
                .ToList();

            // 8. 計算「尚未在本回合抽到」的成員 (remainingMembers)
            var remainingMembers = currentActiveMemberList
                .Where(m => !validDrawnIdsInThisRound.Contains(m.EMP_AUTO_ID))
                .ToList();

            if (!remainingMembers.Any())
            {
                // 已抽完或只剩講師（已被移除）
                // 取「最後一筆抽籤紀錄 (非講師)」來當最後抽到的人
                var lastDrawnLog = db.RCS_CALL_LOG
                    .Where(l => l.MEETING_GROUP_AUTO_ID == mgEntity.AUTO_ID && !l.IS_DELETED)
                    .OrderByDescending(l => l.CREATE_TIME)
                    .ToList()  // 先拉出來再一筆筆檢查
                    .FirstOrDefault(log =>
                    {
                        // 取出 log 對應的員工 (非講師)
                        var empName = db.RCS_EMPLOYEES
                            .Where(e => e.AUTO_ID == log.EMP_AUTO_ID && !e.IS_DELETED)
                            .Select(e => e.EMP_NAME)
                            .FirstOrDefault();
                        return !string.IsNullOrEmpty(empName) && empName != meeting.LECTURER;
                    });

                string lastDrawnEmpName = null;
                if (lastDrawnLog != null)
                {
                    lastDrawnEmpName = db.RCS_EMPLOYEES
                        .Where(e => e.AUTO_ID == lastDrawnLog.EMP_AUTO_ID && !e.IS_DELETED)
                        .Select(e => e.EMP_NAME)
                        .FirstOrDefault();
                }

                Log.Warning("群組: {groupName}, 第{round}回合, 目前尚未被抽的人數為0 (可能全部抽完或只剩講師)", groupName, currentRound);

                return Json(new
                {
                    success = false,
                    message = $"{groupName} 無成員可以顯示，已抽取完畢或只剩講師！",
                    allDrawn = true,
                    currentRound,
                    lastDrawnMember = lastDrawnEmpName
                });
            }

            // 9. 若前端有指定 "selectedMember"，先嘗試找
            long? selectedEmpId = null;
            if (!string.IsNullOrEmpty(selectedMember))
            {
                selectedEmpId = (
                    from mem in remainingMembers
                    join e in db.RCS_EMPLOYEES on mem.EMP_AUTO_ID equals e.AUTO_ID
                    where !e.IS_DELETED && e.EMP_NAME == selectedMember
                    select (long?)mem.EMP_AUTO_ID
                ).FirstOrDefault();
            }

            // 若找不到，就隨機抽
            RCS_MEMBER selectedMemberEntity = null;
            if (selectedEmpId.HasValue)
            {
                selectedMemberEntity = remainingMembers
                    .FirstOrDefault(m => m.EMP_AUTO_ID == selectedEmpId.Value);
            }

            if (selectedMemberEntity == null)
            {
                Random rnd = new Random();
                selectedMemberEntity = remainingMembers[rnd.Next(remainingMembers.Count)];
            }

            // 10. 再檢查一次，該成員是否已在本回合抽到
            bool alreadyDrawnInThisRound = db.RCS_CALL_LOG.Any(l =>
                l.MEETING_GROUP_AUTO_ID == mgEntity.AUTO_ID
                && l.ROUND == currentRound
                && l.EMP_AUTO_ID == selectedMemberEntity.EMP_AUTO_ID
                && !l.IS_DELETED);

            if (alreadyDrawnInThisRound)
            {
                var empName = db.RCS_EMPLOYEES
                    .Where(e => e.AUTO_ID == selectedMemberEntity.EMP_AUTO_ID && !e.IS_DELETED)
                    .Select(e => e.EMP_NAME)
                    .FirstOrDefault();

                return Json(new
                {
                    success = false,
                    message = $"成員 {empName} 已經在回合 {currentRound} 被抽取過了。",
                    allDrawn = false
                });
            }

            // 11. 新增一筆抽籤紀錄(CALL_LOG)
            var newCallLog = new RCS_CALL_LOG
            {
                EMP_AUTO_ID = selectedMemberEntity.EMP_AUTO_ID,
                MEETING_GROUP_AUTO_ID = mgEntity.AUTO_ID,
                ROUND = currentRound,

                AUTO_GUID = Guid.NewGuid(),
                CREATE_BY = Session["Permission"]?.ToString() ?? "System",
                CREATE_TIME = DateTime.Now,
                MODIFY_BY = Session["Permission"]?.ToString() ?? "System",
                MODIFY_TIME = DateTime.Now,
                IS_ACTIVED = true,
                IS_DELETED = false
            };
            db.RCS_CALL_LOG.Add(newCallLog);
            db.SaveChanges();

            // 12. 取得抽中的員工姓名
            var selectedEmpName = db.RCS_EMPLOYEES
                .Where(e => e.AUTO_ID == selectedMemberEntity.EMP_AUTO_ID && !e.IS_DELETED)
                .Select(e => e.EMP_NAME)
                .FirstOrDefault();

            // 13. 判斷本回合是否已全部抽完
            //     - 重新撈目前有效成員 (排除講師)
            var activeMemberIdsNow = db.RCS_MEMBER
                .Where(m => m.MEETING_GROUP_AUTO_ID == mgEntity.AUTO_ID && m.IS_ACTIVED)
                .Select(m => m.EMP_AUTO_ID)
                .ToList();

            if (lecturerEmpId != 0)
            {
                // 排除講師的 ID
                activeMemberIdsNow = activeMemberIdsNow
                    .Where(id => id != lecturerEmpId)
                    .ToList();
            }

            var drawnIdsThisRoundNow = db.RCS_CALL_LOG
                .Where(l => l.MEETING_GROUP_AUTO_ID == mgEntity.AUTO_ID
                         && l.ROUND == currentRound
                         && !l.IS_DELETED)
                .Select(l => l.EMP_AUTO_ID)
                .Distinct()
                .ToList();

            // 只計入還在群組的成員
            var validDrawnCountNow = drawnIdsThisRoundNow.Intersect(activeMemberIdsNow).Count();
            bool allDrawn = (validDrawnCountNow == activeMemberIdsNow.Count);

            Log.Information("抽籤結束,會議Guid: {guid}, 組別: {groupName}, 回合: {round}, 抽中: 員工流水號={empId}, 姓名={empName}, 是否抽完該回合={allDrawn}",
                guid, groupName, currentRound, selectedMemberEntity.EMP_AUTO_ID, selectedEmpName, allDrawn);

            // 14. 回傳 JSON 結果
            return Json(new
            {
                success = true,
                selectedMember = selectedEmpName,
                currentRound,
                allDrawn,
                lastDrawnMember = selectedEmpName // 最後抽到的人（絕非講師，因為前面已移除）
            });
        }




    }
}
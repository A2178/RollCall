using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using RollCall.Models;

namespace RollCall.Controllers
{
    public class RaffleController : Controller
    {
        private TEST_RollCallDBEntities db = new TEST_RollCallDBEntities();
        // GET: Raffle
        [HttpGet]
        public ActionResult Index(Guid guid)
        {
            // 根據 AUTO_GUID 查找會議
            var meeting = db.RCS_MEETING.FirstOrDefault(m => m.AUTO_GUID == guid);
            if (meeting == null)
            {
                return HttpNotFound("找不到會議");
            }

            // 獲取與會議相關的成員列表
            var members = db.RCS_MEMBER.Where(m => m.MEETING_AUTO_ID == meeting.AUTO_ID).ToList();

            // 創建 ViewModel 並傳遞會議資料和成員列表
            var viewModel = new RaffleViewModel
            {
                MeetingName = meeting.MEETING_NAME,
                MeetingStart = meeting.MEETING_START,
                MeetingEnd = meeting.MEETING_END,
                CurrentTime = DateTime.Now,
                MeetingGuid = meeting.AUTO_GUID,  // 傳遞會議的 GUID
                Members = members
            };

            return View(viewModel);
        }


        // 點擊按鈕後進行抽籤並寫入資料庫
        [HttpPost]
        public ActionResult Draw(Guid guid)
        {
            // 根據 AUTO_GUID 查找會議
            var meeting = db.RCS_MEETING.FirstOrDefault(m => m.AUTO_GUID == guid);
            if (meeting == null)
            {
                return HttpNotFound("找不到會議");
            }
            
            long meetingId = meeting.AUTO_ID;

            // 獲取當前會議的成員列表 (從 RCS_MEMBER 表中獲取成員)
            var members = db.RCS_MEMBER.Where(m => m.MEETING_AUTO_ID == meetingId).ToList();

            if (members.Count == 0)
            {
                TempData["Message"] = "無成員可以顯示";
                return RedirectToAction("Index", new { guid = guid });
            }

            // 獲取當前輪次 (基於 RCS_CALL_LOG)
            int currentRound = db.RCS_CALL_LOG
                                 .Where(c => c.RCS_MEMBER.MEETING_AUTO_ID == meetingId)
                                 .Select(c => c.ROUND)
                                 .DefaultIfEmpty(1)
                                 .Max();

            // 獲取當前輪次中已經抽中的成員
            var alreadyDrawnInCurrentRound = db.RCS_CALL_LOG
                                               .Where(c => c.RCS_MEMBER.MEETING_AUTO_ID == meetingId && c.ROUND == currentRound)
                                               .Select(c => c.MEMBER_AUTO_ID)
                                               .ToList();

            // 如果所有成員都已經被抽到，增加回合數並設定 TempData["AllDrawn"] = true
            if ((alreadyDrawnInCurrentRound.Count) == members.Count)
            {
                currentRound++;
                alreadyDrawnInCurrentRound.Clear();
                TempData["AllDrawn"] = true;  // 設定所有成員都抽完
            }
            else
            {
                TempData["AllDrawn"] = false;  // 成員還沒有抽完
            }

            // 獲取未被抽到的成員
            var remainingMembers = members.Where(m => !alreadyDrawnInCurrentRound.Contains(m.AUTO_ID)).ToList();

            if (!remainingMembers.Any())
            {
                TempData["Message"] = "所有成員都已被抽取完畢，進入下一輪。";
                return RedirectToAction("Index", new { guid = guid });
            }

            // 隨機選擇一個成員
            var random = new Random();
            var selectedMember = remainingMembers[random.Next(remainingMembers.Count)];

            // 保存抽籤結果
            var callLog = new RCS_CALL_LOG
            {
                MEMBER_AUTO_ID = selectedMember.AUTO_ID,
                ROUND = currentRound,
                AUTO_GUID = Guid.NewGuid(),
                CREATE_BY = "DefaultUser",  // 替換為當前用戶
                CREATE_TIME = DateTime.Now,
                MODIFY_BY = "DefaultUser",
                MODIFY_TIME = DateTime.Now,
                IS_ACTIVED = true,
                IS_DELETED = false
            };

            db.RCS_CALL_LOG.Add(callLog);
            db.SaveChanges();

            // 使用 TempData 存儲當前抽到的成員名稱和回合數
            TempData["SelectedMemberName"] = selectedMember.MEMBER_NAME;
            TempData["CurrentRound"] = currentRound;

            return RedirectToAction("Index", new { guid = guid });
        }

    }
}
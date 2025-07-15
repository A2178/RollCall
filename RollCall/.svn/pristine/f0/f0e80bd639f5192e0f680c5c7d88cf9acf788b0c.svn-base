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

        [HttpGet]
        public ActionResult Index(Guid guid)
        {
            // 根據 AUTO_GUID 查找會議
            RCS_MEETING meeting = db.RCS_MEETING.FirstOrDefault(m => m.AUTO_GUID == guid);
            if (meeting == null)
            {
                return HttpNotFound("找不到會議");
            }

            // 根據會議的AUTO_ID查出對應的會議成員
            List<RCS_MEMBER> members = db.RCS_MEMBER.Where(m => m.MEETING_AUTO_ID == meeting.AUTO_ID).ToList();

            // 建立Viewmodel並指派對應的會議資料
            RaffleViewModel viewModel = new RaffleViewModel
            {
                MeetingName = meeting.MEETING_NAME,
                MeetingStart = meeting.MEETING_START,
                MeetingEnd = meeting.MEETING_END,
                CurrentTime = DateTime.Now,
                MeetingGuid = meeting.AUTO_GUID,
                Members = members
            };

            ViewBag.MeetingName = meeting.MEETING_NAME;  // 把會議名稱指派給ViewBag

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult Draw(Guid guid)
        {
            // 根據AUTO_GUID找出對應的會議
            RCS_MEETING meeting = db.RCS_MEETING.FirstOrDefault(m => m.AUTO_GUID == guid);

            // 清除之前的TempData
            TempData["TestAlertMessage"] = null;
            TempData["Message"] = "";
            TempData["AllDrawn"] = null;
            TempData["SelectedMemberName"] = null;
            TempData["CurrentRound"] = null;

            if (meeting == null)
            {
                return HttpNotFound("找不到會議");
            }

            DateTime currentTime = DateTime.Now;
            long meetingId = meeting.AUTO_ID;

            // 取出當前會議的所有人員
            List<RCS_MEMBER> members = db.RCS_MEMBER.Where(m => m.MEETING_AUTO_ID == meetingId).ToList();

            if (members.Count == 0)
            {
                TempData["Message"] = "無成員可以顯示";
                return RedirectToAction("Index", new { guid = guid });
            }

            int currentRound;

            if (currentTime < meeting.MEETING_START)  // 測試時段
            {
                TempData["TestAlertMessage"] = true;

                // 測試時段中使用負數回合
                currentRound = db.RCS_CALL_LOG
                                .Where(c => c.RCS_MEMBER.MEETING_AUTO_ID == meetingId && c.ROUND < 0)
                                .Select(c => c.ROUND)
                                .DefaultIfEmpty(0)
                                .Min();  // 最小負數回合

                if (currentRound == 0)  // 如果還未有負數回合，則初始化為 -1
                {
                    currentRound = -1;
                }

                // 查詢已經在當前回合中被抽取的成員
                List<long> alreadyDrawnInCurrentRound = db.RCS_CALL_LOG
                                                        .Where(c => c.RCS_MEMBER.MEETING_AUTO_ID == meetingId && c.ROUND == currentRound)
                                                        .Select(c => c.MEMBER_AUTO_ID)
                                                        .ToList();

                // 如果所有成員在當前回合都被抽取完，才進入下一個回合 (currentRound--)
                if (alreadyDrawnInCurrentRound.Count == members.Count)
                {
                    currentRound--;  // 減少回合數
                    alreadyDrawnInCurrentRound.Clear();  // 清空已抽取的成員列表
                }

                // 查詢出未被抽取的成員
                List<RCS_MEMBER> remainingMembers = members.Where(m => !alreadyDrawnInCurrentRound.Contains(m.AUTO_ID)).ToList();

                if (!remainingMembers.Any())
                {
                    TempData["Message"] = "所有成員都已被抽取完畢，進入下一回合。";
                    return RedirectToAction("Index", new { guid = guid });
                }

                // 隨機選擇一個成員
                Random random = new Random();
                RCS_MEMBER selectedMember = remainingMembers[random.Next(remainingMembers.Count)];

                // 保存抽籤結果
                RCS_CALL_LOG callLog = new RCS_CALL_LOG
                {
                    MEMBER_AUTO_ID = selectedMember.AUTO_ID,
                    ROUND = currentRound,
                    AUTO_GUID = Guid.NewGuid(),
                    CREATE_BY = "DefaultUser",
                    CREATE_TIME = DateTime.Now,
                    MODIFY_BY = "DefaultUser",
                    MODIFY_TIME = DateTime.Now,
                    IS_ACTIVED = true,
                    IS_DELETED = false
                };

                db.RCS_CALL_LOG.Add(callLog);
                db.SaveChanges();

                TempData["SelectedMemberName"] = selectedMember.MEMBER_NAME;
                TempData["CurrentRound"] = 0;
            }
            else  // 非測試時段，正常邏輯
            {
                // 如果是第一次抽籤，讓回合數從1開始
                currentRound = db.RCS_CALL_LOG
                                .Where(c => c.RCS_MEMBER.MEETING_AUTO_ID == meetingId && c.ROUND > 0)
                                .Select(c => c.ROUND)
                                .DefaultIfEmpty(1)  // 如果沒有記錄，預設為1
                                .Max();  // 正常情況下取最大回合數

                // 查詢出目前回合已經被抽取的成員
                List<long> alreadyDrawnInCurrentRound = db.RCS_CALL_LOG
                                                        .Where(c => c.RCS_MEMBER.MEETING_AUTO_ID == meetingId && c.ROUND == currentRound)
                                                        .Select(c => c.MEMBER_AUTO_ID)
                                                        .ToList();

                // 如果所有成員在當前回合都被抽取完，進入下一個回合
                if (alreadyDrawnInCurrentRound.Count == members.Count)
                {
                    currentRound++;
                    alreadyDrawnInCurrentRound.Clear();
                    TempData["AllDrawn"] = true;
                }
                else
                {
                    TempData["AllDrawn"] = false;
                }

                // 查詢出未被抽取的成員
                List<RCS_MEMBER> remainingMembers = members.Where(m => !alreadyDrawnInCurrentRound.Contains(m.AUTO_ID)).ToList();

                if (!remainingMembers.Any())
                {
                    TempData["Message"] = "所有成員都已被抽取完畢，進入下一回合。";
                    return RedirectToAction("Index", new { guid = guid });
                }

                // 隨機選擇一個成員
                Random random = new Random();
                RCS_MEMBER selectedMember = remainingMembers[random.Next(remainingMembers.Count)];

                // 保存抽籤結果
                RCS_CALL_LOG callLog = new RCS_CALL_LOG
                {
                    MEMBER_AUTO_ID = selectedMember.AUTO_ID,
                    ROUND = currentRound,
                    AUTO_GUID = Guid.NewGuid(),
                    CREATE_BY = "DefaultUser",
                    CREATE_TIME = DateTime.Now,
                    MODIFY_BY = "DefaultUser",
                    MODIFY_TIME = DateTime.Now,
                    IS_ACTIVED = true,
                    IS_DELETED = false
                };

                db.RCS_CALL_LOG.Add(callLog);
                db.SaveChanges();

                TempData["SelectedMemberName"] = selectedMember.MEMBER_NAME;
                TempData["CurrentRound"] = currentRound;
            }
            return RedirectToAction("Index", new { guid = guid });
        }
    }
}
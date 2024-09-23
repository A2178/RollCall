using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using RollCall.Models;

namespace RollCall.Controllers
{
    public class RCS_MEETING_TESTController : Controller
    {
        private TEST_RollCallDBEntities db = new TEST_RollCallDBEntities();

        // GET: RCS_MEETING_TEST
        public ActionResult Index()
        {
            return View(db.RCS_MEETING.ToList());
        }

        // GET: RCS_MEETING_TEST/Details/5
        public ActionResult Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RCS_MEETING rCS_MEETING = db.RCS_MEETING.Find(id);
            if (rCS_MEETING == null)
            {
                return HttpNotFound();
            }
            return View(rCS_MEETING);
        }

        // GET: RCS_MEETING_TEST/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: RCS_MEETING_TEST/Create
        // 若要免於大量指派 (overposting) 攻擊，請啟用您要繫結的特定屬性，
        // 如需詳細資料，請參閱 https://go.microsoft.com/fwlink/?LinkId=317598。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "AUTO_ID,AUTO_GUID,MEETING_NAME,MEETING_START,MEETING_END,REMARK,IS_ACTIVED,CREATE_BY,CREATE_TIME,MODIFY_BY,MODIFY_TIME,IS_DELETED,DELETE_BY,DELETE_TIME")] RCS_MEETING rCS_MEETING)
        {
            if (ModelState.IsValid)
            {
                db.RCS_MEETING.Add(rCS_MEETING);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(rCS_MEETING);
        }

        // GET: RCS_MEETING_TEST/Edit/5
        public ActionResult Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RCS_MEETING rCS_MEETING = db.RCS_MEETING.Find(id);
            if (rCS_MEETING == null)
            {
                return HttpNotFound();
            }
            return View(rCS_MEETING);
        }

        // POST: RCS_MEETING_TEST/Edit/5
        // 若要免於大量指派 (overposting) 攻擊，請啟用您要繫結的特定屬性，
        // 如需詳細資料，請參閱 https://go.microsoft.com/fwlink/?LinkId=317598。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "AUTO_ID,AUTO_GUID,MEETING_NAME,MEETING_START,MEETING_END,REMARK,IS_ACTIVED,CREATE_BY,CREATE_TIME,MODIFY_BY,MODIFY_TIME,IS_DELETED,DELETE_BY,DELETE_TIME")] RCS_MEETING rCS_MEETING)
        {
            if (ModelState.IsValid)
            {
                db.Entry(rCS_MEETING).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(rCS_MEETING);
        }

        // GET: RCS_MEETING_TEST/Delete/5
        public ActionResult Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RCS_MEETING rCS_MEETING = db.RCS_MEETING.Find(id);
            if (rCS_MEETING == null)
            {
                return HttpNotFound();
            }
            return View(rCS_MEETING);
        }

        // POST: RCS_MEETING_TEST/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(long id)
        {
            RCS_MEETING rCS_MEETING = db.RCS_MEETING.Find(id);
            db.RCS_MEETING.Remove(rCS_MEETING);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

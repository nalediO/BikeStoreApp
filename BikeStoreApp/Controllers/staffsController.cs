using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BikeStoreApp.Models;

namespace BikeStoreApp.Controllers
{
    public class staffsController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        public async Task<ActionResult> Index()
        {
            var staffs = db.staffs.Include(s => s.staff1).Include(s => s.store);
            return View(await staffs.ToListAsync());
        }

        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            staff staff = await db.staffs.FindAsync(id);
            if (staff == null)
            {
                return HttpNotFound();
            }
            return View(staff);
        }

        public ActionResult Create()
        {
            ViewBag.manager_id = new SelectList(db.staffs, "staff_id", "first_name");
            ViewBag.store_id = new SelectList(db.stores, "store_id", "store_name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "staff_id,first_name,last_name,email,phone,active,store_id,manager_id")] staff staff)
        {
            if (ModelState.IsValid)
            {
                db.staffs.Add(staff);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

           
            ViewBag.manager_id = new SelectList(db.staffs, "staff_id", "first_name", staff.manager_id);
            ViewBag.store_id = new SelectList(db.stores, "store_id", "store_name", staff.store_id);
            return View(staff);
        }


        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            staff staff = db.staffs.Find(id);
            if (staff == null)
            {
                return HttpNotFound();
            }
            ViewBag.manager_id = new SelectList(db.staffs, "staff_id", "first_name", staff.manager_id);
            ViewBag.store_id = new SelectList(db.stores, "store_id", "store_name", staff.store_id);
            return View(staff);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "staff_id,first_name,last_name,email,phone,active,store_id,manager_id")] staff staff)
        {
            if (ModelState.IsValid)
            {
                db.Entry(staff).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.manager_id = new SelectList(db.staffs, "staff_id", "first_name", staff.manager_id);
            ViewBag.store_id = new SelectList(db.stores, "store_id", "store_name", staff.store_id);
            return View(staff);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            staff staff = db.staffs.Find(id);
            if (staff == null)
            {
                return HttpNotFound();
            }
            return View(staff);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var staff = await db.staffs.FindAsync(id);
            if (staff == null)
            {
                return HttpNotFound();
            }
            var subordinates = db.staffs.Where(s => s.manager_id == id).ToList();

            if (subordinates.Any())
            {
                TempData["ErrorMessage"] = "This staff member cannot be deleted because they manage other employees. Reassign or remove their subordinates first.";
                return RedirectToAction("Index");

            }

            db.staffs.Remove(staff);
            await db.SaveChangesAsync();
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeOtomasyon.Data;
using CafeOtomasyon.Models;

namespace CafeOtomasyon.Controllers
{
    public class TablesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TablesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. ESKİ MASA YÖNETİMİ (LİSTELE, EKLE, SİL)
        // ==========================================

        // GET: Tables (Masa Listesi)
        public async Task<IActionResult> Index()
        {
            return View(await _context.Tables.ToListAsync());
        }

        // GET: Tables/Create (Yeni Masa Ekleme Sayfası)
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tables/Create (Kaydet)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TableModel tableModel)
        {
            if (ModelState.IsValid)
            {
                // Varsayılan değerler
                tableModel.Status = TableStatus.Boş;
                tableModel.IsOccupied = false;

                _context.Add(tableModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tableModel);
        }

        // GET: Tables/Edit/5 (Düzenle)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var tableModel = await _context.Tables.FindAsync(id);
            if (tableModel == null) return NotFound();
            return View(tableModel);
        }

        // POST: Tables/Edit/5 (Güncelle)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TableModel tableModel)
        {
            if (id != tableModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tableModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Tables.Any(e => e.Id == tableModel.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tableModel);
        }

        // GET: Tables/Delete/5 (Silme Onay)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var table = await _context.Tables.FirstOrDefaultAsync(m => m.Id == id);
            if (table == null) return NotFound();
            return View(table);
        }

        // POST: Tables/Delete/5 (Sil)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table != null)
            {
                _context.Tables.Remove(table);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // 2. YENİ GÖRSEL KAT PLANI (FLOOR PLAN)
        // ==========================================

        public IActionResult FloorPlan()
        {
            var tables = _context.Tables.AsNoTracking().ToList();
            foreach (var t in tables)
            {
                if (t.IsOccupied) t.Status = TableStatus.Dolu;
                else if (t.Status != TableStatus.Rezerve) t.Status = TableStatus.Boş;
            }
            var grouped = tables.GroupBy(t => t.Location ?? "Genel Alan").ToList();
            return View(grouped);
        }

        // API: Masa Detaylarını Getir
        [HttpGet]
        public async Task<IActionResult> GetTableDetails(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.TableModelId == id && o.IsPaid == false);

            bool isCustomerTable = !string.IsNullOrEmpty(table.UserId);

            if (order != null)
            {
                return Json(new
                {
                    success = true,
                    orderId = order.Id,
                    totalAmount = order.TotalAmount,
                    status = "Dolu",
                    isCustomerTable = isCustomerTable,
                    details = order.OrderDetails.Select(d => new
                    {
                        detailId = d.Id,
                        productName = d.Product != null ? d.Product.Name : "Silinmiş",
                        quantity = d.Quantity,
                        price = d.Price,
                        total = d.Quantity * d.Price
                    })
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    status = table.Status == TableStatus.Rezerve ? "Rezerve" : "Boş",
                    totalAmount = 0,
                    isCustomerTable = isCustomerTable
                });
            }
        }

        // API: Masayı Dolu Yap
        [HttpPost]
        public async Task<IActionResult> StartOrder(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null) return Json(new { success = false });

            if (!_context.Orders.Any(o => o.TableModelId == id && !o.IsPaid))
            {
                var order = new OrderModel
                {
                    TableModelId = id,
                    OpenTime = DateTime.Now,
                    IsPaid = false,
                    TotalAmount = 0
                };
                _context.Orders.Add(order);
                table.IsOccupied = true;
                table.Status = TableStatus.Dolu;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        // API: Masayı Boşalt
        [HttpPost]
        public async Task<IActionResult> CloseOrder(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null) return Json(new { success = false });

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TableModelId == id && !o.IsPaid);
            if (order != null) _context.Orders.Remove(order);

            table.IsOccupied = false;
            table.Status = TableStatus.Boş;
            table.UserId = null;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // API: Rezerve Et
        [HttpPost]
        public async Task<IActionResult> ReserveTable(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null) return Json(new { success = false });

            table.Status = TableStatus.Rezerve;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }



        // --- 1. MÜŞTERİ: YARDIM İSTE (Müşteri Butonuna Basınca Çalışır) ---
        [HttpPost]
        public async Task<IActionResult> RequestHelp(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null) return Json(new { success = false });

            table.IsHelpRequested = true; // Yardım istendi
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // --- 2. GARSON: YARDIMI KAPAT (Bildirimi Sil) ---
        [HttpPost]
        public async Task<IActionResult> ResolveHelp(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null) return Json(new { success = false });

            table.IsHelpRequested = false; // Yardım çözüldü
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // --- 3. SİSTEM: YARDIM İSTEYEN MASALARI KONTROL ET (Otomatik Çalışacak) ---
        [HttpGet]
        public async Task<IActionResult> CheckNotifications()
        {
            // Yardım isteyen masaların ID ve İsimlerini getir
            var helpTables = await _context.Tables
                .Where(t => t.IsHelpRequested)
                .Select(t => new { t.Id, t.Name })
                .ToListAsync();

            return Json(helpTables);
        }
    }
}
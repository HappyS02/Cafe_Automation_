using CafeOtomasyon.Data;
using CafeOtomasyon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics; // Loglama için
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace CafeOtomasyon.Controllers
{
    [Authorize] // Varsayılan: Giriş yapmış herkes erişebilir (Garson dahil)
    public class TablesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TablesController(ApplicationDbContext context)
        {
            _context = context;
        }



        // GET: Tables/FloorPlan (Görsel Kat Planını Gösterir)
        public async Task<IActionResult> FloorPlan()
        {
            // Veritabanındaki tüm masaları çekiyoruz,
            // Önce Konuma, sonra Masa Adına göre sıralıyoruz.
            var tables = await _context.Tables
                .OrderBy(t => t.Location)
                .ThenBy(t => t.Name) // Req #1: Masa 1, Masa 2 diye sırala
                .ToListAsync();

            // Masaları Konum'a göre grupluyoruz (Req #2)
            var groupedTables = tables.GroupBy(t => t.Location);

            // Gruplanmış veriyi View'a gönderiyoruz
            return View(groupedTables);
        }




        // GET: Tables (Listeleme ve Arama)
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var tables = from t in _context.Tables
                         select t;

            if (!string.IsNullOrEmpty(searchString))
            {
                tables = tables.Where(t => t.Name.Contains(searchString)
                                        || t.Location.Contains(searchString));
            }

            return View(await tables.AsNoTracking().ToListAsync());
        }

        // GET: Tables/Create (Yeni Masa Ekleme Formunu Gösterir)
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tables/Create (Yeni Masayı Kaydeder)
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Location,Capacity,Status")] TableModel tableModel)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tableModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tableModel);
        }

        // GET: Tables/Edit/5 (Düzenleme Formunu Gösterir)
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tableModel = await _context.Tables.FindAsync(id);
            if (tableModel == null)
            {
                return NotFound();
            }
            return View(tableModel);
        }

        // POST: Tables/Edit/5 (Düzenlenen Formu Kaydeder)
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Location,Capacity,Status")] TableModel tableModel)
        {
            if (id != tableModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tableModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Tables.Any(e => e.Id == tableModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tableModel);
        }

        // GET: Tables/Delete/5 (Silme Onay Sayfasını Gösterir)
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tableModel = await _context.Tables
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (tableModel == null)
            {
                return NotFound();
            }

            return View(tableModel);
        }

        // POST: Tables/Delete/5 (Masayı Siler)
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tableModel = await _context.Tables.FindAsync(id);
            if (tableModel != null)
            {
                _context.Tables.Remove(tableModel);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }







        [HttpGet] // Bu bir GET isteğidir
        public async Task<IActionResult> GetTableDetails(int id)
        {
            // Masayı ilişkili sipariş detayları ve ürünlerle birlikte getir
            var table = await _context.Tables
                                        .FirstOrDefaultAsync(t => t.Id == id);

            if (table == null) return NotFound();

            int activeOrderId = 0;
            decimal currentTotal = 0;
            var orderDetails = new List<object>();

            // Eğer masa doluysa ve bir sipariş ID'si varsa, o siparişi bulmaya çalış
            if (table.Status == TableStatus.Dolu && table.CurrentOrderId.HasValue)
            {
                var activeOrder = await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(d => d.Product)
                    .FirstOrDefaultAsync(o => o.Id == table.CurrentOrderId.Value && !o.IsPaid);

                if (activeOrder != null)
                {
                    activeOrderId = activeOrder.Id;
                    currentTotal = activeOrder.TotalAmount;
                    orderDetails = activeOrder.OrderDetails.Select(d => new
                    {
                        orderDetailId = d.Id, // DETAY ID'SİNİ DE GÖNDERELİM (Silme/Güncelleme için lazım)
                        productName = d.Product?.Name ?? "Silinmiş Ürün", // Ürün silinmişse diye kontrol
                        quantity = d.Quantity,
                        price = d.Price,
                        total = d.Quantity * d.Price
                    }).ToList<object>();
                }
                else
                {
                    // Veri tutarsızlığı: Masa dolu ama geçerli sipariş yok. Masayı boşa alabiliriz.
                    Debug.WriteLine($"Tutarsızlık: Masa {table.Id} dolu ama OrderId {table.CurrentOrderId} bulunamadı veya ödenmiş.");
                    // table.Status = TableStatus.Boş;
                    // table.CurrentOrderId = null;
                    // _context.Update(table);
                    // await _context.SaveChangesAsync();
                    // Şimdilik sadece boş dönelim.
                }
            }

            // Her durumda masanın mevcut durumunu ve diğer bilgileri JSON olarak dön
            return Json(new
            {
                tableId = table.Id,
                tableName = table.Name,
                status = table.Status.ToString(),
                orderId = activeOrderId, // 0 veya geçerli ID dönecek
                details = orderDetails,
                totalAmount = currentTotal
            });
        }




        // Req #3 & #4: Masayı 'Dolu' yapar ve otomatik tarih kaydıyla yeni bir sipariş başlatır
        [HttpPost]
        public async Task<IActionResult> StartOrder(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null) return NotFound();

            if (table.Status == TableStatus.Boş)
            {
                try
                {
                    // 1. Yeni siparişi oluştur
                    var newOrder = new OrderModel
                    {
                        TableModelId = table.Id,
                        OpenTime = DateTime.Now,
                        IsPaid = false,
                        TotalAmount = 0
                    };
                    _context.Orders.Add(newOrder);
                    await _context.SaveChangesAsync(); // Sipariş ID'si burada oluşur

                    // KONTROL LOG 1: Sipariş ID'si oluştu mu?
                    Debug.WriteLine($"Yeni Sipariş Oluşturuldu: ID = {newOrder.Id} Masa ID = {table.Id}");

                    if (newOrder.Id <= 0)
                    {
                        Debug.WriteLine("HATA: Sipariş ID'si oluşturulamadı!");
                        return Json(new { success = false, message = "Sipariş ID'si oluşturulamadı." });
                    }

                    // 2. Masayı güncelle
                    table.Status = TableStatus.Dolu;
                    table.CurrentOrderId = newOrder.Id;
                    _context.Tables.Update(table);
                    await _context.SaveChangesAsync(); // Masa güncellemesini kaydet

                    // KONTROL LOG 2: Masa güncellendi mi?
                    Debug.WriteLine($"Masa Güncellendi: ID = {table.Id} Yeni CurrentOrderId = {table.CurrentOrderId}");

                    // Başarı JSON'ını yeni ID ile dön
                    return Json(new { success = true, newStatus = "Dolu", orderId = newOrder.Id });
                }
                catch (Exception ex)
                {
                    // KONTROL LOG 3: Veritabanı hatası oldu mu?
                    Debug.WriteLine($"StartOrder HATA: {ex.Message}");
                    return Json(new { success = false, message = "Sipariş başlatılırken bir veritabanı hatası oluştu." });
                }
            }

            Debug.WriteLine($"StartOrder Başarısız: Masa {id} zaten boş değil. Durum: {table.Status}");
            return Json(new { success = false, message = "Masa zaten dolu veya rezerve." });
        }

        // Req #3, #6, #7: Masayı 'Boş' yapar, mevcut siparişi kapatır VEYA rezervasyonu iptal eder
        [HttpPost]
        public async Task<IActionResult> CloseOrder(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null) return NotFound();

            // --- DEĞİŞİKLİK BAŞLANGICI ---

            // 1. Durum 'Dolu' ise: Siparişi kapat ve masayı boşa al
            if (table.Status == TableStatus.Dolu && table.CurrentOrderId != null)
            {
                var order = await _context.Orders.FindAsync(table.CurrentOrderId);
                if (order != null)
                {
                    order.CloseTime = DateTime.Now;
                    order.IsPaid = true; // (Normalde ödeme sonrası bu yapılır)
                    _context.Orders.Update(order);
                }

                table.Status = TableStatus.Boş;
                table.CurrentOrderId = null;
                _context.Tables.Update(table);

                await _context.SaveChangesAsync();
                return Json(new { success = true, newStatus = "Boş" });
            }

            // 2. Durum 'Rezerve' ise: Sadece masayı boşa al (Rezervasyonu iptal et)
            if (table.Status == TableStatus.Rezerve)
            {
                table.Status = TableStatus.Boş;
                // CurrentOrderId zaten null olmalı, dokunmuyoruz
                _context.Tables.Update(table);

                await _context.SaveChangesAsync();
                return Json(new { success = true, newStatus = "Boş" });
            }

            // --- DEĞİŞİKLİK BİTTİ ---

            // Masa zaten 'Boş' ise veya beklenmedik bir durumdaysa
            return Json(new { success = false, message = "Masa zaten boş." });
        }

        // Req #3: Masayı 'Rezerve' yapar
        [HttpPost]
        public async Task<IActionResult> ReserveTable(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null) return NotFound();

            table.Status = TableStatus.Rezerve;
            table.CurrentOrderId = null; // Rezerve masanın aktif siparişi olmaz

            _context.Tables.Update(table);
            await _context.SaveChangesAsync();

            return Json(new { success = true, newStatus = "Rezerve" });
        }








    }
}
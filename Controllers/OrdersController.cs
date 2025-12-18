using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeOtomasyon.Data;
using CafeOtomasyon.Models;

namespace CafeOtomasyon.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- 1. AKTİF SİPARİŞLER ---
        public async Task<IActionResult> ActiveOrders(string searchString)
        {
            var orders = _context.Orders
                .Include(o => o.Table)
                .Where(o => o.IsPaid == false); // Sadece açık olanlar

            if (!string.IsNullOrEmpty(searchString))
            {
                orders = orders.Where(o => o.Table.Name.Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await orders.OrderByDescending(o => o.OpenTime).ToListAsync());
        }

        // --- 2. SİPARİŞ GEÇMİŞİ (Dosya adın History.cshtml olduğu için metod adı History oldu) ---
        public async Task<IActionResult> History()
        {
            var orders = await _context.Orders
                .Include(o => o.Table)
                .Where(o => o.IsPaid == true) // Sadece ödenmişler
                .OrderByDescending(o => o.CloseTime)
                .ToListAsync();

            return View(orders); // Views/Orders/History.cshtml dosyasını açar
        }

        // --- 3. DETAY GÖRÜNTÜLEME (Çalışmayan Detay butonu için) ---
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // --- 4. ADİSYON YAZDIR (Çalışmayan Yazdır butonu için) ---
        public async Task<IActionResult> PrintReceipt(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            // Adisyon tasarımı için Views/Orders/PrintReceipt.cshtml olmalı
            return View(order);
        }

        // --- 5. ÖDEME AL ---
        [HttpPost]
        public async Task<IActionResult> MarkOrderAsPaid(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return Json(new { success = false, message = "Sipariş bulunamadı." });

            order.IsPaid = true;
            order.CloseTime = DateTime.Now;

            // Masayı Boşalt
            if (order.TableModelId != null)
            {
                var table = await _context.Tables.FindAsync(order.TableModelId);
                if (table != null)
                {
                    table.IsOccupied = false;
                    table.Status = TableStatus.Boş;
                    table.UserId = null;
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, newStatus = "Boş" });
        }






        
        // --- ÜRÜN LİSTESİNİ GETİR ---
        [HttpGet]
        public async Task<IActionResult> GetActiveProducts()
        {
            // Veritabanından aktif ürünleri çek
            var products = await _context.Products
                .Where(p => p.IsActive) // Modelinde IsActive yoksa bu satırı sil
                .Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    imageName = p.ImageName,
                    categoryName = p.Category != null ? p.Category.Name : "Diğer"
                })
                .ToListAsync();

            return Json(products);
        }


        // --- MİKTAR GÜNCELLEME (ARTIR / AZALT) ---
        [HttpPost]
        public async Task<IActionResult> UpdateItemQuantity(int orderDetailId, int quantityChange)
        {
            var detail = await _context.OrderDetails.Include(d => d.Order).FirstOrDefaultAsync(d => d.Id == orderDetailId);

            // Eğer detay yoksa hata dön
            if (detail == null) return Json(new { success = false, message = "Ürün bulunamadı." });

            detail.Quantity += quantityChange;

            // Adet 0 veya altına düşerse ürünü sil
            if (detail.Quantity <= 0)
            {
                _context.OrderDetails.Remove(detail);
            }

            await _context.SaveChangesAsync();

            // Toplam tutarı güncelle
            var order = await _context.Orders.FindAsync(detail.OrderModelId);
            if (order != null)
            {
                // Veritabanından güncel toplamı hesapla
                var newTotal = _context.OrderDetails.Where(d => d.OrderModelId == order.Id).Sum(d => d.Quantity * d.Price);
                order.TotalAmount = newTotal;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // --- ÜRÜN SİLME ---
        [HttpPost]
        public async Task<IActionResult> RemoveItemFromOrder(int orderDetailId)
        {
            var detail = await _context.OrderDetails.FindAsync(orderDetailId);
            if (detail == null) return Json(new { success = false, message = "Ürün bulunamadı." });

            int orderId = detail.OrderModelId;
            _context.OrderDetails.Remove(detail);
            await _context.SaveChangesAsync();

            // Toplam tutarı güncelle
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                var newTotal = _context.OrderDetails.Where(d => d.OrderModelId == order.Id).Sum(d => d.Quantity * d.Price);
                order.TotalAmount = newTotal;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }




        // --- YARDIMCI METOD: TOPLAM TUTARI HESAPLA ---
        private async Task RecalculateOrderTotal(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                // Detaylardaki (Fiyat x Adet) toplamını al
                var newTotal = _context.OrderDetails
                    .Where(d => d.OrderModelId == orderId)
                    .Sum(d => d.Quantity * d.Price);

                order.TotalAmount = newTotal;
                await _context.SaveChangesAsync();
            }
        }

        // --- EKSİK OLAN KISIM: SİPARİŞE ÜRÜN EKLE (MODAL İÇİN) ---
        [HttpPost]
        public async Task<IActionResult> AddItemToOrder(int orderId, int productId, int quantity)
        {
            var order = await _context.Orders.FindAsync(orderId);
            var product = await _context.Products.FindAsync(productId);

            if (order == null || product == null)
                return Json(new { success = false, message = "Kayıt bulunamadı." });

            // Sipariş Detayı Ekle
            var detail = new OrderDetailModel
            {
                OrderModelId = orderId,
                ProductModelId = productId,
                Quantity = quantity,
                Price = product.Price
            };

            _context.OrderDetails.Add(detail);
            await _context.SaveChangesAsync();

            // Toplamı güncelle
            await RecalculateOrderTotal(orderId);

            return Json(new { success = true });
        }
    }
}
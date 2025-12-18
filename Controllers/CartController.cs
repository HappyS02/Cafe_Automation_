using CafeOtomasyon.Data;
using CafeOtomasyon.Extensions; // SessionExtensions class'ın olduğu yer
using CafeOtomasyon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CafeOtomasyon.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. MASA SEÇİMİ (GİRİŞ)
        public async Task<IActionResult> Index()
        {
            // Kullanıcının açık siparişi var mı kontrol et
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var activeOrder = await _context.Orders.Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Table.UserId == userId && !o.IsPaid);

            if (activeOrder != null)
            {
                HttpContext.Session.SetInt32("TableId", activeOrder.TableModelId ?? 0);
                return RedirectToAction("Menu", "Home");
            }

            var tables = await _context.Tables.AsNoTracking().ToListAsync();
            // Durumları ayarla
            foreach (var t in tables)
            {
                if (t.IsOccupied) t.Status = TableStatus.Dolu;
                else if (t.Status != TableStatus.Rezerve) t.Status = TableStatus.Boş;
            }
            return View(tables.GroupBy(t => t.Location ?? "Genel").ToList());
        }

        // 2. MASA SEÇİMİNİ ONAYLA
        [HttpPost]
        public async Task<IActionResult> Checkout(int tableId)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var table = await _context.Tables.FindAsync(tableId);

            if (table == null || (table.IsOccupied && table.UserId != userId))
                return RedirectToAction("Index");

            if (!table.IsOccupied)
            {
                table.IsOccupied = true;
                table.Status = TableStatus.Dolu;
                table.UserId = userId;

                var newOrder = new OrderModel
                {
                    TableModelId = tableId,
                    OpenTime = DateTime.Now,
                    IsPaid = false,
                    TotalAmount = 0
                };
                _context.Orders.Add(newOrder);
                await _context.SaveChangesAsync();
            }

            HttpContext.Session.SetInt32("TableId", tableId);
            return RedirectToAction("Menu", "Home");
        }

        // 3. SEPET VERİSİNİ GETİR (HEM BEKLEYEN HEM ONAYLANAN)
        [HttpGet]
        public async Task<IActionResult> GetCartPreview()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // A. Veritabanındaki (Mutfağa Gitmiş) Siparişler
            var dbOrder = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Table.UserId == userId && !o.IsPaid);

            var sentItems = new List<object>();
            string tableName = "Masa Seçilmedi";
            decimal totalAmount = 0;

            if (dbOrder != null)
            {
                tableName = dbOrder.Table.Name;
                totalAmount = dbOrder.TotalAmount; // DB'deki tutar
                sentItems = dbOrder.OrderDetails.Select(x => new {
                    productName = x.Product.Name,
                    price = x.Price,
                    quantity = x.Quantity,
                    total = x.Price * x.Quantity,
                    status = "sent" // Gönderildi
                }).ToList<object>();
            }

            // B. Session'daki (Henüz Gönderilmemiş) Sepet
            var draftCart = HttpContext.Session.GetObject<List<CartItem>>("DraftCart") ?? new List<CartItem>();
            decimal draftTotal = draftCart.Sum(x => x.Total);

            var draftItems = draftCart.Select(x => new {
                productId = x.ProductId,
                productName = x.ProductName,
                price = x.Price,
                quantity = x.Quantity,
                total = x.Total,
                status = "draft" // Taslak
            }).ToList<object>();

            return Json(new
            {
                success = true,
                tableName = tableName,
                totalAmount = totalAmount + draftTotal, // Toplam Tutar
                items = sentItems.Concat(draftItems).ToList()
            });
        }

        // 4. GEÇİCİ SEPETE EKLE/ÇIKAR (SESSION)
        [HttpPost]
        public IActionResult AddToDraft(int productId, string productName, decimal price, int change)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("DraftCart") ?? new List<CartItem>();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);

            if (item != null)
            {
                item.Quantity += change;
                if (item.Quantity <= 0) cart.Remove(item);
            }
            else if (change > 0)
            {
                cart.Add(new CartItem { ProductId = productId, ProductName = productName, Price = price, Quantity = change });
            }

            HttpContext.Session.SetObject("DraftCart", cart);
            return Json(new { success = true });
        }

        // 5. SİPARİŞİ ONAYLA (SESSION -> DB)
        [HttpPost]
        public async Task<IActionResult> ConfirmOrder()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = HttpContext.Session.GetObject<List<CartItem>>("DraftCart");

            if (cart == null || !cart.Any()) return Json(new { success = false, message = "Sepet boş." });

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Table.UserId == userId && !o.IsPaid);

            if (order == null) return Json(new { success = false, message = "Masa bulunamadı." });

            // Session'daki her ürünü DB'ye işle
            foreach (var item in cart)
            {
                var existingDetail = order.OrderDetails.FirstOrDefault(x => x.ProductModelId == item.ProductId);
                if (existingDetail != null)
                {
                    existingDetail.Quantity += item.Quantity;
                }
                else
                {
                    order.OrderDetails.Add(new OrderDetailModel
                    {
                        ProductModelId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        OrderModelId = order.Id
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Toplamı güncelle
            order.TotalAmount = _context.OrderDetails.Where(x => x.OrderModelId == order.Id).Sum(x => x.Quantity * x.Price);
            await _context.SaveChangesAsync();

            // Session'ı temizle
            HttpContext.Session.Remove("DraftCart");

            return Json(new { success = true });
        }



        // --- QR KOD İLE HIZLI GİRİŞ ---
        // Link şuna benzeyecek: /Cart/QrSelect?tableId=5
        [AllowAnonymous]
        [HttpGet]
        public IActionResult QrSelect(int tableId)
        {
            if (tableId <= 0) return RedirectToAction("Index"); // Hatalı ID ise masa seçime at

            // 1. Masayı Session'a (Hafızaya) kaydet
            // Sistem artık senin o masada oturduğunu biliyor.
            HttpContext.Session.SetInt32("TableId", tableId);

            // 2. Direkt Menüye fırlat
            return RedirectToAction("Menu", "Home");
        }
    }
}
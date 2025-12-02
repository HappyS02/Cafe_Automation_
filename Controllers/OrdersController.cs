using CafeOtomasyon.Data;
using CafeOtomasyon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System; // TimeSpan için gerekli
using Microsoft.AspNetCore.Authorization;

namespace CafeOtomasyon.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // AJAX isteği ile çağrılacak: Aktif ürün listesini JSON olarak döner
        [HttpGet]
        public async Task<IActionResult> GetActiveProducts()
        {
            var products = await _context.Products
                                        .Where(p => p.IsActive) // Sadece aktif ürünleri al
                                        .Select(p => new { p.Id, p.Name, p.Price }) // Sadece gerekli bilgileri seç
                                        .ToListAsync();
            return Json(products);
        }

        // AJAX isteği ile çağrılacak: Siparişe yeni bir ürün ekler
        [HttpPost]
        public async Task<IActionResult> AddItemToOrder(int orderId, int productId, int quantity)
        {
            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Adet 0'dan büyük olmalıdır." });
            }

            var order = await _context.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == orderId);
            var product = await _context.Products.FindAsync(productId);

            if (order == null || product == null)
            {
                return Json(new { success = false, message = "Sipariş veya ürün bulunamadı." });
            }

            // Siparişte bu ürün zaten var mı diye kontrol et
            var existingDetail = order.OrderDetails.FirstOrDefault(d => d.ProductModelId == productId);

            if (existingDetail != null)
            {
                // Varsa adedini artır
                existingDetail.Quantity += quantity;
                _context.OrderDetails.Update(existingDetail);
            }
            else
            {
                // Yoksa yeni bir detay satırı ekle
                var newDetail = new OrderDetailModel
                {
                    OrderModelId = orderId,
                    ProductModelId = productId,
                    Quantity = quantity,
                    Price = product.Price // Ürünün GÜNCEL fiyatını al
                };
                _context.OrderDetails.Add(newDetail);
            }

            // Siparişin toplam tutarını GÜNCELLE
            // Önce SaveChangesAsync ile yeni eklenen ürünün ID'sinin oluşmasını bekle
            await _context.SaveChangesAsync();

            // Şimdi tüm detayları (yeni eklenen dahil) tekrar çek ve toplamı hesapla
            var updatedOrderDetails = await _context.OrderDetails
                                            .Where(d => d.OrderModelId == orderId)
                                            .ToListAsync();
            order.TotalAmount = updatedOrderDetails.Sum(d => d.Quantity * d.Price);
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return Json(new { success = true, newTotalAmount = order.TotalAmount });
        }



        // Req #6: Siparişteki bir ürünün adedini değiştirir (artırma/azaltma)
        [HttpPost]
        public async Task<IActionResult> UpdateItemQuantity(int orderDetailId, int quantityChange)
        {
            var orderDetail = await _context.OrderDetails.FindAsync(orderDetailId);
            if (orderDetail == null) return NotFound();

            orderDetail.Quantity += quantityChange; // Adedi artır veya azalt

            // Adet 0'a veya altına düşerse, ürünü sil
            if (orderDetail.Quantity <= 0)
            {
                _context.OrderDetails.Remove(orderDetail);
            }
            else
            {
                _context.OrderDetails.Update(orderDetail);
            }
            await _context.SaveChangesAsync();

            // Siparişin ana toplamını yeniden hesapla
            await RecalculateOrderTotal(orderDetail.OrderModelId);

            return Json(new { success = true });
        }

        // Req #6: Siparişten bir ürünü tamamen siler
        [HttpPost]
        public async Task<IActionResult> RemoveItemFromOrder(int orderDetailId)
        {
            var orderDetail = await _context.OrderDetails.FindAsync(orderDetailId);
            if (orderDetail == null) return NotFound();

            int orderId = orderDetail.OrderModelId; // ID'yi silmeden önce kaydet
            _context.OrderDetails.Remove(orderDetail);
            await _context.SaveChangesAsync();

            // Siparişin ana toplamını yeniden hesapla
            await RecalculateOrderTotal(orderId);

            return Json(new { success = true });
        }


        // AJAX isteği ile çağrılacak: Siparişi 'Ödendi' olarak işaretler ve masayı boşa alır
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        [HttpPost]
        public async Task<IActionResult> MarkOrderAsPaid(int orderId)
        {
            // 1. Siparişi bul (Detaylara gerek yok)
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Sipariş bulunamadı." });
            }

            // Zaten ödenmişse bir şey yapma (isteğe bağlı)
            if (order.IsPaid)
            {
                // Veya zaten ödenmişse masayı boşa almayı garanti edebiliriz
                var maybeTable = await _context.Tables.FirstOrDefaultAsync(t => t.Id == order.TableModelId);
                if (maybeTable != null && maybeTable.CurrentOrderId == order.Id)
                {
                    maybeTable.Status = TableStatus.Boş;
                    maybeTable.CurrentOrderId = null;
                    _context.Tables.Update(maybeTable);
                    await _context.SaveChangesAsync();
                    Debug.WriteLine($"MarkAsPaid: Zaten ödenmiş sipariş ({orderId}) için masa ({maybeTable.Id}) boşa alındı.");
                }
                return Json(new { success = true, newStatus = "Boş", message = "Sipariş zaten ödenmiş." });
            }

            // 2. Siparişi güncelle
            order.IsPaid = true;         // Ödendi olarak işaretle
            order.CloseTime = DateTime.Now; // Kapanış saatini kaydet (Req #7 için)
            _context.Orders.Update(order);
            Debug.WriteLine($"Sipariş ({orderId}) ödendi olarak işaretlendi. Kapanış: {order.CloseTime}");


            // 3. İlişkili masayı bul ve güncelle (Req #6)
            var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == order.TableModelId);
            if (table != null)
            {
                // Sadece bu sipariş hala masanın aktif siparişiyse masayı boşa al
                if (table.CurrentOrderId == order.Id)
                {
                    table.Status = TableStatus.Boş;
                    table.CurrentOrderId = null;
                    _context.Tables.Update(table);
                    Debug.WriteLine($"Masa ({table.Id}) sipariş ({orderId}) ödemesi sonrası boşa alındı.");
                }
                else
                {
                    Debug.WriteLine($"Masa ({table.Id}) zaten başka bir siparişte ({table.CurrentOrderId}) veya boş, durum değiştirilmedi.");
                }
            }
            else
            {
                Debug.WriteLine($"UYARI: Sipariş ({orderId}) için ilişkili masa ({order.TableModelId}) bulunamadı!");
            }

            // 4. Tüm değişiklikleri kaydet
            await _context.SaveChangesAsync();

            // Başarı mesajı dön (artık masanın durumu 'Boş')
            return Json(new { success = true, newStatus = "Boş" });
        }


        // === YARDIMCI METOT ===
        // Siparişin toplam tutarını yeniden hesaplayan özel bir metot
        private async Task RecalculateOrderTotal(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return;

            var updatedOrderDetails = await _context.OrderDetails
                                            .Where(d => d.OrderModelId == orderId)
                                            .ToListAsync();

            order.TotalAmount = updatedOrderDetails.Sum(d => d.Quantity * d.Price);
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }




        // GET: Orders/History (Ödenmiş Sipariş Geçmişini Gösterir)
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        public async Task<IActionResult> History()
        {
            // Veritabanından sadece ödenmiş siparişleri çekiyoruz.
            // İlişkili Masa bilgisini de alıyoruz (Include).
            // En yeniden eskiye doğru sıralıyoruz (OrderByDescending).
            var paidOrders = await _context.Orders
                .Where(o => o.IsPaid == true && o.CloseTime.HasValue) // Sadece ödenmiş ve kapanış saati olanlar
                .Include(o => o.Table) // İlişkili masayı getir
                .OrderByDescending(o => o.CloseTime) // En son kapanandan başla
                .ToListAsync();

            // Bu listeyi History.cshtml adlı View'a gönderiyoruz
            return View(paidOrders);
        }


        // GET: Orders/ActiveOrders (Aktif, Ödenmemiş Siparişleri Listeler)
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        public async Task<IActionResult> ActiveOrders(string searchString = null)
        {
            ViewData["CurrentFilter"] = searchString;

            // 1. Temel sorguyu IQueryable olarak başlat
            IQueryable<OrderModel> query = _context.Orders
                                            .Where(o => o.IsPaid == false)
                                            .Include(o => o.Table);

            // 2. Arama metni varsa filtrelemeyi uygula
            if (!string.IsNullOrEmpty(searchString))
            {
                // .Where sonucu yine IQueryable olduğu için sorun yok
                query = query.Where(o => o.Table != null && o.Table.Name.Contains(searchString));
            }

            // 3. Filtreleme bittikten SONRA sıralamayı uygula
            var orderedQuery = query.OrderByDescending(o => o.OpenTime);

            // 4. Sonucu listeye çevir
            var activeOrders = await orderedQuery.ToListAsync();
            // Not: AsNoTracking()'i burada da ekleyebilirsiniz: await orderedQuery.AsNoTracking().ToListAsync();

            return View(activeOrders);
        }



        // GET: Orders/Details/5 (Belirli bir siparişin detaylarını gösterir)
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound(); // ID gelmediyse hata ver
            }

            // Siparişi, ilişkili Masası, Detayları ve Detayların Ürünleri ile birlikte çek
            var orderModel = await _context.Orders
                .Include(o => o.Table)          // İlişkili masayı getir
                .Include(o => o.OrderDetails)   // Sipariş detaylarını (satırları) getir
                    .ThenInclude(od => od.Product) // Her detayın ürün bilgisini getir
                .AsNoTracking()                 // Sadece okuma yapacağımız için izlemeye gerek yok
                .FirstOrDefaultAsync(m => m.Id == id); // Belirtilen ID'deki siparişi bul

            if (orderModel == null)
            {
                return NotFound(); // Sipariş bulunamadıysa hata ver
            }

            // Bulunan sipariş modelini Details.cshtml adlı View'a gönder
            return View(orderModel);
        }


        // GET: Orders/PrintReceipt/5 (Belirli bir siparişin adisyonunu yazdırma için hazırlar)
        // Yönetici veya Kasiyer erişebilir
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        public async Task<IActionResult> PrintReceipt(int id) // OrderId'yi parametre olarak alır
        {
            // Siparişi, ilişkili Masası, Detayları ve Detayların Ürünleri ile birlikte çek
            // Tıpkı Details metodundaki gibi, tam detayı alıyoruz.
            var orderModel = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (orderModel == null)
            {
                return NotFound(); // Sipariş bulunamadıysa hata ver
            }

            // Eğer sipariş henüz ödenmemişse bile adisyon yazdırılabilir.
            // Sipariş modelini PrintReceipt.cshtml adlı özel View'a gönder
            return View(orderModel);
        }
    }
}
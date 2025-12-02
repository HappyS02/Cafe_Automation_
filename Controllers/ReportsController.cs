using CafeOtomasyon.Data;
using Microsoft.AspNetCore.Authorization; // Yetkilendirme için
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System; // DateTime için
using System.Linq; // LINQ metotları için
using System.Threading.Tasks; // async Task için

namespace CafeOtomasyon.Controllers
{
    // Raporlara sadece Yönetici ve Kasiyer erişebilsin
    [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reports/DailySales (Günlük Satış Raporu Sayfasını Gösterir)
        public async Task<IActionResult> DailySales(DateTime? reportDate = null)
        {
            // Eğer tarih seçilmemişse, bugünün tarihini varsayalım
            DateTime selectedDate = reportDate ?? DateTime.Today;

            // Seçilen tarihte kapanmış ve ödenmiş siparişleri bul
            var ordersOnDate = await _context.Orders
                .Where(o => o.IsPaid                             // Ödenmiş mi?
                         && o.CloseTime.HasValue               // Kapanış saati var mı?
                         && o.CloseTime.Value.Date == selectedDate.Date) // Tarih eşleşiyor mu?
                .ToListAsync();

            // Toplam ciroyu hesapla
            decimal totalRevenue = ordersOnDate.Sum(o => o.TotalAmount);
            // Toplam sipariş sayısını al
            int orderCount = ordersOnDate.Count;

            // Sonuçları View'a göndermek için bir ViewModel veya ViewData kullanalım
            ViewData["ReportDate"] = selectedDate.ToString("yyyy-MM-dd"); // Tarih input'u için format
            ViewData["TotalRevenue"] = totalRevenue;
            ViewData["OrderCount"] = orderCount;

            return View();
        }

        // TODO: Diğer raporlar (En Çok Satanlar, Kategori Satışları vb.) için metotlar buraya eklenecek
    }
}
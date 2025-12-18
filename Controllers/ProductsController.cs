using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeOtomasyon.Data;
using CafeOtomasyon.Models;

namespace CafeOtomasyon.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Yönetici Ürün Listesi (Varsa)
        public async Task<IActionResult> Index()
        {
            return View(await _context.Products.Include(p => p.Category).ToListAsync());
        }

        // --- 1. ÜRÜN DETAYI VE YORUMLARI GETİR (JSON) ---
        // Bu metod, Menü sayfasında "İncele" butonuna basınca çalışır.
        [HttpGet]
        public async Task<IActionResult> GetProductDetails(int id)
        {
            // Ürünü bul
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return Json(new { success = false, message = "Ürün bulunamadı." });

            // O ürüne ait yorumları çek (Tarihe göre en yeniden eskiye)
            var comments = await _context.ProductComments
                .Where(c => c.ProductId == id)
                .OrderByDescending(c => c.Date)
                .Select(c => new {
                    name = c.UserName,
                    text = c.Text,
                    rating = c.Rating,
                    date = c.Date.ToString("dd.MM.yyyy HH:mm") // Tarih formatı
                })
                .ToListAsync();

            // JS tarafına gönder
            return Json(new
            {
                success = true,
                id = product.Id,
                name = product.Name,
                description = product.Description,
                price = product.Price,
                imageName = product.ImageName,
                category = product.Category?.Name ?? "Diğer",
                comments = comments // Gerçek yorum listesi
            });
        }

        // --- 2. YENİ YORUM KAYDET ---
        // Menüdeki modal penceresinden "Gönder" butonuna basınca burası çalışır.
        [HttpPost]
        public async Task<IActionResult> AddComment(int productId, string userName, string text, int rating)
        {
            // Basit doğrulama
            if (string.IsNullOrEmpty(text) || rating < 1 || rating > 5)
            {
                return Json(new { success = false, message = "Lütfen geçerli bir yorum ve puan giriniz." });
            }

            // Model oluştur
            var comment = new ProductCommentModel
            {
                ProductId = productId,
                UserName = string.IsNullOrEmpty(userName) ? "Misafir" : userName,
                Text = text,
                Rating = rating,
                Date = DateTime.Now
            };

            // Veritabanına ekle
            _context.ProductComments.Add(comment);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Yorumunuz başarıyla kaydedildi!" });
        }
    }
}
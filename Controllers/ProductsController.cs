using CafeOtomasyon.Data;
using CafeOtomasyon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace CafeOtomasyon.Controllers 
{
    [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }



        public async Task<IActionResult> Index(string searchString)
        {
            // 1. Arama kutusunun dolu kalması için metni View'a geri gönder
            ViewData["CurrentFilter"] = searchString;

            // 2. Temel sorgu: Ürünleri ve ilişkili kategorilerini al
            var products = from p in _context.Products.Include(p => p.Category)
                           select p;

            // 3. Arama kutusu boş değilse filtrele
            if (!string.IsNullOrEmpty(searchString))
            {
                var searchLower = searchString.ToLower(); // Büyük/küçük harf duyarsız arama için

                products = products.Where(p =>
                    p.Name.ToLower().Contains(searchLower) ||
                    p.Description.ToLower().Contains(searchLower) ||
                    (p.Category != null && p.Category.Name.ToLower().Contains(searchLower))
                );
            }

            // 4. Filtrelenmiş listeyi View'a gönder
            return View(await products.AsNoTracking().ToListAsync());
        }



        // GET: Products/Create
        // Bu metot, ürün ekleme formunu gösterir ve kategori listesini de view'a gönderir.
        public IActionResult Create()
        {
            var categoriesList = _context.Categories.Where(c => c.IsActive).ToList();
            // Kategorileri dropdown'da göstermek için veritabanından çekip View'a gönderiyoruz.
            ViewData["CategoryId"] = new SelectList(categoriesList, "Id", "Name");
            return View();
        }

        // POST: Products/Create
        // Formdan gelen ürün bilgisini veritabanına kaydeder.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,IsActive,CategoryId,ImageUpload")] ProductModel productModel)
        {
            if (ModelState.IsValid)
            {
                
                if (productModel.ImageUpload != null)
                {
                    // 1. Dosya için benzersiz bir isim oluştur (resim1.jpg yerine guid_resim1.jpg)
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(productModel.ImageUpload.FileName);
                    string path = Path.Combine(wwwRootPath + "/img/products/", fileName);

                    // 2. Klasör yoksa oluştur
                    if (!Directory.Exists(Path.Combine(wwwRootPath + "/img/products/")))
                    {
                        Directory.CreateDirectory(Path.Combine(wwwRootPath + "/img/products/"));
                    }

                    // 3. Dosyayı kaydet
                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await productModel.ImageUpload.CopyToAsync(fileStream);
                    }

                    // 4. Veritabanına sadece ismini kaydet
                    productModel.ImageName = fileName;
                }
                

                _context.Add(productModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(c => c.IsActive), "Id", "Name", productModel.CategoryId);
            return View(productModel);
        }



        // GET: Products/Edit/5 (Düzenleme formunu gösterir)
        public async Task<IActionResult> Edit(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }

            var productModel = await _context.Products.FindAsync(id);
            if (productModel == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(c => c.IsActive), "Id", "Name", productModel.CategoryId);
            return View(productModel);
        }

        // POST: Products/Edit/5 (Düzenlenen formu kaydeder)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,IsActive,CategoryId,ImageUpload,ImageName")] ProductModel productModel)
        {
            if (id != productModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // --- RESİM YÜKLEME İŞLEMİ (Sadece burada olmalı) ---
                    if (productModel.ImageUpload != null)
                    {
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(productModel.ImageUpload.FileName);

                        // Klasör yolunu tanımla
                        string uploadPath = Path.Combine(wwwRootPath, "img", "products");

                        // --- KRİTİK DÜZELTME: Klasör yoksa oluştur ---
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }
                        // ---------------------------------------------

                        // Dosyayı kaydet
                        string filePath = Path.Combine(uploadPath, fileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await productModel.ImageUpload.CopyToAsync(fileStream);
                        }

                        // Eski resmi sil (Sunucuda yer açmak için)
                        // Şu an productModel.ImageName'de ESKİ resmin adı var.
                        if (!string.IsNullOrEmpty(productModel.ImageName))
                        {
                            string oldPath = Path.Combine(uploadPath, productModel.ImageName);
                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Delete(oldPath);
                            }
                        }

                        // Yeni dosya adını veritabanına kaydedilecek alana ata
                        productModel.ImageName = fileName;
                    }
                    // ---------------------------------------------------

                    _context.Update(productModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.Id == productModel.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(c => c.IsActive), "Id", "Name", productModel.CategoryId);
            return View(productModel);
        }


        // GET: Products/Delete/5 (Silme onay sayfasını gösterir)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productModel = await _context.Products
                .Include(p => p.Category) // Kategori adını da göstermek için Include ekledik
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (productModel == null)
            {
                return NotFound();
            }

            return View(productModel);
        }


        // POST: Products/Delete/5 (Ürünü siler)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var productModel = await _context.Products.FindAsync(id);
            if (productModel != null)
            {
                _context.Products.Remove(productModel);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        // POST: Products/ToggleStatus/5 (Aktif/Pasif durumunu değiştirir)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var productModel = await _context.Products.FindAsync(id);
            if (productModel != null)
            {
                // Mevcut durumun tersini ata
                productModel.IsActive = !productModel.IsActive;
                _context.Update(productModel);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }





        // Controllers/ProductsController.cs içine...

        // GET: Products/Details/5 (Ürün detayını ve yorumları gösterir)
        [AllowAnonymous] // Herkes (giriş yapmayanlar da) ürün detayını görebilsin
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Comments) // Yorumları da çekiyoruz
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }



        // POST: Products/AddComment (Yeni yorum kaydeder)
        [HttpPost]
        [AllowAnonymous] // Herkes yorum yapabilsin (İsterseniz [Authorize] yapabilirsiniz)
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment([Bind("ProductId,UserName,Text,Rating")] ProductCommentModel comment)
        {
            if (ModelState.IsValid)
            {
                comment.Date = DateTime.Now;
                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                // Yorumdan sonra sayfayı yenile (Detay sayfasına geri dön)
                return RedirectToAction("Details", new { id = comment.ProductId });
            }

            // Hata varsa yine detay sayfasına dön (Basitlik için)
            return RedirectToAction("Details", new { id = comment.ProductId });
        }



    }
}
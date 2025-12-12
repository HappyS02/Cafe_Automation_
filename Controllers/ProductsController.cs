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
    // Sınıf seviyesindeki [Authorize] kaldırıldı, çünkü buraya Müşteriler de girecek (Detay ve Yorum için)
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // YÖNETİCİ VEYA KASİYER GÖREBİLİR
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var products = from p in _context.Products.Include(p => p.Category)
                           select p;

            if (!string.IsNullOrEmpty(searchString))
            {
                var searchLower = searchString.ToLower();
                products = products.Where(p =>
                    p.Name.ToLower().Contains(searchLower) ||
                    p.Description.ToLower().Contains(searchLower) ||
                    (p.Category != null && p.Category.Name.ToLower().Contains(searchLower))
                );
            }

            return View(await products.AsNoTracking().ToListAsync());
        }

        // GET: Products/Create
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        public IActionResult Create()
        {
            var categoriesList = _context.Categories.Where(c => c.IsActive).ToList();
            ViewData["CategoryId"] = new SelectList(categoriesList, "Id", "Name");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,IsActive,CategoryId,ImageUpload")] ProductModel productModel)
        {
            if (ModelState.IsValid)
            {
                if (productModel.ImageUpload != null)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(productModel.ImageUpload.FileName);
                    string path = Path.Combine(wwwRootPath + "/img/products/", fileName);

                    if (!Directory.Exists(Path.Combine(wwwRootPath + "/img/products/")))
                    {
                        Directory.CreateDirectory(Path.Combine(wwwRootPath + "/img/products/"));
                    }

                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await productModel.ImageUpload.CopyToAsync(fileStream);
                    }

                    productModel.ImageName = fileName;
                }

                _context.Add(productModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(c => c.IsActive), "Id", "Name", productModel.CategoryId);
            return View(productModel);
        }

        // GET: Products/Edit/5
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var productModel = await _context.Products.FindAsync(id);
            if (productModel == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(c => c.IsActive), "Id", "Name", productModel.CategoryId);
            return View(productModel);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,IsActive,CategoryId,ImageUpload,ImageName")] ProductModel productModel)
        {
            if (id != productModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // --- RESİM YÜKLEME ---
                    if (productModel.ImageUpload != null)
                    {
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(productModel.ImageUpload.FileName);
                        string uploadPath = Path.Combine(wwwRootPath, "img", "products");

                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }

                        string filePath = Path.Combine(uploadPath, fileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await productModel.ImageUpload.CopyToAsync(fileStream);
                        }

                        if (!string.IsNullOrEmpty(productModel.ImageName))
                        {
                            string oldPath = Path.Combine(uploadPath, productModel.ImageName);
                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Delete(oldPath);
                            }
                        }
                        productModel.ImageName = fileName;
                    }
                    // ---------------------

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

        // GET: Products/Delete/5
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var productModel = await _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (productModel == null) return NotFound();

            return View(productModel);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
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

        // POST: Products/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var productModel = await _context.Products.FindAsync(id);
            if (productModel != null)
            {
                productModel.IsActive = !productModel.IsActive;
                _context.Update(productModel);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Details/5
        // Detayları HERKES (Giriş yapmayanlar dahil) görebilir
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Comments) // <--- BU SATIR ÇOK ÖNEMLİ (Yorumları getirir)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }


        // POST: Products/AddComment
        [HttpPost]
        [Authorize] // Sadece giriş yapanlar
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment([Bind("ProductId,Text,Rating")] ProductCommentModel comment)
        {
            // 1. Kullanıcı adını otomatik ata
            // Eğer User.Identity.Name boşsa (ki Authorize var, olmaz ama) güvenlik olsun
            comment.UserName = User.Identity.Name ?? "Anonim";

            // 2. KRİTİK NOKTA: UserName formdan gelmediği için ModelState bunu hata sayar.
            // Biz elle atadığımız için bu hatayı listeden siliyoruz.
            ModelState.Remove("UserName");

            if (ModelState.IsValid)
            {
                comment.Date = DateTime.Now;
                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                // Başarılıysa sayfaya dön
                return RedirectToAction("Details", new { id = comment.ProductId });
            }

            // --- HATA VARSA GÖRMEK İÇİN ---
            // Eğer hala kaydetmiyorsa, hatanın ne olduğunu anlamak için 
            // Console'a yazdırabilirsin (Geliştirme aşamasında)
            /*
            foreach (var modelState in ModelState.Values) {
                foreach (var error in modelState.Errors) {
                    System.Diagnostics.Debug.WriteLine("HATA: " + error.ErrorMessage);
                }
            }
            */

            // Hata varsa bile sayfaya geri dön
            return RedirectToAction("Details", new { id = comment.ProductId });
        }
    }
}
using CafeOtomasyon.Data;
using CafeOtomasyon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CafeOtomasyon.Controllers
{
    // Bu Controller'a sadece Yönetici ve Kasiyer girebilir
    [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Categories
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var categories = from c in _context.Categories
                             select c;

            if (!string.IsNullOrEmpty(searchString))
            {
                categories = categories.Where(c => c.Name.ToLower().Contains(searchString.ToLower()));
            }

            return View(await categories.ToListAsync());
        }

        // GET: Categories/Create
        // Sadece YÖNETİCİ kategori ekleyebilsin istiyorsanız bu satırı ekleyin:
        // [Authorize(Roles = AppRoles.Yönetici)] 
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // [Authorize(Roles = AppRoles.Yönetici)] // Sadece yönetici kayıt yapabilsin
        public async Task<IActionResult> Create([Bind("Name")] CategoryModel categoryModel)
        {
            if (ModelState.IsValid)
            {
                categoryModel.IsActive = true;
                _context.Add(categoryModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(categoryModel);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var categoryModel = await _context.Categories.FindAsync(id);
            if (categoryModel == null) return NotFound();

            return View(categoryModel);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,IsActive")] CategoryModel categoryModel)
        {
            if (id != categoryModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(categoryModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Categories.Any(e => e.Id == categoryModel.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(categoryModel);
        }


        // --- CategoriesController.cs EN ALTINA EKLE ---

        // 1. Silme Onay Sayfasını Gösterir (GET)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null) return NotFound();

            return View(category);
        }

        // 2. Silme İşlemini Gerçekleştirir (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category != null)
            {
                // ÖNEMLİ KONTROL: Eğer bu kategoriye bağlı ürünler varsa silmeyi engellemek daha güvenlidir.
                // Ama şimdilik direkt siliyoruz.
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        // POST: Categories/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                // Mevcut durumun tam tersini yap (True ise False, False ise True)
                category.IsActive = !category.IsActive;
                _context.Update(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
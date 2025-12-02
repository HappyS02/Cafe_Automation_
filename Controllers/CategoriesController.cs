using CafeOtomasyon.Data;
using CafeOtomasyon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Linq;

// Yönetici VEYA Kasiyer erişebilir (Virgülle ayırma 'VEYA' anlamına gelir)
[Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _context; // DbContext'i tutacak değişken

    // Constructor üzerinden DbContext'i alıyoruz (Dependency Injection)
    public CategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index(string searchString)
    {
        // ViewData ile arama metnini View'a geri taşıyoruz ki kutu boşalmasın.
        ViewData["CurrentFilter"] = searchString;

        /*var categories = new List<CategoryModel>
        {
            new CategoryModel { Category="Sıcak İçecekler", IsActive = true },
            new CategoryModel { Category="Sıcak İçecekler", IsActive = true },
            new CategoryModel { Category="Sıcak İçecekler", IsActive = true }
        };*/
        // Veriyi artık hardcoded liste yerine veritabanından çekiyoruz
        var categories = from c in _context.Categories
                         select c;

        // Eğer arama kutusu boş değilse, listeyi filtrele
        if (!string.IsNullOrEmpty(searchString))
        {
            // Büyük/küçük harf duyarsız arama yapıyoruz
            categories = categories.Where(c => c.Name.ToLower().Contains(searchString.ToLower()));
        }

        return View(categories.ToList());
    }

    // GET: Categories/Create
    // Bu metot, kullanıcıya boş kategori ekleme formunu gösterir.
    public IActionResult Create()
    {
        // Varsayım: Kullanıcının rolü Session'da saklanıyor
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Yönetici")
        {
            // Yetkisi yoksa Ana Sayfaya veya bir Hata Sayfasına yönlendir
            return RedirectToAction("Index", "Home");
            // Veya return Forbid(); // 403 Yetkisiz hatası döndürür
        }

        return View();
    }

    // POST: Categories/Create
    // Bu metot, formdan gönderilen bilgiyi alır ve veritabanına kaydeder.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name")] CategoryModel categoryModel)
    {
        // --- YETKİLENDİRME KONTROLÜ ---
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Yönetici")
        {
            return Forbid();
        }
        // --- /YETKİLENDİRME KONTROLÜ ---

        if (!ModelState.IsValid) // Model geçerli mi kontrolü
        {
            return View(categoryModel); // Model geçerli değilse, formu hatalarla birlikte geri göster
        }
        categoryModel.IsActive = true; // Kaydederken 'true' olmaya zorla

        _context.Add(categoryModel);          // Yeni kategoriyi DbContext'e ekle
        await _context.SaveChangesAsync();    // Değişiklikleri veritabanına kaydet
        return RedirectToAction(nameof(Index)); // Kayıt sonrası liste sayfasına yönlendir
    }


    // GET: Categories/Edit/5 (Düzenleme formunu gösterir)
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var categoryModel = await _context.Categories.FindAsync(id);
        if (categoryModel == null)
        {
            return NotFound();
        }
        return View(categoryModel);
    }

    // POST: Categories/Edit/5 (Düzenlenen formu kaydeder)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,IsActive")] CategoryModel categoryModel)
    {
        if (id != categoryModel.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(categoryModel);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Categories.Any(e => e.Id == categoryModel.Id))
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
        return View(categoryModel);
    }
}
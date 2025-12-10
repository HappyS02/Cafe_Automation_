using CafeOtomasyon.Data;
using CafeOtomasyon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CafeOtomasyon.Controllers
{
    [Authorize(Roles = AppRoles.Yönetici)]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LİSTELEME
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // --- YENİ KULLANICI EKLEME (GET) ---
        public IActionResult Create()
        {
            return View();
        }

        // --- YENİ KULLANICI EKLEME (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserModel userModel)
        {
            // Kullanıcı adı veya Email dolu mu kontrolü
            if (await _context.Users.AnyAsync(u => u.Username == userModel.Username || u.Email == userModel.Email))
            {
                ModelState.AddModelError("", "Bu Kullanıcı Adı veya E-Posta zaten kayıtlı.");
            }

            if (ModelState.IsValid)
            {
                userModel.IsActive = true; // Varsayılan aktif olsun
                _context.Add(userModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(userModel);
        }

        // DÜZENLEME (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Şifreyi güvenlik gereği boş gönderiyoruz, isterse doldurur
            user.Password = "";
            return View(user);
        }

        // DÜZENLEME (POST) - ŞİFRE DEĞİŞTİRME MANTIĞI BURADA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserModel userModel)
        {
            if (id != userModel.Id) return NotFound();

            // Eski veriyi çek (Şifreyi korumak için)
            var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (existingUser == null) return NotFound();

            // Eğer şifre alanı boş bırakıldıysa, eski şifreyi geri koy
            if (string.IsNullOrEmpty(userModel.Password))
            {
                userModel.Password = existingUser.Password;
                // Validasyon hatasını temizle (Çünkü boş gelince hata veriyor model)
                ModelState.Remove("Password");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(userModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Users.Any(e => e.Id == userModel.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(userModel);
        }

        // DURUM DEĞİŞTİR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                if (user.Username == User.Identity.Name)
                    TempData["Error"] = "Kendinizi pasif yapamazsınız!";
                else
                {
                    user.IsActive = !user.IsActive;
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // SİLME
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                if (user.Username == User.Identity.Name)
                    TempData["Error"] = "Kendinizi silemezsiniz!";
                else
                {
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
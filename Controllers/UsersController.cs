using CafeOtomasyon.Data;
using CafeOtomasyon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace CafeOtomasyon.Controllers
{
    [Authorize(Roles = AppRoles.Yönetici)] // SADECE YÖNETİCİ ERİŞEBİLİR
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Users (Listeleme ve Arama)
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var users = from u in _context.Users
                        select u;

            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u => u.Username.Contains(searchString)
                                       || u.Email.Contains(searchString));
            }

            return View(await users.AsNoTracking().ToListAsync());
        }

        // GET: Users/Create (Yeni Kullanıcı Formunu Göster)
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create (Yeni Kullanıcıyı Kaydet)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Username,Password,TC,Email,PhoneNumber,IsActive,Role")] UserModel userModel)
        {
            if (ModelState.IsValid)
            {
                // ... (Şifre hashleme vb.) ...
                _context.Add(userModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(userModel);
        }



        // GET: Users/Edit/5 (Düzenleme formunu gösterir)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userModel = await _context.Users.FindAsync(id);
            if (userModel == null)
            {
                return NotFound();
            }
            return View(userModel);
        }

        // POST: Users/Edit/5 (Düzenlenen formu kaydeder)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Password,TC,Email,PhoneNumber,IsActive, Role")] UserModel userModel)
        {
            if (id != userModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // ... (Şifre kontrolü ve hashleme) ...
                    _context.Update(userModel);
                    await _context.SaveChangesAsync();

                    // --- KRİTİK ŞİFRE KONTROLÜ ---
                    // Eğer formdan gelen şifre alanı boşsa, bu, kullanıcının şifresini
                    // değiştirmek istemediği anlamına gelir.
                    if (string.IsNullOrEmpty(userModel.Password))
                    {
                        // Veritabanından mevcut kullanıcıyı bul
                        var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                        if (existingUser != null)
                        {
                            // Formdan gelen modele, veritabanındaki eski şifreyi ata.
                            // Böylece şifresi boş olarak güncellenmez.
                            userModel.Password = existingUser.Password;
                        }
                    }
                    // else
                    // {
                    //    // Eğer şifre boş değilse, YENİ BİR ŞİFRE GİRİLMİŞTİR.
                    //    // Bu noktada bu yeni şifreyi HASH'lemeniz gerekir!
                    //    // userModel.Password = Hashleyin(userModel.Password);
                    // }

                    _context.Update(userModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Users.Any(e => e.Id == userModel.Id))
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
            return View(userModel);
        }

        // GET: Users/Delete/5 (Silme onay sayfasını gösterir)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userModel = await _context.Users
                .AsNoTracking() // Silme onayı olduğu için veriyi izlemeye gerek yok
                .FirstOrDefaultAsync(m => m.Id == id);

            if (userModel == null)
            {
                return NotFound();
            }

            return View(userModel);
        }

        // POST: Users/Delete/5 (Kullanıcıyı siler)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userModel = await _context.Users.FindAsync(id);
            if (userModel != null)
            {
                _context.Users.Remove(userModel);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }




        // POST: Users/ToggleStatus/5 (Aktif/Pasif durumunu değiştirir)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                // Mevcut durumun tersini ata
                user.IsActive = !user.IsActive;
                _context.Update(user);
                await _context.SaveChangesAsync();
            }

            // İşlem bittikten sonra liste sayfasına geri dön
            return RedirectToAction(nameof(Index));
        }

    }
}
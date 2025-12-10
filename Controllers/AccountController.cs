using CafeOtomasyon.Data;
using CafeOtomasyon.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CafeOtomasyon.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string usernameOrEmail, string password, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // 1. Kullanıcıyı Bul (Kullanıcı Adı VEYA E-Posta ile)
            // Bu sorgu: "Girilen metin username'e eşitse YA DA email'e eşitse" diye bakar.
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                (u.Username == usernameOrEmail || u.Email == usernameOrEmail) &&
                u.Password == password);

            // 2. Kullanıcı bulundu mu ve aktif mi?
            if (user != null)
            {
                if (!user.IsActive)
                {
                    ViewBag.Error = "Hesabınız pasif durumdadır. Yönetici ile görüşün.";
                    return View();
                }

                // 3. Kimlik Oluştur
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name), // Ekranda Ad Soyad görünsün
            new Claim(ClaimTypes.Role, user.Role)
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = true }; // Beni hatırla

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // 1. Eğer MÜŞTERİ ise -> Direkt MENÜ sayfasına uçur 🚀
                if (user.Role == AppRoles.Musteri)
                {
                    return RedirectToAction("Menu", "Home");
                }

                // 2. Eğer PERSONEL (Yönetici, Kasiyer, Garson) ise -> HOŞ GELDİNİZ paneline gönder 🏠
                // Böylece Yönetim Paneli mi yoksa Menü mü diye seçebilirler.
                return RedirectToAction("Welcome", "Home");

                // (Eğer returnUrl varsa onu kontrol etmek istersen buraya ekleyebilirsin ama 
                // yukarıdaki mantık daha temiz bir akış sağlar.)
            
        }

            ViewBag.Error = "Kullanıcı adı/E-posta veya şifre hatalı.";
            return View();
        }

        // --- YENİ EKLENEN: KAYIT OLMA (REGISTER) ---

        // GET: /Account/Register
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }



        // POST: /Account/Register
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string name, string username, string email, string password)
        {
            // 1. Boş alan kontrolü
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Lütfen tüm alanları doldurun.";
                return View();
            }

            try
            {
                // 2. Kullanıcı Adı veya E-Posta daha önce alınmış mı?
                if (await _context.Users.AnyAsync(u => u.Username == username || u.Email == email))
                {
                    ViewBag.Error = "Bu kullanıcı adı veya e-posta zaten sistemde kayıtlı.";
                    return View();
                }

                // 3. Yeni Kullanıcıyı Oluştur
                var newUser = new UserModel
                {
                    Name = name,
                    Username = username,
                    Email = email,         // <-- E-Posta eklendi
                    Password = password,
                    Role = AppRoles.Musteri, // Otomatik Müşteri Rolü
                    IsActive = true
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Kayıt başarılı! Giriş yapabilirsiniz.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Hata oluştu: " + ex.Message;
                return View();
            }
        }

        // ---------------------------------------------

        // POST: /Account/Logout
        [HttpPost] // Post olması güvenlik için daha iyidir ama link ile çıkış için GET de eklenebilir
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // Link ile çıkış yapmak istersen (Navbar'daki link için)
        [HttpGet]
        public async Task<IActionResult> LogoutGet()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
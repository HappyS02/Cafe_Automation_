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
        [HttpGet]
        public IActionResult Login()
        {
            // Eski mesajları temizle
            TempData.Clear();
            return View();
        }

        // POST: /Account/Login
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string usernameOrEmail, string password, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                (u.Username == usernameOrEmail || u.Email == usernameOrEmail) &&
                u.Password == password);

            if (user != null)
            {
                if (!user.IsActive)
                {
                    ViewBag.Error = "Hesabınız pasif durumdadır. Yönetici ile görüşün.";
                    return View();
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // UserId burada tutuluyor
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = true };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Müşteri ise Menüye, Personel ise Hoşgeldin ekranına
                if (user.Role == AppRoles.Musteri)
                {
                    return RedirectToAction("Menu", "Home");
                }

                return RedirectToAction("Welcome", "Home");
            }

            ViewBag.Error = "Kullanıcı adı/E-posta veya şifre hatalı.";
            return View();
        }

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
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Lütfen tüm alanları doldurun.";
                return View();
            }

            try
            {
                if (await _context.Users.AnyAsync(u => u.Username == username || u.Email == email))
                {
                    ViewBag.Error = "Bu kullanıcı adı veya e-posta zaten sistemde kayıtlı.";
                    return View();
                }

                var newUser = new UserModel
                {
                    Name = name,
                    Username = username,
                    Email = email,
                    Password = password,
                    Role = AppRoles.Musteri,
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

        // --- ÇIKIŞ İŞLEMLERİ (DÜZELTİLDİ) ---

        // POST: /Account/Logout (Butona basınca burası çalışır)
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // 1. ÖNCE SESSION'I SİL (Masa bilgilerini temizle)
            HttpContext.Session.Clear();

            // 2. SONRA HESAPTAN ÇIK
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/Logout (Navbar linkine basınca burası çalışır)
        [HttpGet]
        public async Task<IActionResult> LogoutGet()
        {
            // BURAYI DA GÜNCELLEDİK: Linke tıklayınca da hafıza silinsin!
            HttpContext.Session.Clear();

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
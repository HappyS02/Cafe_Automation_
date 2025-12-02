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

        // GET: /Account/Login (Giriş sayfasını gösterir)
        [AllowAnonymous] // Herkes erişebilir
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl; // Başarılı girişten sonra nereye gidileceğini sakla
            return View();
        }

        // POST: /Account/Login (Giriş işlemini yapar)
        [AllowAnonymous] // Giriş yapmayanlar erişebilir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // Kullanıcıyı veritabanında ara (ŞİFRE KONTROLÜ HENÜZ YAPILMIYOR!)
                var user = await _context.Users
                                    .FirstOrDefaultAsync(u => u.Username == model.Username /* && u.Password == HashlenmisSifre(model.Password) */);

                // --- ŞİMDİLİK BASİT ŞİFRE KONTROLÜ ---
                // !!! GÜVENLİK UYARISI: Bu KESİNLİKLE gerçek uygulamada kullanılmamalıdır !!!
                if (user != null && user.Password == model.Password && user.IsActive)
                {
                    // Kullanıcı bulundu, aktif ve şifre (şimdilik) doğru. GİRİŞ YAP.

                    // 1. Kullanıcının kimlik bilgilerini (Claims) oluştur
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Kullanıcı ID'si
                        new Claim(ClaimTypes.Name, user.Username), // Kullanıcı Adı
                        new Claim(ClaimTypes.Role, user.Role) // KULLANICI ROLÜ (En önemlisi bu!)
                        // İsterseniz Email gibi başka bilgileri de ekleyebilirsiniz
                    };

                    // 2. Kimlik oluştur (Authentication şemasıyla eşleşmeli)
                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    // 3. Oturum açma özelliklerini ayarla (örn: kalıcı cookie)
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe, // "Beni Hatırla" seçiliyse cookie kalıcı olur
                        ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : (DateTimeOffset?)null // Kalıcıysa 7 gün
                    };

                    // 4. Kullanıcıyı SİSTEME GİRİŞ YAP (Cookie oluşturulur)
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // Başarılı giriş sonrası yönlendirme
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl); // Geldiği sayfaya geri gönder
                    }
                    else
                    {
                        return RedirectToAction("FloorPlan", "Tables"); // Veya varsayılan ana sayfaya gönder
                    }
                }
                // --- /BASİT ŞİFRE KONTROLÜ SONU ---

                // Kullanıcı bulunamadı, pasif veya şifre yanlışsa hata ver
                ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre ya da hesap pasif.");
            }

            // Model geçerli değilse veya giriş başarısızsa formu tekrar göster
            return View(model);
        }

        // GET veya POST: /Account/Logout (Çıkış işlemini yapar)
        [HttpPost] // Güvenlik için genellikle POST tercih edilir
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Kullanıcıyı SİSTEMDEN ÇIKIŞ YAP (Cookie silinir)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account"); // Giriş sayfasına yönlendir
        }

        // GET: /Account/AccessDenied (Yetkisiz Erişim Sayfası)
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
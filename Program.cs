using CafeOtomasyon.Data;
using Microsoft.EntityFrameworkCore;
// Gerekli using'ler eklenmiþ olabilir
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// === SERVÝSLER ===

// Veritabaný Baðlamý
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Kimlik Doðrulama (Authentication)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HER ZAMAN HTTPS GEREKTÝR
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

// Yetkilendirme (Authorization)
builder.Services.AddAuthorization();

// Session Servisleri (DOÐRU YER BURASI)
builder.Services.AddDistributedMemoryCache(); // Önce bellek önbelleði
builder.Services.AddSession(options =>        // Sonra Session ayarlarý
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HER ZAMAN HTTPS GEREKTÝR
});

// MVC Servisleri (Authorize Filter dahil)
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter()); // Tüm controller'larý koru
});


builder.Services.AddSession(); 
builder.Services.AddHttpContextAccessor();

// === UYGULAMA OLUÞTURMA ===
var app = builder.Build();


// --- (TÜRKÇE AYARI) ---
var supportedCultures = new[] { "tr-TR" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("tr-TR")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);


// === MIDDLEWARE PIPELINE (SIRALAMA ÖNEMLÝ) ===

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();


app.UseSession();


app.UseRouting(); // URL'leri eþleþtirme

app.UseSession(); // Session'ý ETKÝNLEÞTÝR (UseRouting'den SONRA, UseAuthentication/UseAuthorization'dan ÖNCE)

app.UseAuthentication(); // Kimlik Doðrula (Session'dan SONRA)
app.UseAuthorization();  // Yetkilendir (Authentication'dan SONRA)

// Controller/Action rotasýný tanýmla
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run(); // Uygulamayý çalýþtýr
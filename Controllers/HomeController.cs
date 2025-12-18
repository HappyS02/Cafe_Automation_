using CafeOtomasyon.Data;
using CafeOtomasyon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace CafeOtomasyon.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // Veritabaný baðlantýsý

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Menu(int? tableId)
        {
            // 1. KULLANICI GÝRÝÞ YAPMIÞ MI?
            string currentUserId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

            // 2. DIÞARIDAN QR ÝLE GELMÝÞSE (URL'de tableId var)
            if (tableId.HasValue)
            {
                var qrTable = _context.Tables.AsNoTracking().FirstOrDefault(t => t.Id == tableId.Value);

                // Masa var ve (Boþsa VEYA Zaten benimse) -> Ýçeri al
                if (qrTable != null && (!qrTable.IsOccupied || qrTable.UserId == currentUserId))
                {
                    HttpContext.Session.SetInt32("TableModelId", tableId.Value);
                }
                else if (qrTable != null && qrTable.IsOccupied)
                {
                    // Masa doluysa ve benim deðilse, QR okutsam bile oturumu açma
                    // (Burada istersen hata mesajý da verdirebilirsin)
                }
            }

            // 3. MEVCUT OTURUMU KONTROL ET (KRÝTÝK BÖLÜM)
            int? sessionTableId = HttpContext.Session.GetInt32("TableModelId");

            if (sessionTableId.HasValue)
            {
                // Veritabanýndan en güncel hali çek (AsNoTracking ile önbelleði deliyoruz)
                var dbTable = _context.Tables.AsNoTracking().FirstOrDefault(t => t.Id == sessionTableId.Value);

                bool atilmali = false;

                // KONTROL LÝSTESÝ:
                if (dbTable == null) atilmali = true; // Masa silinmiþ
                else if (dbTable.IsOccupied == false) atilmali = true; // Masa BOÞALMIÞ (Ödeme yapýlmýþ!)
                else if (dbTable.UserId != null && dbTable.UserId != currentUserId) atilmali = true; // Baþkasý oturmuþ

                if (atilmali)
                {
                    // --- KESÝN ÇÖZÜM BURASI ---
                    // Eðer masa artýk geçerli deðilse:
                    // 1. Hafýzayý sil
                    HttpContext.Session.Remove("TableModelId");

                    // 2. Sayfayý PARAMETRESÝZ olarak yeniden yükle (Redirect)
                    // Bu sayede "?tableId=1" gibi URL takýlý kaldýysa o da temizlenir.
                    return RedirectToAction("Menu");
                }

                // Sorun yoksa bilgileri gönder
                ViewBag.TableId = dbTable.Id;
                ViewBag.TableName = dbTable.Name;
            }
            // Geri Dönüþ Senaryosu: Session yok ama DB'de masam var
            else if (currentUserId != null)
            {
                // BURAYI DEÐÝÞTÝRÝYORUZ:
                // Sadece UserId bana ait olan DEÐÝL, ayný zamanda DOLU olan masayý getir.
                // Eðer masa boþalmýþsa (IsOccupied == false), UserId bende kalsa bile getirme!

                var myRealTable = _context.Tables
                    .AsNoTracking() // Veritabanýndan taze bilgi al
                    .FirstOrDefault(t => t.UserId == currentUserId && t.IsOccupied == true); // <--- '&& t.IsOccupied == true' ÞARTI ÇOK ÖNEMLÝ

                if (myRealTable != null)
                {
                    // Masa hem benim hem de hala dolu, o zaman hafýzaya al
                    HttpContext.Session.SetInt32("TableModelId", myRealTable.Id);
                    ViewBag.TableId = myRealTable.Id;
                    ViewBag.TableName = myRealTable.Name;
                }
                else
                {
                    // EÐER VERÝTABANINDA BANA AÝT DOLU MASA BULAMADIYSAK...
                    // ...AMA HAFIZADA (SESSION) HALA BÝR MASA VARSA...
                    int? sessionTid = HttpContext.Session.GetInt32("TableModelId");
                    if (sessionTid.HasValue)
                    {
                        // Demek ki masa az önce kapandý. Session'ý da sil ki kurtulalým.
                        HttpContext.Session.Remove("TableModelId");
                        return RedirectToAction("Menu"); // Sayfayý yenile ve temizle
                    }
                }
            }

            var products = _context.Products.Include(p => p.Category).ToList();
            return View(products);
        }


        /*
        public IActionResult LeaveTable()
        {
            // Sadece masa bilgisini sil, diðer oturum bilgileri (giriþ vs) kalsýn
            HttpContext.Session.Remove("TableId");

            // Sepeti de boþaltmak istersen:
            // HttpContext.Session.Remove("Cart"); 

            return RedirectToAction("Menu");
        }
        */



        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }



        [Authorize] // Sadece giriþ yapmýþ olanlar görebilir
        public IActionResult Welcome()
        {
            return View();
        }
    }
}

using CafeOtomasyon.Data;
using CafeOtomasyon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Diagnostics;

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

        public async Task<IActionResult> Menu()
        {
            // Sadece Aktif ürünleri getiriyoruz
            var products = await _context.Products
                .Include(p => p.Category) // Kategori bilgisini de al
                .Where(p => p.IsActive)   // Sadece aktif olanlar
                .OrderBy(p => p.Category.Name) // Kategoriye göre sýrala
                .ToListAsync();

            return View(products);
        }

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
    }
}

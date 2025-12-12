using CafeOtomasyon.Data;
using CafeOtomasyon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CafeOtomasyon.Controllers
{
    // Sadece Personel (Yönetici, Kasiyer) görebilsin
    [Authorize(Roles = $"{AppRoles.Yönetici},{AppRoles.Kasiyer}")]
    public class Reports2Controller : Controller
    {
        private readonly ApplicationDbContext _context;

        
        public Reports2Controller(ApplicationDbContext context) 
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = new StatsViewModel();

            // 1. TEMEL SAYILAR
            model.TotalProductCount = await _context.Products.CountAsync();
            model.TotalCategoryCount = await _context.Categories.CountAsync();
            model.TotalOrderCount = await _context.Orders.CountAsync();

            // Sadece ödenmiş siparişlerin cirosunu topla
            model.TotalRevenue = await _context.Orders
                .Where(o => o.IsPaid)
                .SumAsync(o => o.TotalAmount);

            // 2. KULLANICI İSTATİSTİKLERİ
            var users = await _context.Users.ToListAsync();
            model.AdminCount = users.Count(u => u.Role == AppRoles.Yönetici);
            model.CashierCount = users.Count(u => u.Role == AppRoles.Kasiyer);
            model.WaiterCount = users.Count(u => u.Role == AppRoles.Garson);
            model.CustomerCount = users.Count(u => u.Role == AppRoles.Musteri);

            // 3. SATIŞ ANALİZİ
            var productSales = await _context.OrderDetails
                .Include(od => od.Product)
                .ThenInclude(p => p.Category)
                .GroupBy(od => od.Product)
                .Select(g => new ProductSalesStat
                {
                    ProductName = g.Key.Name,
                    CategoryName = g.Key.Category.Name,
                    Price = g.Key.Price,
                    TotalSold = g.Sum(od => od.Quantity),
                    TotalIncome = g.Sum(od => od.Quantity * od.Price)
                })
                .OrderByDescending(x => x.TotalSold)
                .ToListAsync();

            model.ProductStats = productSales;

            if (productSales.Any())
            {
                var best = productSales.First();
                var worst = productSales.Last();

                model.BestSellingProductName = best.ProductName;
                model.BestSellingProductCount = best.TotalSold;

                model.LeastSellingProductName = worst.ProductName;
                model.LeastSellingProductCount = worst.TotalSold;
            }
            else
            {
                model.BestSellingProductName = "Henüz Satış Yok";
                model.LeastSellingProductName = "Henüz Satış Yok";
            }

            return View(model);
        }
    }
}
namespace CafeOtomasyon.Models
{
    public class StatsViewModel
    {
        // Kartlar için Sayısal Veriler
        public int TotalProductCount { get; set; }
        public int TotalCategoryCount { get; set; }
        public int TotalOrderCount { get; set; }
        public decimal TotalRevenue { get; set; } // Toplam Ciro

        // Kullanıcı Sayıları
        public int AdminCount { get; set; }
        public int CashierCount { get; set; }
        public int WaiterCount { get; set; }
        public int CustomerCount { get; set; }

        // En Çok ve En Az Satan Ürünler
        public string BestSellingProductName { get; set; }
        public int BestSellingProductCount { get; set; }

        public string LeastSellingProductName { get; set; }
        public int LeastSellingProductCount { get; set; }

        // Tüm Ürünlerin Satış Listesi
        public List<ProductSalesStat> ProductStats { get; set; }
    }

    // Her bir ürünün detay satırı için yardımcı sınıf
    public class ProductSalesStat
    {
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public int TotalSold { get; set; } // Kaç adet satıldı
        public decimal TotalIncome { get; set; } // Bu üründen kazanılan para
    }
}
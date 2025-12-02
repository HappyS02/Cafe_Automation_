using System.ComponentModel.DataAnnotations.Schema;

namespace CafeOtomasyon.Models
{
    public class ProductModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }
        // ESKİ HALİ:
        // public string Category { get; set; }

        // YENİ HALİ:
        public int CategoryId { get; set; } // Bu, CategoryModel'in Id'sine karşılık gelen Foreign Key olacak.
        // Bu özellik, EF Core'un ilgili kategoriyi ürüne bağlamasını sağlar.
        public CategoryModel? Category { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}

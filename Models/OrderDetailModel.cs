using System.ComponentModel.DataAnnotations.Schema;

namespace CafeOtomasyon.Models
{
    public class OrderDetailModel
    {
        public int Id { get; set; }

        public int OrderModelId { get; set; } // Hangi siparişe ait
        public OrderModel? Order { get; set; }

        public int ProductModelId { get; set; } // Hangi ürün
        public ProductModel? Product { get; set; }

        public int Quantity { get; set; } // Kaç adet

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; } // Sipariş anındaki ürün fiyatı
    }
}
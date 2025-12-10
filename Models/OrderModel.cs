using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeOtomasyon.Models
{
    public class OrderModel
    {
        public int Id { get; set; }

        public int TableModelId { get; set; } // Hangi masaya ait
        public TableModel? Table { get; set; }

        public DateTime OpenTime { get; set; } // Masa açılış (dolu) tarihi (Req #4)
        public DateTime? CloseTime { get; set; } // Masa kapanış tarihi

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; } // Toplam tutar (Req #5)

        public bool IsPaid { get; set; } // Ödendi mi? (Req #6)

        // Bu siparişe ait ürün detayları
        public ICollection<OrderDetailModel> OrderDetails { get; set; } = new List<OrderDetailModel>();



    }
}
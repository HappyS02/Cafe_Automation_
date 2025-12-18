using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace CafeOtomasyon.Models
{
    public class ProductCommentModel
    {
        public int Id { get; set; }

        // Hangi ürüne yapıldı?
        public int ProductId { get; set; }
        public ProductModel? Product { get; set; }

        // Yorumu yapan kişi (Müşteri girişi yoksa sadece isim isteriz)
        [Required(ErrorMessage = "İsim zorunludur")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Yorum yazmalısınız")]
        public string Text { get; set; }

        [Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasında olmalıdır")]
        public int Rating { get; set; } // 1-5 arası yıldız

        public DateTime Date { get; set; } = DateTime.Now;

    }
}
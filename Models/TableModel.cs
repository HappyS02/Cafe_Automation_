using System.ComponentModel.DataAnnotations;

namespace CafeOtomasyon.Models
{
    public class TableModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Masa adı zorunludur")]
        [Display(Name = "Masa Adı / Numarası")]
        public string? Name { get; set; } 

        [Display(Name = "Konum")]
        public string? Location { get; set; } // Örn: "Bahçe", "İç Alan"

        [Display(Name = "Kapasite")]
        public int Capacity { get; set; } // Kişi sayısı

        [Display(Name = "Durum")]
        public TableStatus Status { get; set; } // Enum tipimizi kullanıyoruz


        // YENİ EKLENEN ALAN: Masanın mevcut açık siparişini tutar
        public int? CurrentOrderId { get; set; }

        public bool IsOccupied { get; set; } = false;


        // Bu masada oturan kullanıcının ID'si (Guid string olarak tutulur)
        public string? UserId { get; set; }

        public bool IsHelpRequested { get; set; } = false;
    }

    public enum TableStatus
    {
        Boş,  // Boş
        Dolu,   // Dolu (Servis var)
        Rezerve    // Rezerve
    }

}
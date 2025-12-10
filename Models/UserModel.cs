using System.ComponentModel.DataAnnotations;

namespace CafeOtomasyon.Models
{
    public class UserModel
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }      // Ad Soyad
        public string Username { get; set; }  // Kullanıcı Adı
        public string Email { get; set; }     // E-Posta
        public string Password { get; set; }  // Şifre
        public string Role { get; set; }      // Rol
        public bool IsActive { get; set; } = true; // Aktiflik Durumu
    }
}
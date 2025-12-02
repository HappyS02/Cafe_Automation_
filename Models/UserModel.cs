using System.ComponentModel.DataAnnotations;

namespace CafeOtomasyon.Models
{
    public class UserModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur")]
        [DataType(DataType.Password)] // Bu, input'un 'password' tipinde olmasını sağlar
        public string Password { get; set; }

        [Required(ErrorMessage = "TC Kimlik Numarası zorunludur")]
        public string TC { get; set; }

        [Required(ErrorMessage = "Email zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi girin")]
        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public bool IsActive { get; set; }

        [Required(ErrorMessage = "Rol seçimi zorunludur")]
        [Display(Name = "Kullanıcı Rolü")]
        public string Role { get; set; }
    }
}
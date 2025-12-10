using CafeOtomasyon.Models;
using Microsoft.EntityFrameworkCore;

namespace CafeOtomasyon.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Veritabanında oluşturulacak tabloları burada DbSet olarak tanımlıyoruz.
        public DbSet<ProductModel> Products { get; set; }
        public DbSet<CategoryModel> Categories { get; set; }
        public DbSet<UserModel> Users { get; set; }
        public DbSet<TableModel> Tables { get; set; }
        public DbSet<OrderModel> Orders { get; set; }
        public DbSet<OrderDetailModel> OrderDetails { get; set; }

        public DbSet<ProductCommentModel> Comments { get; set; }
    }
}
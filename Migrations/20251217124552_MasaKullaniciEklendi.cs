using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeOtomasyon.Migrations
{
    /// <inheritdoc />
    public partial class MasaKullaniciEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Tables",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Tables");
        }
    }
}

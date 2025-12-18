using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeOtomasyon.Migrations
{
    /// <inheritdoc />
    public partial class AddHelpRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHelpRequested",
                table: "Tables",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHelpRequested",
                table: "Tables");
        }
    }
}

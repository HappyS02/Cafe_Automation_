using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeOtomasyon.Migrations
{
    /// <inheritdoc />
    public partial class AddComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductCommentModelId",
                table: "Comments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ProductCommentModelId",
                table: "Comments",
                column: "ProductCommentModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Comments_ProductCommentModelId",
                table: "Comments",
                column: "ProductCommentModelId",
                principalTable: "Comments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Comments_ProductCommentModelId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_ProductCommentModelId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "ProductCommentModelId",
                table: "Comments");
        }
    }
}

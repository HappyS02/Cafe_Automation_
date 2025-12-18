using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeOtomasyon.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCommentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Comments_ProductCommentModelId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Products_ProductId",
                table: "Comments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Comments",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_ProductCommentModelId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "ProductCommentModelId",
                table: "Comments");

            migrationBuilder.RenameTable(
                name: "Comments",
                newName: "ProductCommentModel");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_ProductId",
                table: "ProductCommentModel",
                newName: "IX_ProductCommentModel_ProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductCommentModel",
                table: "ProductCommentModel",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCommentModel_Products_ProductId",
                table: "ProductCommentModel",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductCommentModel_Products_ProductId",
                table: "ProductCommentModel");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductCommentModel",
                table: "ProductCommentModel");

            migrationBuilder.RenameTable(
                name: "ProductCommentModel",
                newName: "Comments");

            migrationBuilder.RenameIndex(
                name: "IX_ProductCommentModel_ProductId",
                table: "Comments",
                newName: "IX_Comments_ProductId");

            migrationBuilder.AddColumn<int>(
                name: "ProductCommentModelId",
                table: "Comments",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Comments",
                table: "Comments",
                column: "Id");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Products_ProductId",
                table: "Comments",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

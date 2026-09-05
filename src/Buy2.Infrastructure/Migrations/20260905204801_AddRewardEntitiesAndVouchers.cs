using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buy2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardEntitiesAndVouchers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PointTransactionId",
                table: "RewardRedemptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsTransactionId",
                table: "RewardRedemptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RewardVoucherId",
                table: "RewardRedemptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RewardVoucherId1",
                table: "RewardRedemptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "RewardName",
                table: "RewardItems",
                type: "nvarchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "BannerImageUrl",
                table: "RewardItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "RewardItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId1",
                table: "RewardItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "RewardItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HowToRedeem",
                table: "RewardItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "RewardItems",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonetaryValue",
                table: "RewardItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TermsOfUse",
                table: "RewardItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "RequestTypes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "RequestTypes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateTable(
                name: "RewardCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RewardVouchers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RewardItemId = table.Column<int>(type: "int", nullable: false),
                    RewardItemId1 = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardVouchers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RewardVouchers_RewardItems_RewardItemId",
                        column: x => x.RewardItemId,
                        principalTable: "RewardItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RewardVouchers_RewardItems_RewardItemId1",
                        column: x => x.RewardItemId1,
                        principalTable: "RewardItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RewardRedemptions_PointsTransactionId",
                table: "RewardRedemptions",
                column: "PointsTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardRedemptions_PointTransactionId",
                table: "RewardRedemptions",
                column: "PointTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardRedemptions_RewardVoucherId",
                table: "RewardRedemptions",
                column: "RewardVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardRedemptions_RewardVoucherId1",
                table: "RewardRedemptions",
                column: "RewardVoucherId1");

            migrationBuilder.CreateIndex(
                name: "IX_RewardItems_CategoryId",
                table: "RewardItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardItems_CategoryId1",
                table: "RewardItems",
                column: "CategoryId1");

            migrationBuilder.CreateIndex(
                name: "IX_RewardVouchers_RewardItemId",
                table: "RewardVouchers",
                column: "RewardItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardVouchers_RewardItemId1",
                table: "RewardVouchers",
                column: "RewardItemId1");

            migrationBuilder.AddForeignKey(
                name: "FK_RewardItems_RewardCategories_CategoryId",
                table: "RewardItems",
                column: "CategoryId",
                principalTable: "RewardCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RewardItems_RewardCategories_CategoryId1",
                table: "RewardItems",
                column: "CategoryId1",
                principalTable: "RewardCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RewardRedemptions_PointsTransactions_PointTransactionId",
                table: "RewardRedemptions",
                column: "PointTransactionId",
                principalTable: "PointsTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RewardRedemptions_PointsTransactions_PointsTransactionId",
                table: "RewardRedemptions",
                column: "PointsTransactionId",
                principalTable: "PointsTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RewardRedemptions_RewardVouchers_RewardVoucherId",
                table: "RewardRedemptions",
                column: "RewardVoucherId",
                principalTable: "RewardVouchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RewardRedemptions_RewardVouchers_RewardVoucherId1",
                table: "RewardRedemptions",
                column: "RewardVoucherId1",
                principalTable: "RewardVouchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RewardItems_RewardCategories_CategoryId",
                table: "RewardItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RewardItems_RewardCategories_CategoryId1",
                table: "RewardItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RewardRedemptions_PointsTransactions_PointTransactionId",
                table: "RewardRedemptions");

            migrationBuilder.DropForeignKey(
                name: "FK_RewardRedemptions_PointsTransactions_PointsTransactionId",
                table: "RewardRedemptions");

            migrationBuilder.DropForeignKey(
                name: "FK_RewardRedemptions_RewardVouchers_RewardVoucherId",
                table: "RewardRedemptions");

            migrationBuilder.DropForeignKey(
                name: "FK_RewardRedemptions_RewardVouchers_RewardVoucherId1",
                table: "RewardRedemptions");

            migrationBuilder.DropTable(
                name: "RewardCategories");

            migrationBuilder.DropTable(
                name: "RewardVouchers");

            migrationBuilder.DropIndex(
                name: "IX_RewardRedemptions_PointsTransactionId",
                table: "RewardRedemptions");

            migrationBuilder.DropIndex(
                name: "IX_RewardRedemptions_PointTransactionId",
                table: "RewardRedemptions");

            migrationBuilder.DropIndex(
                name: "IX_RewardRedemptions_RewardVoucherId",
                table: "RewardRedemptions");

            migrationBuilder.DropIndex(
                name: "IX_RewardRedemptions_RewardVoucherId1",
                table: "RewardRedemptions");

            migrationBuilder.DropIndex(
                name: "IX_RewardItems_CategoryId",
                table: "RewardItems");

            migrationBuilder.DropIndex(
                name: "IX_RewardItems_CategoryId1",
                table: "RewardItems");

            migrationBuilder.DropColumn(
                name: "PointTransactionId",
                table: "RewardRedemptions");

            migrationBuilder.DropColumn(
                name: "PointsTransactionId",
                table: "RewardRedemptions");

            migrationBuilder.DropColumn(
                name: "RewardVoucherId",
                table: "RewardRedemptions");

            migrationBuilder.DropColumn(
                name: "RewardVoucherId1",
                table: "RewardRedemptions");

            migrationBuilder.DropColumn(
                name: "BannerImageUrl",
                table: "RewardItems");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "RewardItems");

            migrationBuilder.DropColumn(
                name: "CategoryId1",
                table: "RewardItems");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "RewardItems");

            migrationBuilder.DropColumn(
                name: "HowToRedeem",
                table: "RewardItems");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "RewardItems");

            migrationBuilder.DropColumn(
                name: "MonetaryValue",
                table: "RewardItems");

            migrationBuilder.DropColumn(
                name: "TermsOfUse",
                table: "RewardItems");

            migrationBuilder.AlterColumn<string>(
                name: "RewardName",
                table: "RewardItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "RequestTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "RequestTypes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}

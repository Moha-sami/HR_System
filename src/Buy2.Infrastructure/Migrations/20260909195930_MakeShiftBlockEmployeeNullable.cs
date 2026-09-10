using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buy2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeShiftBlockEmployeeNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShiftBlocks_EmployeeId",
                table: "ShiftBlocks");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "ShiftBlocks",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftBlocks_EmployeeId",
                table: "ShiftBlocks",
                column: "EmployeeId",
                unique: true,
                filter: "[EmployeeId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShiftBlocks_EmployeeId",
                table: "ShiftBlocks");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "ShiftBlocks",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftBlocks_EmployeeId",
                table: "ShiftBlocks",
                column: "EmployeeId",
                unique: true);
        }
    }
}

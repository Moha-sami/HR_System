using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buy2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShiftBlockEmployeeUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShiftBlocks_EmployeeId",
                table: "ShiftBlocks");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftBlocks_EmployeeId",
                table: "ShiftBlocks",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShiftBlocks_EmployeeId",
                table: "ShiftBlocks");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftBlocks_EmployeeId",
                table: "ShiftBlocks",
                column: "EmployeeId",
                unique: true,
                filter: "[EmployeeId] IS NOT NULL");
        }
    }
}

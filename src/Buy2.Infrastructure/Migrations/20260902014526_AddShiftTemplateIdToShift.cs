using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buy2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftTemplateIdToShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShiftTemplateId",
                table: "ShiftEntities",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftEntities_ShiftTemplateId",
                table: "ShiftEntities",
                column: "ShiftTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftEntities_ShiftTemplates_ShiftTemplateId",
                table: "ShiftEntities",
                column: "ShiftTemplateId",
                principalTable: "ShiftTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShiftEntities_ShiftTemplates_ShiftTemplateId",
                table: "ShiftEntities");

            migrationBuilder.DropIndex(
                name: "IX_ShiftEntities_ShiftTemplateId",
                table: "ShiftEntities");

            migrationBuilder.DropColumn(
                name: "ShiftTemplateId",
                table: "ShiftEntities");
        }
    }
}

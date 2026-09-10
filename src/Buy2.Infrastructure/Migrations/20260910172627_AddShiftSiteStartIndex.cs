using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buy2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftSiteStartIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShiftEntities_SiteId",
                table: "ShiftEntities");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftEntities_SiteId_StartTime",
                table: "ShiftEntities",
                columns: new[] { "SiteId", "StartTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShiftEntities_SiteId_StartTime",
                table: "ShiftEntities");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftEntities_SiteId",
                table: "ShiftEntities",
                column: "SiteId");
        }
    }
}

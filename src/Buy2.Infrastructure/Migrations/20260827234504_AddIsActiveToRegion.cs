using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buy2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToRegion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Regions",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Regions");
        }
    }
}

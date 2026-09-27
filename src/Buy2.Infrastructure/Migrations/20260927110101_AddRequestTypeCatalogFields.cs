using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buy2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestTypeCatalogFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddedBy",
                table: "RequestTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hint",
                table: "RequestTypes",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "RequestTypes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "LeavePay",
                table: "RequestTypes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeaveType",
                table: "RequestTypes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddedBy",
                table: "RequestTypes");

            migrationBuilder.DropColumn(
                name: "Hint",
                table: "RequestTypes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "RequestTypes");

            migrationBuilder.DropColumn(
                name: "LeavePay",
                table: "RequestTypes");

            migrationBuilder.DropColumn(
                name: "LeaveType",
                table: "RequestTypes");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buy2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncProductionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sites_Employees_EmployeeId",
                table: "Sites");

            migrationBuilder.DropIndex(
                name: "IX_Sites_EmployeeId",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Sites");

            migrationBuilder.AddColumn<decimal>(
                name: "AllTimeAverage",
                table: "PerformanceMetrics",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentScore",
                table: "PerformanceMetrics",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ScoreLabel",
                table: "PerformanceMetrics",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "WorkWeekStart",
                table: "PayrollProfiles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "WorkWeekEnd",
                table: "PayrollProfiles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "AttendanceType",
                table: "PayrollProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OfflineWorkdaysJson",
                table: "PayrollProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnlineWorkdaysJson",
                table: "PayrollProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkSiteIdsJson",
                table: "PayrollProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "EmployeeTasks",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "EmployeeTasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SiteId1",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SiteId1",
                table: "Employees",
                column: "SiteId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Sites_SiteId1",
                table: "Employees",
                column: "SiteId1",
                principalTable: "Sites",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Sites_SiteId1",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_SiteId1",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AllTimeAverage",
                table: "PerformanceMetrics");

            migrationBuilder.DropColumn(
                name: "CurrentScore",
                table: "PerformanceMetrics");

            migrationBuilder.DropColumn(
                name: "ScoreLabel",
                table: "PerformanceMetrics");

            migrationBuilder.DropColumn(
                name: "AttendanceType",
                table: "PayrollProfiles");

            migrationBuilder.DropColumn(
                name: "OfflineWorkdaysJson",
                table: "PayrollProfiles");

            migrationBuilder.DropColumn(
                name: "OnlineWorkdaysJson",
                table: "PayrollProfiles");

            migrationBuilder.DropColumn(
                name: "WorkSiteIdsJson",
                table: "PayrollProfiles");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "EmployeeTasks");

            migrationBuilder.DropColumn(
                name: "SiteId1",
                table: "Employees");

            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "Sites",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WorkWeekStart",
                table: "PayrollProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "WorkWeekEnd",
                table: "PayrollProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "EmployeeTasks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sites_EmployeeId",
                table: "Sites",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sites_Employees_EmployeeId",
                table: "Sites",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");
        }
    }
}

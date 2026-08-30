using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buy2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAttendnceRecordAndDayStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EarlyLeaveMinutes",
                table: "AttendanceRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScheduledHours",
                table: "AttendanceRecords",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EarlyLeaveMinutes",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "ScheduledHours",
                table: "AttendanceRecords");
        }
    }
}

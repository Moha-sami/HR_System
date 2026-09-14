using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buy2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PerformanceSubmissions_EmployeeId",
                table: "PerformanceSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeTasks_EmployeeId",
                table: "EmployeeTasks");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_EmployeeId",
                table: "AttendanceRecords");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftEntities_IsPublished_EmployeeId_StartTime",
                table: "ShiftEntities",
                columns: new[] { "IsPublished", "EmployeeId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_PointsAutomationRuns_Status_Category_PeriodEnd",
                table: "PointsAutomationRuns",
                columns: new[] { "Status", "Category", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSubmissions_EmployeeId_SubmissionDate",
                table: "PerformanceSubmissions",
                columns: new[] { "EmployeeId", "SubmissionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTasks_EmployeeId_DueDate",
                table: "EmployeeTasks",
                columns: new[] { "EmployeeId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_EmployeeId_Date",
                table: "AttendanceRecords",
                columns: new[] { "EmployeeId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShiftEntities_IsPublished_EmployeeId_StartTime",
                table: "ShiftEntities");

            migrationBuilder.DropIndex(
                name: "IX_PointsAutomationRuns_Status_Category_PeriodEnd",
                table: "PointsAutomationRuns");

            migrationBuilder.DropIndex(
                name: "IX_PerformanceSubmissions_EmployeeId_SubmissionDate",
                table: "PerformanceSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeTasks_EmployeeId_DueDate",
                table: "EmployeeTasks");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_EmployeeId_Date",
                table: "AttendanceRecords");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSubmissions_EmployeeId",
                table: "PerformanceSubmissions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTasks_EmployeeId",
                table: "EmployeeTasks",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_EmployeeId",
                table: "AttendanceRecords",
                column: "EmployeeId");
        }
    }
}

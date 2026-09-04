using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buy2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomationCategoryToPointsTransactionAndTaskPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AutomationCategory",
                table: "PointsTransactions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MetricId",
                table: "PointsAutomationSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "EmployeeTasks",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Medium");

            migrationBuilder.CreateIndex(
                name: "IX_PointsTransaction_Idempotency",
                table: "PointsTransactions",
                columns: new[] { "EmployeeId", "AutomationCategory", "TriggeredBy", "EvaluationPeriodStart", "EvaluationPeriodEnd" },
                unique: true,
                filter: "[AutomationCategory] IS NOT NULL AND [EvaluationPeriodStart] IS NOT NULL AND [EvaluationPeriodEnd] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PointsAutomationSettings_MetricId",
                table: "PointsAutomationSettings",
                column: "MetricId");

            migrationBuilder.AddForeignKey(
                name: "FK_PointsAutomationSettings_PerformanceMetrics_MetricId",
                table: "PointsAutomationSettings",
                column: "MetricId",
                principalTable: "PerformanceMetrics",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PointsAutomationSettings_PerformanceMetrics_MetricId",
                table: "PointsAutomationSettings");

            migrationBuilder.DropIndex(
                name: "IX_PointsTransaction_Idempotency",
                table: "PointsTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PointsAutomationSettings_MetricId",
                table: "PointsAutomationSettings");

            migrationBuilder.DropColumn(
                name: "AutomationCategory",
                table: "PointsTransactions");

            migrationBuilder.DropColumn(
                name: "MetricId",
                table: "PointsAutomationSettings");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "EmployeeTasks");
        }
    }
}

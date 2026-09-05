using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buy2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FilterAutomationRunIndexToCompleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PointsAutomationRun_Period",
                table: "PointsAutomationRuns");

            migrationBuilder.CreateIndex(
                name: "IX_PointsAutomationRun_Period",
                table: "PointsAutomationRuns",
                columns: new[] { "AutomationPeriod", "PeriodStart", "PeriodEnd" },
                unique: true,
                filter: "[Status] = 'Completed'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PointsAutomationRun_Period",
                table: "PointsAutomationRuns");

            migrationBuilder.CreateIndex(
                name: "IX_PointsAutomationRun_Period",
                table: "PointsAutomationRuns",
                columns: new[] { "AutomationPeriod", "PeriodStart", "PeriodEnd" },
                unique: true);
        }
    }
}

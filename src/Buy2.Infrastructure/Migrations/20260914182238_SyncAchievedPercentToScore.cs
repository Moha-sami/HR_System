using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buy2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncAchievedPercentToScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Single source of truth: Score wins. Backfill the legacy mirror column
            // so overview queries and the automation evaluator agree on old rows.
            migrationBuilder.Sql(
                "UPDATE [PerformanceSubmissions] SET [AchievedPercent] = [Score] WHERE [AchievedPercent] <> [Score];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data backfill is not reversible.
        }
    }
}

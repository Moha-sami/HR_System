using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buy2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPointsManagementEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PointsTransactions_EmployeeId",
                table: "PointsTransactions");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "PointsTransactions",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "Comments",
                table: "PointsTransactions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "PointsTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EvaluationPeriodEnd",
                table: "PointsTransactions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EvaluationPeriodStart",
                table: "PointsTransactions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriggeredBy",
                table: "PointsTransactions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "RuleKey",
                table: "PointsRules",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "PointsRules",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ConditionExpression",
                table: "PointsRules",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ActionType",
                table: "PointsRules",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PointsRules",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "PointsRules",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PointsAutomationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    SubCategory = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    AutomationPeriod = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsAutomationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PointsAutomationRanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AutomationSettingId = table.Column<int>(type: "int", nullable: false),
                    RangeType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    FromValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ToValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TaskPriority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    PointsValue = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsAutomationRanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointsAutomationRanges_PointsAutomationSettings_AutomationSettingId",
                        column: x => x.AutomationSettingId,
                        principalTable: "PointsAutomationSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PointsTransactions_EmployeeId_CreatedAt",
                table: "PointsTransactions",
                columns: new[] { "EmployeeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PointsTransactions_TransactionType",
                table: "PointsTransactions",
                column: "TransactionType");

            migrationBuilder.CreateIndex(
                name: "IX_PointsTransactions_TriggeredBy",
                table: "PointsTransactions",
                column: "TriggeredBy");

            migrationBuilder.CreateIndex(
                name: "IX_PointsRules_RuleKey",
                table: "PointsRules",
                column: "RuleKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PointsAutomationRanges_AutomationSettingId",
                table: "PointsAutomationRanges",
                column: "AutomationSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_PointsAutomationSettings_Category_SubCategory",
                table: "PointsAutomationSettings",
                columns: new[] { "Category", "SubCategory" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PointsAutomationRanges");

            migrationBuilder.DropTable(
                name: "PointsAutomationSettings");

            migrationBuilder.DropIndex(
                name: "IX_PointsTransactions_EmployeeId_CreatedAt",
                table: "PointsTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PointsTransactions_TransactionType",
                table: "PointsTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PointsTransactions_TriggeredBy",
                table: "PointsTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PointsRules_RuleKey",
                table: "PointsRules");

            migrationBuilder.DropColumn(
                name: "Comments",
                table: "PointsTransactions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "PointsTransactions");

            migrationBuilder.DropColumn(
                name: "EvaluationPeriodEnd",
                table: "PointsTransactions");

            migrationBuilder.DropColumn(
                name: "EvaluationPeriodStart",
                table: "PointsTransactions");

            migrationBuilder.DropColumn(
                name: "TriggeredBy",
                table: "PointsTransactions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PointsRules");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PointsRules");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PointsTransactions",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<string>(
                name: "RuleKey",
                table: "PointsRules",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "PointsRules",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ConditionExpression",
                table: "PointsRules",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "ActionType",
                table: "PointsRules",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.CreateIndex(
                name: "IX_PointsTransactions_EmployeeId",
                table: "PointsTransactions",
                column: "EmployeeId");
        }
    }
}

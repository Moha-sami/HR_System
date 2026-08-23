using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buy2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewEntitiesAndRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "Sites",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttendanceType",
                table: "Employees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "Birthdate",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DirectManagerId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeCode",
                table: "Employees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ExperienceYears",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Employees",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "JobType",
                table: "Employees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "JoinDate",
                table: "Employees",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "OfflineWorkdaysJson",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnlineWorkdaysJson",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoUrl",
                table: "Employees",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeniorityLevel",
                table: "Employees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActionDate",
                table: "DisciplinaryViolations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActionDescription",
                table: "DisciplinaryViolations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActionTakenById",
                table: "DisciplinaryViolations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActionType",
                table: "DisciplinaryViolations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentUrl",
                table: "DisciplinaryViolations",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReportedById",
                table: "DisciplinaryViolations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DisciplinaryViolations",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ViolationType",
                table: "DisciplinaryViolations",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WitnessesJson",
                table: "DisciplinaryViolations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeAchievements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    BadgeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAchievements_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSites",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSites", x => new { x.EmployeeId, x.SiteId });
                    table.ForeignKey(
                        name: "FK_EmployeeSites_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeSites_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TaskName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTasks_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    SalaryType = table.Column<string>(type: "varchar(20)", nullable: false),
                    PayoutPeriod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PayoutDay = table.Column<int>(type: "int", nullable: false),
                    WorkWeekStart = table.Column<int>(type: "int", nullable: false),
                    WorkWeekEnd = table.Column<int>(type: "int", nullable: false),
                    PaymentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OvertimeThresholdHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OvertimeHourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollProfiles_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Target = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    MetricId = table.Column<int>(type: "int", nullable: false),
                    AchievedPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceSubmissions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceSubmissions_PerformanceMetrics_MetricId",
                        column: x => x.MetricId,
                        principalTable: "PerformanceMetrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sites_EmployeeId",
                table: "Sites",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DirectManagerId",
                table: "Employees",
                column: "DirectManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryViolations_ActionTakenById",
                table: "DisciplinaryViolations",
                column: "ActionTakenById");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryViolations_ReportedById",
                table: "DisciplinaryViolations",
                column: "ReportedById");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAchievements_EmployeeId",
                table: "EmployeeAchievements",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSites_SiteId",
                table: "EmployeeSites",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTasks_EmployeeId",
                table: "EmployeeTasks",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollProfiles_EmployeeId",
                table: "PayrollProfiles",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSubmissions_EmployeeId",
                table: "PerformanceSubmissions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSubmissions_MetricId",
                table: "PerformanceSubmissions",
                column: "MetricId");

            migrationBuilder.AddForeignKey(
                name: "FK_DisciplinaryViolations_Employees_ActionTakenById",
                table: "DisciplinaryViolations",
                column: "ActionTakenById",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DisciplinaryViolations_Employees_ReportedById",
                table: "DisciplinaryViolations",
                column: "ReportedById",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Employees_DirectManagerId",
                table: "Employees",
                column: "DirectManagerId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sites_Employees_EmployeeId",
                table: "Sites",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DisciplinaryViolations_Employees_ActionTakenById",
                table: "DisciplinaryViolations");

            migrationBuilder.DropForeignKey(
                name: "FK_DisciplinaryViolations_Employees_ReportedById",
                table: "DisciplinaryViolations");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Employees_DirectManagerId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Sites_Employees_EmployeeId",
                table: "Sites");

            migrationBuilder.DropTable(
                name: "EmployeeAchievements");

            migrationBuilder.DropTable(
                name: "EmployeeSites");

            migrationBuilder.DropTable(
                name: "EmployeeTasks");

            migrationBuilder.DropTable(
                name: "PayrollProfiles");

            migrationBuilder.DropTable(
                name: "PerformanceSubmissions");

            migrationBuilder.DropTable(
                name: "PerformanceMetrics");

            migrationBuilder.DropIndex(
                name: "IX_Sites_EmployeeId",
                table: "Sites");

            migrationBuilder.DropIndex(
                name: "IX_Employees_DirectManagerId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_DisciplinaryViolations_ActionTakenById",
                table: "DisciplinaryViolations");

            migrationBuilder.DropIndex(
                name: "IX_DisciplinaryViolations_ReportedById",
                table: "DisciplinaryViolations");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "AttendanceType",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Birthdate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DirectManagerId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmployeeCode",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ExperienceYears",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "JobType",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "JoinDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "OfflineWorkdaysJson",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "OnlineWorkdaysJson",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ProfilePhotoUrl",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SeniorityLevel",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ActionDate",
                table: "DisciplinaryViolations");

            migrationBuilder.DropColumn(
                name: "ActionDescription",
                table: "DisciplinaryViolations");

            migrationBuilder.DropColumn(
                name: "ActionTakenById",
                table: "DisciplinaryViolations");

            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "DisciplinaryViolations");

            migrationBuilder.DropColumn(
                name: "DocumentUrl",
                table: "DisciplinaryViolations");

            migrationBuilder.DropColumn(
                name: "ReportedById",
                table: "DisciplinaryViolations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "DisciplinaryViolations");

            migrationBuilder.DropColumn(
                name: "ViolationType",
                table: "DisciplinaryViolations");

            migrationBuilder.DropColumn(
                name: "WitnessesJson",
                table: "DisciplinaryViolations");
        }
    }
}

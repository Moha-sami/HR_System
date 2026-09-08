using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buy2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ShiftTemplatesAndBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DaysOfWeekJson",
                table: "ShiftTemplates");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "ShiftTemplates");

            migrationBuilder.DropColumn(
                name: "RequiredHeadcount",
                table: "ShiftTemplates");

            migrationBuilder.AddColumn<int>(
                name: "LastUpdatedByEmployeeId",
                table: "ShiftTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "ShiftTemplates",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ShiftBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftTemplateId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    JobRoleId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftBlocks_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftBlocks_JobRoles_JobRoleId",
                        column: x => x.JobRoleId,
                        principalTable: "JobRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftBlocks_ShiftTemplates_ShiftTemplateId",
                        column: x => x.ShiftTemplateId,
                        principalTable: "ShiftTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShiftTemplateSites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftTemplateId = table.Column<int>(type: "int", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftTemplateSites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftTemplateSites_ShiftTemplates_ShiftTemplateId",
                        column: x => x.ShiftTemplateId,
                        principalTable: "ShiftTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShiftTemplateSites_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftTemplates_LastUpdatedByEmployeeId",
                table: "ShiftTemplates",
                column: "LastUpdatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftBlocks_EmployeeId",
                table: "ShiftBlocks",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftBlocks_JobRoleId",
                table: "ShiftBlocks",
                column: "JobRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftBlocks_ShiftTemplateId",
                table: "ShiftBlocks",
                column: "ShiftTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftTemplateSites_ShiftTemplateId_SiteId",
                table: "ShiftTemplateSites",
                columns: new[] { "ShiftTemplateId", "SiteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftTemplateSites_SiteId",
                table: "ShiftTemplateSites",
                column: "SiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftTemplates_Employees_LastUpdatedByEmployeeId",
                table: "ShiftTemplates",
                column: "LastUpdatedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShiftTemplates_Employees_LastUpdatedByEmployeeId",
                table: "ShiftTemplates");

            migrationBuilder.DropTable(
                name: "ShiftBlocks");

            migrationBuilder.DropTable(
                name: "ShiftTemplateSites");

            migrationBuilder.DropIndex(
                name: "IX_ShiftTemplates_LastUpdatedByEmployeeId",
                table: "ShiftTemplates");

            migrationBuilder.DropColumn(
                name: "LastUpdatedByEmployeeId",
                table: "ShiftTemplates");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ShiftTemplates");

            migrationBuilder.AddColumn<string>(
                name: "DaysOfWeekJson",
                table: "ShiftTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "ShiftTemplates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredHeadcount",
                table: "ShiftTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}

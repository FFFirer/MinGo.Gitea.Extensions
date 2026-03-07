using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinGo.Gitea.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GiteaConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiteaConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledTriggers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Owner = table.Column<string>(type: "TEXT", nullable: false),
                    Repo = table.Column<string>(type: "TEXT", nullable: false),
                    CronExpression = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Inputs = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Workflow = table.Column<string>(type: "TEXT", nullable: false),
                    Ref = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledTriggers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowRunHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Owner = table.Column<string>(type: "TEXT", nullable: false),
                    Repo = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowId = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowName = table.Column<string>(type: "TEXT", nullable: false),
                    Ref = table.Column<string>(type: "TEXT", nullable: false),
                    CommitSha = table.Column<string>(type: "TEXT", nullable: false),
                    RunId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Conclusion = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TriggerType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowRunHistories", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GiteaConfigs");

            migrationBuilder.DropTable(
                name: "ScheduledTriggers");

            migrationBuilder.DropTable(
                name: "WorkflowRunHistories");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinGo.Gitea.Core.Migrations
{
    public partial class AddWorkflowTriggerTemplate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowTriggerTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Owner = table.Column<string>(type: "TEXT", nullable: false),
                    Repo = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowId = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowName = table.Column<string>(type: "TEXT", nullable: false),
                    Ref = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTriggerTemplates", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowTriggerTemplates");
        }
    }
}

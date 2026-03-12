using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinGo.Gitea.Core.Migrations
{
    public partial class AddLastTriggeredAtToTemplate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastTriggeredAt",
                table: "WorkflowTriggerTemplates",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTriggeredAt",
                table: "WorkflowTriggerTemplates");
        }
    }
}

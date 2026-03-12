using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinGo.Gitea.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddInputsToWorkflowRunHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Inputs",
                table: "WorkflowRunHistories",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Inputs",
                table: "WorkflowRunHistories");
        }
    }
}

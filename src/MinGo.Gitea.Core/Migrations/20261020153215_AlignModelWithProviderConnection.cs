using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinGo.Gitea.Core.Migrations
{
    /// <inheritdoc />
    public partial class AlignModelWithProviderConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConnectionId",
                table: "WorkflowTriggerTemplates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "WorkflowTriggerTemplates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ConnectionId",
                table: "WorkflowRunHistories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "WorkflowRunHistories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProviderConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderType = table.Column<int>(type: "INTEGER", nullable: false),
                    ServerUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderConnections", x => x.Id);
                });

            // 将旧 GiteaConfigs 数据迁入 ProviderConnections，避免丢配置
            migrationBuilder.Sql(@"
                INSERT INTO ProviderConnections (Id, Name, ProviderType, ServerUrl, Token, Username, IsEnabled)
                SELECT Id, 'default', 0, ServerUrl, Token, Username, 1
                FROM GiteaConfigs;
            ");

            migrationBuilder.DropTable(
                name: "GiteaConfigs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.Sql(@"
                INSERT INTO GiteaConfigs (Id, ServerUrl, Token, Username)
                SELECT Id, ServerUrl, Token, Username
                FROM ProviderConnections;
            ");

            migrationBuilder.DropTable(
                name: "ProviderConnections");

            migrationBuilder.DropColumn(
                name: "ConnectionId",
                table: "WorkflowTriggerTemplates");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "WorkflowTriggerTemplates");

            migrationBuilder.DropColumn(
                name: "ConnectionId",
                table: "WorkflowRunHistories");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "WorkflowRunHistories");
        }
    }
}

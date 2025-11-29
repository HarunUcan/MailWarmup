using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoWarm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarmupMetadataToLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWarmup",
                table: "WarmupEmailLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WarmupId",
                table: "WarmupEmailLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsWarmup",
                table: "WarmupEmailLogs");

            migrationBuilder.DropColumn(
                name: "WarmupId",
                table: "WarmupEmailLogs");
        }
    }
}

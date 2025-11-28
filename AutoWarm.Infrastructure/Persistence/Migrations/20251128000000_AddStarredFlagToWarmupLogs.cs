using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoWarm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStarredFlagToWarmupLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MarkedAsStarred",
                table: "WarmupEmailLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarkedAsStarred",
                table: "WarmupEmailLogs");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoWarm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarmupPlannedEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarmupPlannedEmails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderMailAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetMailAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InternetMessageId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MarkRead = table.Column<bool>(type: "bit", nullable: false),
                    SendReply = table.Column<bool>(type: "bit", nullable: false),
                    MarkImportant = table.Column<bool>(type: "bit", nullable: false),
                    AddStar = table.Column<bool>(type: "bit", nullable: false),
                    Archive = table.Column<bool>(type: "bit", nullable: false),
                    Delete = table.Column<bool>(type: "bit", nullable: false),
                    RescueFromSpam = table.Column<bool>(type: "bit", nullable: false),
                    ImportantStarGraceLimit = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarmupPlannedEmails", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarmupPlannedEmails_InternetMessageId",
                table: "WarmupPlannedEmails",
                column: "InternetMessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarmupPlannedEmails");
        }
    }
}

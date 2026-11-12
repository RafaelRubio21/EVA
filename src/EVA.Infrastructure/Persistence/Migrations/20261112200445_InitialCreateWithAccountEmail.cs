using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EVA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateWithAccountEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AccountEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Sender = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriorityLevel = table.Column<int>(type: "int", nullable: false),
                    RequiresResponse = table.Column<bool>(type: "bit", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    Deadline = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NextReminderAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailAnalyses_EmailRecords_EmailRecordId",
                        column: x => x.EmailRecordId,
                        principalTable: "EmailRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailAnalyses_EmailRecordId",
                table: "EmailAnalyses",
                column: "EmailRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailAnalyses_RequiresResponse_PriorityLevel_NextReminderAt",
                table: "EmailAnalyses",
                columns: new[] { "RequiresResponse", "PriorityLevel", "NextReminderAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailRecords_AccountEmail_MessageId",
                table: "EmailRecords",
                columns: new[] { "AccountEmail", "MessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailRecords_IsProcessed_ReceivedAt",
                table: "EmailRecords",
                columns: new[] { "IsProcessed", "ReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailAnalyses");

            migrationBuilder.DropTable(
                name: "EmailRecords");
        }
    }
}

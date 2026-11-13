using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EVA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvaSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailPollingIntervalSeconds = table.Column<int>(type: "int", nullable: false),
                    ReminderCheckIntervalSeconds = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvaSettings");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EVA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSuggestedReplyDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SuggestedReplyDraft",
                table: "EmailAnalyses",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SuggestedReplyDraft",
                table: "EmailAnalyses");
        }
    }
}

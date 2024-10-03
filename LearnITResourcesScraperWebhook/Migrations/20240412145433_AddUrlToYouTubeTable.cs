using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnITResourcesScraperWebhook.Migrations
{
    /// <inheritdoc />
    public partial class AddUrlToYouTubeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "YouTubeChannels",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Url",
                table: "YouTubeChannels");
        }
    }
}

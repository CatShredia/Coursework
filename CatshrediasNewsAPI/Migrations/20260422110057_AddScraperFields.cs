using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatshrediasNewsAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddScraperFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentSelector",
                table: "RssSources",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DateSelector",
                table: "RssSources",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageSelector",
                table: "RssSources",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkSelector",
                table: "RssSources",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "RssSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TitleSelector",
                table: "RssSources",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentSelector",
                table: "RssSources");

            migrationBuilder.DropColumn(
                name: "DateSelector",
                table: "RssSources");

            migrationBuilder.DropColumn(
                name: "ImageSelector",
                table: "RssSources");

            migrationBuilder.DropColumn(
                name: "LinkSelector",
                table: "RssSources");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "RssSources");

            migrationBuilder.DropColumn(
                name: "TitleSelector",
                table: "RssSources");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatshrediasNewsAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAvatarFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarColor",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarColor",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Users");
        }
    }
}

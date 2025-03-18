using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShotYourPet.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddColunsToDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AvatarId",
                table: "Authors",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pseudo",
                table: "Authors",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarId",
                table: "Authors");

            migrationBuilder.DropColumn(
                name: "Pseudo",
                table: "Authors");
        }
    }
}

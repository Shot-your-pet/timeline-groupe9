using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShotYourPet.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ChangeChallengeIdType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name:"ChallengeId", table: "Posts");
            migrationBuilder.AddColumn<Guid>(
                name: "ChallengeId",
                table: "Posts",
                type: "uuid",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name:"ChallengeId", table: "Posts");
            migrationBuilder.AddColumn<long>(
                name: "ChallengeId",
                table: "Posts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}

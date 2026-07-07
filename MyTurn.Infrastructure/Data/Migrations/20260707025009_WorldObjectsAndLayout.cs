using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTurn.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class WorldObjectsAndLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LayoutId",
                table: "WorldSessions",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LayoutSource",
                table: "WorldSessions",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileId",
                table: "WorldSessions",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorldObjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorldSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObjectId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ObjectType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    X = table.Column<int>(type: "INTEGER", nullable: false),
                    Y = table.Column<int>(type: "INTEGER", nullable: false),
                    IsBlocking = table.Column<bool>(type: "INTEGER", nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EncounterSeed = table.Column<int>(type: "INTEGER", nullable: true),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorldObjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorldObjects_WorldSessions_WorldSessionId",
                        column: x => x.WorldSessionId,
                        principalTable: "WorldSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorldObjects_WorldSessionId_ObjectId",
                table: "WorldObjects",
                columns: new[] { "WorldSessionId", "ObjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorldObjects_WorldSessionId_X_Y",
                table: "WorldObjects",
                columns: new[] { "WorldSessionId", "X", "Y" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorldObjects");

            migrationBuilder.DropColumn(
                name: "LayoutId",
                table: "WorldSessions");

            migrationBuilder.DropColumn(
                name: "LayoutSource",
                table: "WorldSessions");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "WorldSessions");
        }
    }
}

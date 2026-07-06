using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTurn.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemDefinitions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsStackable = table.Column<bool>(type: "INTEGER", nullable: false),
                    WeaponType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    MinDamage = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxDamage = table.Column<int>(type: "INTEGER", nullable: true),
                    EquipmentSlot = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    HealingAmount = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SaveSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastPlayedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaveSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnemyDefinitions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    WeaponItemId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ExperienceReward = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnemyDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnemyDefinitions_ItemDefinitions_WeaponItemId",
                        column: x => x.WeaponItemId,
                        principalTable: "ItemDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemStatModifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemDefinitionId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    StatType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemStatModifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemStatModifiers_ItemDefinitions_ItemDefinitionId",
                        column: x => x.ItemDefinitionId,
                        principalTable: "ItemDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SaveSlotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Species = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CharacterClass = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Steps = table.Column<long>(type: "INTEGER", nullable: false),
                    Currency = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_SaveSlots_SaveSlotId",
                        column: x => x.SaveSlotId,
                        principalTable: "SaveSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorldSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SaveSlotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Seed = table.Column<int>(type: "INTEGER", nullable: false),
                    MinCoordinate = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxCoordinate = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentX = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentY = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorldSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorldSessions_SaveSlots_SaveSlotId",
                        column: x => x.SaveSlotId,
                        principalTable: "SaveSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnemyActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EnemyDefinitionId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnemyActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnemyActions_EnemyDefinitions_EnemyDefinitionId",
                        column: x => x.EnemyDefinitionId,
                        principalTable: "EnemyDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnemyLootDrops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EnemyDefinitionId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    MinQuantity = table.Column<long>(type: "INTEGER", nullable: false),
                    MaxQuantity = table.Column<long>(type: "INTEGER", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnemyLootDrops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnemyLootDrops_EnemyDefinitions_EnemyDefinitionId",
                        column: x => x.EnemyDefinitionId,
                        principalTable: "EnemyDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnemySpawnWeights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EnemyDefinitionId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnemySpawnWeights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnemySpawnWeights_EnemyDefinitions_EnemyDefinitionId",
                        column: x => x.EnemyDefinitionId,
                        principalTable: "EnemyDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnemyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EnemyDefinitionId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    StatType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnemyStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnemyStats_EnemyDefinitions_EnemyDefinitionId",
                        column: x => x.EnemyDefinitionId,
                        principalTable: "EnemyDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerEquipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slot = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerEquipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerEquipment_ItemDefinitions_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ItemDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerEquipment_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerInventoryStacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerInventoryStacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerInventoryStacks_ItemDefinitions_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ItemDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerInventoryStacks_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SkillType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CurrentLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Experience = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxLevel = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerSkills_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StatType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    BaseValue = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxValue = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerStats_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorldRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorldSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    X = table.Column<int>(type: "INTEGER", nullable: false),
                    Y = table.Column<int>(type: "INTEGER", nullable: false),
                    RoomType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EncounterSeed = table.Column<int>(type: "INTEGER", nullable: true),
                    IsVisited = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCleared = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLooted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorldRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorldRooms_WorldSessions_WorldSessionId",
                        column: x => x.WorldSessionId,
                        principalTable: "WorldSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnemyActions_EnemyDefinitionId",
                table: "EnemyActions",
                column: "EnemyDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_EnemyDefinitions_WeaponItemId",
                table: "EnemyDefinitions",
                column: "WeaponItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EnemyLootDrops_EnemyDefinitionId",
                table: "EnemyLootDrops",
                column: "EnemyDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_EnemySpawnWeights_EnemyDefinitionId",
                table: "EnemySpawnWeights",
                column: "EnemyDefinitionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnemyStats_EnemyDefinitionId_StatType",
                table: "EnemyStats",
                columns: new[] { "EnemyDefinitionId", "StatType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemStatModifiers_ItemDefinitionId_StatType_Value",
                table: "ItemStatModifiers",
                columns: new[] { "ItemDefinitionId", "StatType", "Value" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEquipment_ItemId",
                table: "PlayerEquipment",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEquipment_PlayerId_Slot",
                table: "PlayerEquipment",
                columns: new[] { "PlayerId", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInventoryStacks_ItemId",
                table: "PlayerInventoryStacks",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInventoryStacks_PlayerId_ItemId",
                table: "PlayerInventoryStacks",
                columns: new[] { "PlayerId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_SaveSlotId",
                table: "Players",
                column: "SaveSlotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSkills_PlayerId_SkillType",
                table: "PlayerSkills",
                columns: new[] { "PlayerId", "SkillType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStats_PlayerId_StatType",
                table: "PlayerStats",
                columns: new[] { "PlayerId", "StatType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorldRooms_WorldSessionId_X_Y",
                table: "WorldRooms",
                columns: new[] { "WorldSessionId", "X", "Y" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorldSessions_SaveSlotId_IsCompleted",
                table: "WorldSessions",
                columns: new[] { "SaveSlotId", "IsCompleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnemyActions");

            migrationBuilder.DropTable(
                name: "EnemyLootDrops");

            migrationBuilder.DropTable(
                name: "EnemySpawnWeights");

            migrationBuilder.DropTable(
                name: "EnemyStats");

            migrationBuilder.DropTable(
                name: "ItemStatModifiers");

            migrationBuilder.DropTable(
                name: "PlayerEquipment");

            migrationBuilder.DropTable(
                name: "PlayerInventoryStacks");

            migrationBuilder.DropTable(
                name: "PlayerSkills");

            migrationBuilder.DropTable(
                name: "PlayerStats");

            migrationBuilder.DropTable(
                name: "WorldRooms");

            migrationBuilder.DropTable(
                name: "EnemyDefinitions");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "WorldSessions");

            migrationBuilder.DropTable(
                name: "ItemDefinitions");

            migrationBuilder.DropTable(
                name: "SaveSlots");
        }
    }
}

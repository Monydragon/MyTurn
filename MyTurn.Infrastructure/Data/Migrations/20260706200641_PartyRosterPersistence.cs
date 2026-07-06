using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTurn.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PartyRosterPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Currency",
                table: "SaveSlots",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Steps",
                table: "SaveSlots",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "SuggestedLevel",
                table: "ItemDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "ItemDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ThreatRating",
                table: "EnemyDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "PartyInventoryStacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SaveSlotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyInventoryStacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyInventoryStacks_ItemDefinitions_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ItemDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartyInventoryStacks_SaveSlots_SaveSlotId",
                        column: x => x.SaveSlotId,
                        principalTable: "SaveSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartyMembers",
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
                    Location = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ActiveOrder = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyMembers_SaveSlots_SaveSlotId",
                        column: x => x.SaveSlotId,
                        principalTable: "SaveSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartyMemberEquipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartyMemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slot = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyMemberEquipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyMemberEquipment_ItemDefinitions_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ItemDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartyMemberEquipment_PartyMembers_PartyMemberId",
                        column: x => x.PartyMemberId,
                        principalTable: "PartyMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartyMemberSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartyMemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SkillType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CurrentLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Experience = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxLevel = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyMemberSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyMemberSkills_PartyMembers_PartyMemberId",
                        column: x => x.PartyMemberId,
                        principalTable: "PartyMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartyMemberStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartyMemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StatType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    BaseValue = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxValue = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyMemberStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyMemberStats_PartyMembers_PartyMemberId",
                        column: x => x.PartyMemberId,
                        principalTable: "PartyMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartyInventoryStacks_ItemId",
                table: "PartyInventoryStacks",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyInventoryStacks_SaveSlotId_ItemId",
                table: "PartyInventoryStacks",
                columns: new[] { "SaveSlotId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyMemberEquipment_ItemId",
                table: "PartyMemberEquipment",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyMemberEquipment_PartyMemberId_Slot",
                table: "PartyMemberEquipment",
                columns: new[] { "PartyMemberId", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyMembers_SaveSlotId_ActiveOrder",
                table: "PartyMembers",
                columns: new[] { "SaveSlotId", "ActiveOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyMemberSkills_PartyMemberId_SkillType",
                table: "PartyMemberSkills",
                columns: new[] { "PartyMemberId", "SkillType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyMemberStats_PartyMemberId_StatType",
                table: "PartyMemberStats",
                columns: new[] { "PartyMemberId", "StatType" },
                unique: true);

            migrationBuilder.Sql(@"
                UPDATE SaveSlots
                SET Currency = COALESCE((SELECT Currency FROM Players WHERE Players.SaveSlotId = SaveSlots.Id), 0),
                    Steps = COALESCE((SELECT Steps FROM Players WHERE Players.SaveSlotId = SaveSlots.Id), 0);

                INSERT INTO PartyMembers (Id, SaveSlotId, Name, Age, Gender, Species, CharacterClass, Steps, Location, ActiveOrder)
                SELECT Id, SaveSlotId, Name, Age, Gender, Species, CharacterClass, Steps, 'Active', 0
                FROM Players;

                INSERT INTO PartyMemberStats (PartyMemberId, StatType, BaseValue, MaxValue)
                SELECT PlayerId, StatType, BaseValue, MaxValue
                FROM PlayerStats;

                INSERT INTO PartyMemberSkills (PartyMemberId, SkillType, Name, CurrentLevel, Experience, MaxLevel)
                SELECT PlayerId, SkillType, Name, CurrentLevel, Experience, MaxLevel
                FROM PlayerSkills;

                INSERT INTO PartyMemberEquipment (PartyMemberId, Slot, ItemId)
                SELECT PlayerId, Slot, ItemId
                FROM PlayerEquipment;

                INSERT INTO PartyInventoryStacks (SaveSlotId, ItemId, Quantity)
                SELECT Players.SaveSlotId, PlayerInventoryStacks.ItemId, PlayerInventoryStacks.Quantity
                FROM PlayerInventoryStacks
                INNER JOIN Players ON Players.Id = PlayerInventoryStacks.PlayerId;
            ");

            migrationBuilder.DropTable(
                name: "PlayerEquipment");

            migrationBuilder.DropTable(
                name: "PlayerInventoryStacks");

            migrationBuilder.DropTable(
                name: "PlayerSkills");

            migrationBuilder.DropTable(
                name: "PlayerStats");

            migrationBuilder.DropTable(
                name: "Players");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartyInventoryStacks");

            migrationBuilder.DropTable(
                name: "PartyMemberEquipment");

            migrationBuilder.DropTable(
                name: "PartyMemberSkills");

            migrationBuilder.DropTable(
                name: "PartyMemberStats");

            migrationBuilder.DropTable(
                name: "PartyMembers");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "SaveSlots");

            migrationBuilder.DropColumn(
                name: "Steps",
                table: "SaveSlots");

            migrationBuilder.DropColumn(
                name: "SuggestedLevel",
                table: "ItemDefinitions");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "ItemDefinitions");

            migrationBuilder.DropColumn(
                name: "ThreatRating",
                table: "EnemyDefinitions");

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SaveSlotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterClass = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Currency = table.Column<long>(type: "INTEGER", nullable: false),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Species = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Steps = table.Column<long>(type: "INTEGER", nullable: false)
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
                name: "PlayerEquipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PlayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slot = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
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
                    ItemId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PlayerId = table.Column<Guid>(type: "TEXT", nullable: false),
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
                    CurrentLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Experience = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SkillType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
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
                    BaseValue = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxValue = table.Column<int>(type: "INTEGER", nullable: false),
                    StatType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
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
        }
    }
}

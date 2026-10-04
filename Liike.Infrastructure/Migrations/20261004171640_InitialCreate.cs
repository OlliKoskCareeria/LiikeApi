using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Liike.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organizers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Website = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SportEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Sport = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    Municipality = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    PriceDescription = table.Column<string>(type: "text", nullable: true),
                    RegistrationInfo = table.Column<string>(type: "text", nullable: true),
                    EquipmentInfo = table.Column<string>(type: "text", nullable: true),
                    ArrivalInfo = table.Column<string>(type: "text", nullable: true),
                    AdditionalInfo = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SportEvents", x => x.Id);
                    table.CheckConstraint("CK_SportEvents_Coordinates", "((\"Latitude\" IS NULL AND \"Longitude\" IS NULL) OR (\"Latitude\" IS NOT NULL AND \"Longitude\" IS NOT NULL)) AND (\"Latitude\" IS NULL OR \"Latitude\" BETWEEN -90 AND 90) AND (\"Longitude\" IS NULL OR \"Longitude\" BETWEEN -180 AND 180)");
                    table.CheckConstraint("CK_SportEvents_NonNegativePrice", "\"Price\" IS NULL OR \"Price\" >= 0");
                    table.CheckConstraint("CK_SportEvents_TimeRange", "\"EndTime\" > \"StartTime\"");
                    table.ForeignKey(
                        name: "FK_SportEvents_Organizers_OrganizerId",
                        column: x => x.OrganizerId,
                        principalTable: "Organizers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SportEventTags",
                columns: table => new
                {
                    SportEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tag = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SportEventTags", x => new { x.SportEventId, x.Tag });
                    table.CheckConstraint("CK_SportEventTags_ValidTag", "\"Tag\" > 0");
                    table.ForeignKey(
                        name: "FK_SportEventTags_SportEvents_SportEventId",
                        column: x => x.SportEventId,
                        principalTable: "SportEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Organizers_Email",
                table: "Organizers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_SportEvents_IsPublished_Category_StartTime",
                table: "SportEvents",
                columns: new[] { "IsPublished", "Category", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_SportEvents_IsPublished_Municipality_StartTime",
                table: "SportEvents",
                columns: new[] { "IsPublished", "Municipality", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_SportEvents_IsPublished_StartTime",
                table: "SportEvents",
                columns: new[] { "IsPublished", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_SportEvents_OrganizerId",
                table: "SportEvents",
                column: "OrganizerId");

            migrationBuilder.CreateIndex(
                name: "IX_SportEventTags_Tag_SportEventId",
                table: "SportEventTags",
                columns: new[] { "Tag", "SportEventId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SportEventTags");

            migrationBuilder.DropTable(
                name: "SportEvents");

            migrationBuilder.DropTable(
                name: "Organizers");
        }
    }
}

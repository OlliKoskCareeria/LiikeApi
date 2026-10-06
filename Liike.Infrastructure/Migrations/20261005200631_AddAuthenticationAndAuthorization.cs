using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Liike.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthenticationAndAuthorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SportEvents_Organizers_OrganizerId",
                table: "SportEvents");

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    OrganizerId = table.Column<Guid>(type: "uuid", nullable: true),
                    MustChangePassword = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAccounts_Organizers_OrganizerId",
                        column: x => x.OrganizerId,
                        principalTable: "Organizers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "Organizers"
                        GROUP BY UPPER(BTRIM("Email"))
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Resolve duplicate organizer emails before applying authentication migration.';
                    END IF;
                END $$;

                INSERT INTO "UserAccounts" (
                    "Id", "Email", "NormalizedEmail", "PasswordHash", "Role", "OrganizerId", "MustChangePassword")
                SELECT gen_random_uuid(), BTRIM(organizer."Email"), UPPER(BTRIM(organizer."Email")),
                       NULL, 1, organizer."Id", TRUE
                FROM "Organizers" AS organizer;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_NormalizedEmail",
                table: "UserAccounts",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_OrganizerId",
                table: "UserAccounts",
                column: "OrganizerId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SportEvents_Organizers_OrganizerId",
                table: "SportEvents",
                column: "OrganizerId",
                principalTable: "Organizers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SportEvents_Organizers_OrganizerId",
                table: "SportEvents");

            migrationBuilder.DropTable(
                name: "UserAccounts");

            migrationBuilder.AddForeignKey(
                name: "FK_SportEvents_Organizers_OrganizerId",
                table: "SportEvents",
                column: "OrganizerId",
                principalTable: "Organizers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

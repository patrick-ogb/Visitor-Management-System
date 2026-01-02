using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnterpriseUsers",
                columns: table => new
                {
                    EnterpriseUserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OnePortalUserId = table.Column<int>(type: "int", nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OnePortalEnterpriseId = table.Column<int>(type: "int", nullable: false),
                    EnterpriseId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnterpriseUsers", x => x.EnterpriseUserId);
                    table.ForeignKey(
                        name: "FK_EnterpriseUsers_Enterprises_EnterpriseId",
                        column: x => x.EnterpriseId,
                        principalTable: "Enterprises",
                        principalColumn: "EnterpriseId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseUsers_EmailAddress",
                table: "EnterpriseUsers",
                column: "EmailAddress");

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseUsers_EnterpriseId",
                table: "EnterpriseUsers",
                column: "EnterpriseId");

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseUsers_OnePortalEnterpriseId",
                table: "EnterpriseUsers",
                column: "OnePortalEnterpriseId");

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseUsers_OnePortalUserId",
                table: "EnterpriseUsers",
                column: "OnePortalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseUsers_OnePortalUserId_OnePortalEnterpriseId",
                table: "EnterpriseUsers",
                columns: new[] { "OnePortalUserId", "OnePortalEnterpriseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnterpriseUsers");
        }
    }
}

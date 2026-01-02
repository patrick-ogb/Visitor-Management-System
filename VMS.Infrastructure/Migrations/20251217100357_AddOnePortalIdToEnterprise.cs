using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOnePortalIdToEnterprise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OnePortalId",
                table: "Enterprises",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enterprises_OnePortalId",
                table: "Enterprises",
                column: "OnePortalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enterprises_OnePortalId",
                table: "Enterprises");

            migrationBuilder.DropColumn(
                name: "OnePortalId",
                table: "Enterprises");
        }
    }
}

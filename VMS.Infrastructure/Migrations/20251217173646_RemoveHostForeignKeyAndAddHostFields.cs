using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHostForeignKeyAndAddHostFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_WalkInRequests_WalkInRequestId",
                table: "Approvals");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestInvitations_AspNetUsers_HostId",
                table: "GuestInvitations");

            migrationBuilder.DropTable(
                name: "WalkInRequests");

            migrationBuilder.DropIndex(
                name: "IX_Approvals_WalkInRequestId",
                table: "Approvals");

            migrationBuilder.DropColumn(
                name: "WalkInRequestId",
                table: "Approvals");

            migrationBuilder.AlterColumn<int>(
                name: "HostId",
                table: "GuestInvitations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "EnterpriseUserId",
                table: "GuestInvitations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostEmail",
                table: "GuestInvitations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostName",
                table: "GuestInvitations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "HostType",
                table: "GuestInvitations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_GuestInvitations_EnterpriseUserId",
                table: "GuestInvitations",
                column: "EnterpriseUserId");

            // Migrate existing data: populate HostName and HostEmail from ApplicationUser
            migrationBuilder.Sql(@"
                UPDATE gi
                SET 
                    gi.HostName = COALESCE(au.FirstName + ' ' + au.LastName, 'Unknown'),
                    gi.HostEmail = au.Email,
                    gi.HostType = 0
                FROM GuestInvitations gi
                INNER JOIN AspNetUsers au ON gi.HostId = au.Id
                WHERE gi.HostName = '' OR gi.HostName IS NULL
            ");

            // Set default HostName for any records that couldn't be migrated
            migrationBuilder.Sql(@"
                UPDATE GuestInvitations
                SET HostName = 'Unknown', HostType = 0
                WHERE HostName = '' OR HostName IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropIndex(
                name: "IX_GuestInvitations_EnterpriseUserId",
                table: "GuestInvitations");

            migrationBuilder.DropColumn(
                name: "EnterpriseUserId",
                table: "GuestInvitations");

            migrationBuilder.DropColumn(
                name: "HostEmail",
                table: "GuestInvitations");

            migrationBuilder.DropColumn(
                name: "HostName",
                table: "GuestInvitations");

            migrationBuilder.DropColumn(
                name: "HostType",
                table: "GuestInvitations");

            migrationBuilder.AlterColumn<int>(
                name: "HostId",
                table: "GuestInvitations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WalkInRequestId",
                table: "Approvals",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WalkInRequests",
                columns: table => new
                {
                    WalkInRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnterpriseId = table.Column<int>(type: "int", nullable: true),
                    LfzStaffId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GuestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HostName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NumberOfGuests = table.Column<int>(type: "int", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestType = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    VehiclePlateNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalkInRequests", x => x.WalkInRequestId);
                    table.ForeignKey(
                        name: "FK_WalkInRequests_AspNetUsers_LfzStaffId",
                        column: x => x.LfzStaffId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WalkInRequests_Enterprises_EnterpriseId",
                        column: x => x.EnterpriseId,
                        principalTable: "Enterprises",
                        principalColumn: "EnterpriseId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_WalkInRequestId",
                table: "Approvals",
                column: "WalkInRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_WalkInRequests_EnterpriseId",
                table: "WalkInRequests",
                column: "EnterpriseId");

            migrationBuilder.CreateIndex(
                name: "IX_WalkInRequests_LfzStaffId",
                table: "WalkInRequests",
                column: "LfzStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_WalkInRequests_Status",
                table: "WalkInRequests",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_WalkInRequests_WalkInRequestId",
                table: "Approvals",
                column: "WalkInRequestId",
                principalTable: "WalkInRequests",
                principalColumn: "WalkInRequestId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestInvitations_AspNetUsers_HostId",
                table: "GuestInvitations",
                column: "HostId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

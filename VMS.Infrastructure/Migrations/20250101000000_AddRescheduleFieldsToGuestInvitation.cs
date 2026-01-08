using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRescheduleFieldsToGuestInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRescheduled",
                table: "GuestInvitations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RescheduledById",
                table: "GuestInvitations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RescheduledAt",
                table: "GuestInvitations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestInvitations_IsRescheduled",
                table: "GuestInvitations",
                column: "IsRescheduled");

            migrationBuilder.CreateIndex(
                name: "IX_GuestInvitations_RescheduledById",
                table: "GuestInvitations",
                column: "RescheduledById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GuestInvitations_RescheduledById",
                table: "GuestInvitations");

            migrationBuilder.DropIndex(
                name: "IX_GuestInvitations_IsRescheduled",
                table: "GuestInvitations");

            migrationBuilder.DropColumn(
                name: "RescheduledAt",
                table: "GuestInvitations");

            migrationBuilder.DropColumn(
                name: "RescheduledById",
                table: "GuestInvitations");

            migrationBuilder.DropColumn(
                name: "IsRescheduled",
                table: "GuestInvitations");
        }
    }
}


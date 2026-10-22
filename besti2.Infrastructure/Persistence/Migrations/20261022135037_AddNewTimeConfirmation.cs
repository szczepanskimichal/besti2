using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNewTimeConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NewTimeTokenHash",
                table: "Bookings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreviousStartUtc",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_NewTimeTokenHash",
                table: "Bookings",
                column: "NewTimeTokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_NewTimeTokenHash",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "NewTimeTokenHash",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PreviousStartUtc",
                table: "Bookings");
        }
    }
}

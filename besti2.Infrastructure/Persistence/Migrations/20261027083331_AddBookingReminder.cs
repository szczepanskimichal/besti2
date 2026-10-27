using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingReminder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedUtc",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentUtc",
                table: "Bookings",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedUtc",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReminderSentUtc",
                table: "Bookings");
        }
    }
}

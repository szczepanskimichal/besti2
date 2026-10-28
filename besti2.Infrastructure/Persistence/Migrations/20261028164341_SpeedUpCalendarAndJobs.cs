using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SpeedUpCalendarAndJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_ServiceId",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_EndUtc",
                table: "Bookings",
                column: "EndUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_NoShows",
                table: "Bookings",
                columns: new[] { "ServiceId", "CustomerEmail" },
                filter: "[Status] = 'MotteIkke'");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ReminderDue",
                table: "Bookings",
                column: "StartUtc",
                filter: "[Status] = 'Bekreftet' AND [ReminderSentUtc] IS NULL AND [NoReminder] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ServiceId_StartUtc",
                table: "Bookings",
                columns: new[] { "ServiceId", "StartUtc" })
                .Annotation("SqlServer:Include", new[] { "EndUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_EndUtc",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_NoShows",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ReminderDue",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ServiceId_StartUtc",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ServiceId",
                table: "Bookings",
                column: "ServiceId");
        }
    }
}

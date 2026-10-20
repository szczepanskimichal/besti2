using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueActiveBookingSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_EmployeeId_StartUtc",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_EmployeeId_StartUtc",
                table: "Bookings",
                columns: new[] { "EmployeeId", "StartUtc" },
                unique: true,
                filter: "[Status] <> 'Avlyst'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_EmployeeId_StartUtc",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_EmployeeId_StartUtc",
                table: "Bookings",
                columns: new[] { "EmployeeId", "StartUtc" });
        }
    }
}

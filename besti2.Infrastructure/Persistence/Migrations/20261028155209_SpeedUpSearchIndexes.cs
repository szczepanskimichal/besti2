using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SpeedUpSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Services_BusinessId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_BusinessId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_EmployeeId_StartUtc",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Services_BusinessId",
                table: "Services",
                column: "BusinessId")
                .Annotation("SqlServer:Include", new[] { "IsDeleted", "Name", "DurationMinutes" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BusinessId",
                table: "Reviews",
                column: "BusinessId")
                .Annotation("SqlServer:Include", new[] { "Stars" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_EmployeeId_StartUtc",
                table: "Bookings",
                columns: new[] { "EmployeeId", "StartUtc" },
                unique: true,
                filter: "[Status] <> 'Avlyst'")
                .Annotation("SqlServer:Include", new[] { "EndUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Services_BusinessId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_BusinessId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_EmployeeId_StartUtc",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Services_BusinessId",
                table: "Services",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BusinessId",
                table: "Reviews",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_EmployeeId_StartUtc",
                table: "Bookings",
                columns: new[] { "EmployeeId", "StartUtc" },
                unique: true,
                filter: "[Status] <> 'Avlyst'");
        }
    }
}

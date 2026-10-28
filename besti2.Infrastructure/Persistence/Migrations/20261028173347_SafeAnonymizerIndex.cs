using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SafeAnonymizerIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_EndUtc",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ToAnonymize",
                table: "Bookings",
                column: "EndUtc",
                filter: "[CustomerEmail] <> N'anonymisert@besti.invalid'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_ToAnonymize",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_EndUtc",
                table: "Bookings",
                column: "EndUtc");
        }
    }
}

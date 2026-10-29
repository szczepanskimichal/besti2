using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStatsAdminFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "Businesses",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedUtc",
                table: "Businesses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ProInterestUtc",
                table: "Businesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WeeklyReportOff",
                table: "Businesses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "WeeklyReportSentUtc",
                table: "Businesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceNok",
                table: "Bookings",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Favorites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Favorites_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "AdminNote", "CreatedUtc", "ProInterestUtc", "WeeklyReportOff", "WeeklyReportSentUtc" },
                values: new object[] { null, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, null });

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "AdminNote", "CreatedUtc", "ProInterestUtc", "WeeklyReportOff", "WeeklyReportSentUtc" },
                values: new object[] { null, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, null });

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "AdminNote", "CreatedUtc", "ProInterestUtc", "WeeklyReportOff", "WeeklyReportSentUtc" },
                values: new object[] { null, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, null });

            // Businesses from before this column: the approval time (or the sign-up agreement), otherwise now
            migrationBuilder.Sql(
                "UPDATE Businesses SET CreatedUtc = COALESCE(ApprovedUtc, DataProcessingAcceptedUtc, SYSUTCDATETIME()) " +
                "WHERE CreatedUtc = '0001-01-01'");

            // Bookings from before: the service's price today (fixed price, or the hourly price times the hours)
            migrationBuilder.Sql(
                "UPDATE b SET PriceNok = CASE s.PriceType WHEN 'FastPris' THEN s.PriceNok " +
                "ELSE s.PriceNok * (DATEDIFF(minute, b.StartUtc, b.EndUtc) / s.DurationMinutes) END " +
                "FROM Bookings b JOIN Services s ON s.Id = b.ServiceId " +
                "WHERE s.PriceNok IS NOT NULL AND s.DurationMinutes > 0 AND s.PriceType IN ('FastPris', 'PerTime')");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_BusinessId",
                table: "Favorites",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId_BusinessId",
                table: "Favorites",
                columns: new[] { "UserId", "BusinessId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Favorites");

            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "CreatedUtc",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "ProInterestUtc",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "WeeklyReportOff",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "WeeklyReportSentUtc",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "PriceNok",
                table: "Bookings");
        }
    }
}

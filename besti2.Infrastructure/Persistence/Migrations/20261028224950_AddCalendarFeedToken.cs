using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarFeedToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CalendarFeedToken",
                table: "Businesses",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CalendarFeedToken",
                value: null);

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CalendarFeedToken",
                value: null);

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CalendarFeedToken",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_CalendarFeedToken",
                table: "Businesses",
                column: "CalendarFeedToken",
                unique: true,
                filter: "[CalendarFeedToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Businesses_CalendarFeedToken",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "CalendarFeedToken",
                table: "Businesses");
        }
    }
}

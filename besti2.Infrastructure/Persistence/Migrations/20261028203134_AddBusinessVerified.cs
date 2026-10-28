using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessVerified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedUtc",
                table: "Businesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "VerifiedUtc",
                value: null);

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "VerifiedUtc",
                value: null);

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "VerifiedUtc",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerifiedUtc",
                table: "Businesses");
        }
    }
}

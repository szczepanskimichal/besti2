using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessReferralSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferralSource",
                table: "Businesses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "ReferralSource",
                value: null);

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "ReferralSource",
                value: null);

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "ReferralSource",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferralSource",
                table: "Businesses");
        }
    }
}

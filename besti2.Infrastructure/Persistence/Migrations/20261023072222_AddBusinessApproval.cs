using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalTokenHash",
                table: "Businesses",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedUtc",
                table: "Businesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataProcessingAcceptedUtc",
                table: "Businesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "ApprovalTokenHash", "ApprovedUtc", "DataProcessingAcceptedUtc" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "ApprovalTokenHash", "ApprovedUtc", "DataProcessingAcceptedUtc" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "ApprovalTokenHash", "ApprovedUtc", "DataProcessingAcceptedUtc" },
                values: new object[] { null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_ApprovalTokenHash",
                table: "Businesses",
                column: "ApprovalTokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Businesses_ApprovalTokenHash",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "ApprovalTokenHash",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "ApprovedUtc",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "DataProcessingAcceptedUtc",
                table: "Businesses");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedServicesAndEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "BusinessId", "LastName", "Name" },
                values: new object[,]
                {
                    { new Guid("b1111111-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111"), "Hansen", "Ingrid" },
                    { new Guid("b1111111-0000-0000-0000-000000000002"), new Guid("11111111-1111-1111-1111-111111111111"), "Berg", "Sofie" },
                    { new Guid("b2222222-0000-0000-0000-000000000001"), new Guid("22222222-2222-2222-2222-222222222222"), "Johansen", "Lars" },
                    { new Guid("b3333333-0000-0000-0000-000000000001"), new Guid("33333333-3333-3333-3333-333333333333"), "Nordmann", "Kari" }
                });

            migrationBuilder.InsertData(
                table: "Services",
                columns: new[] { "Id", "BusinessId", "Description", "DurationMinutes", "Name", "PriceNok", "PriceType" },
                values: new object[,]
                {
                    { new Guid("a1111111-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111"), "Klipp, vask og styling", 60, "Dameklipp", 790m, "FastPris" },
                    { new Guid("a1111111-0000-0000-0000-000000000002"), new Guid("11111111-1111-1111-1111-111111111111"), null, 30, "Herreklipp", 450m, "FastPris" },
                    { new Guid("a1111111-0000-0000-0000-000000000003"), new Guid("11111111-1111-1111-1111-111111111111"), "Inkludert vask og føn", 120, "Farging", 1490m, "FastPris" },
                    { new Guid("a2222222-0000-0000-0000-000000000001"), new Guid("22222222-2222-2222-2222-222222222222"), "Gratis befaring og tilbud", 60, "Befaring", null, "EtterAvtale" },
                    { new Guid("a2222222-0000-0000-0000-000000000002"), new Guid("22222222-2222-2222-2222-222222222222"), "Bad og kjøkken", 60, "Fliselegging", 950m, "PerKvadratmeter" },
                    { new Guid("a2222222-0000-0000-0000-000000000003"), new Guid("22222222-2222-2222-2222-222222222222"), null, 60, "Snekkerarbeid", 850m, "PerTime" },
                    { new Guid("a3333333-0000-0000-0000-000000000001"), new Guid("33333333-3333-3333-3333-333333333333"), "Individuell samtale, 50 minutter", 50, "Samtaleterapi", 1200m, "FastPris" },
                    { new Guid("a3333333-0000-0000-0000-000000000002"), new Guid("33333333-3333-3333-3333-333333333333"), null, 60, "Første konsultasjon", 950m, "FastPris" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("b1111111-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("b1111111-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("b2222222-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("b3333333-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("a1111111-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("a1111111-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("a1111111-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("a2222222-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("a2222222-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("a2222222-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("a3333333-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("a3333333-0000-0000-0000-000000000002"));

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}

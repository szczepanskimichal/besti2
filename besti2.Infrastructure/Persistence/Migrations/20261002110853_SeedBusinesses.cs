using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedBusinesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Businesses",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.InsertData(
                table: "Businesses",
                columns: new[] { "Id", "Address", "Category", "City", "Description", "Email", "Name", "OrgNumber", "PhoneNumber", "PostalCode" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Karl Johans gate 10", "Frisor", "Oslo", "Hårklipp og farging for hele familien.", "post@klippogstil.example", "Klipp & Stil Frisør", "999000001", "+4722000001", "0154" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Bryggen 5", "Bygg", "Bergen", "Befaring og oppussing av bad og kjøkken.", "kontakt@fjordbygg.example", "Fjord Bygg AS", "999000002", "+4755000002", "5003" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Munkegata 20", "Helse", "Trondheim", "Samtaleterapi for voksne.", "time@nordlyspsykolog.example", "Nordlys Psykologsenter", "999000003", "+4773000003", "7011" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Businesses");
        }
    }
}

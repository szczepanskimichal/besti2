using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHealthCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Health categories are removed from BusinessCategory (bookings can reveal health information).
            // Businesses that still use one move to Annet, otherwise they could not be loaded any more.
            migrationBuilder.Sql(
                "UPDATE [Businesses] SET [Category] = N'Annet' " +
                "WHERE [Category] IN (N'Helse', N'Fysioterapi', N'Kiropraktor', N'Naprapat', " +
                "N'Psykolog', N'Tannlege', N'Lege', N'Optiker');");

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "Category", "Description", "Email", "Name", "Slug" },
                values: new object[] { "Massasje", "Klassisk massasje og hot stone i Trondheim sentrum.", "time@nordlysmassasje.example", "Nordlys Massasje", "nordlys-massasje-trondheim" });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("a3333333-0000-0000-0000-000000000001"),
                columns: new[] { "Description", "Name", "PriceNok" },
                values: new object[] { "Hele kroppen, 50 minutter", "Klassisk massasje", 890m });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("a3333333-0000-0000-0000-000000000002"),
                columns: new[] { "Name", "PriceNok" },
                values: new object[] { "Hot stone-massasje", 990m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The old category of moved businesses is not known any more, they stay in Annet
            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "Category", "Description", "Email", "Name", "Slug" },
                values: new object[] { "Helse", "Samtaleterapi for voksne.", "time@nordlyspsykolog.example", "Nordlys Psykologsenter", "nordlys-psykologsenter-trondheim" });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("a3333333-0000-0000-0000-000000000001"),
                columns: new[] { "Description", "Name", "PriceNok" },
                values: new object[] { "Individuell samtale, 50 minutter", "Samtaleterapi", 1200m });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("a3333333-0000-0000-0000-000000000002"),
                columns: new[] { "Name", "PriceNok" },
                values: new object[] { "Første konsultasjon", 950m });
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Businesses",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "Slug",
                value: "klipp-og-stil-frisor-oslo");

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "Slug",
                value: "fjord-bygg-as-bergen");

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "Slug",
                value: "nordlys-psykologsenter-trondheim");

            // Businesses that already exist get an address from name + city, like BusinessSlug.From does
            // (æ ø å, & and the usual punctuation; the same address twice gets -2, -3 ...)
            migrationBuilder.Sql("""
                UPDATE Businesses SET Slug = LOWER(Name + N' ' + City) WHERE Slug = N'';
                UPDATE Businesses SET Slug =
                    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(Slug,
                        N'&', N' og '), N'æ', N'ae'), N'ø', N'o'), N'å', N'a'), N'é', N'e'), N'è', N'e'),
                        N'ü', N'u'), N'ö', N'o'), N'ä', N'a'), N'á', N'a');
                UPDATE Businesses SET Slug = TRANSLATE(Slug, N' .,''"()/\:;!?+*#@_', N'------------------');
                UPDATE Businesses SET Slug = REPLACE(REPLACE(REPLACE(Slug, N'----', N'-'), N'---', N'-'), N'--', N'-');
                UPDATE Businesses SET Slug = REPLACE(Slug, N'--', N'-');
                UPDATE Businesses SET Slug = LEFT(Slug, 75);
                UPDATE Businesses SET Slug = SUBSTRING(Slug, 2, 80) WHERE Slug LIKE N'-%';
                UPDATE Businesses SET Slug = LEFT(Slug, LEN(Slug) - 1) WHERE Slug LIKE N'%-';
                -- Anything still odd (other alphabets, emoji): a plain, safe address
                UPDATE Businesses SET Slug = N'bedrift-' + LEFT(LOWER(CONVERT(nvarchar(36), Id)), 8)
                    WHERE Slug = N'' OR Slug LIKE N'%[^a-z0-9-]%' COLLATE Latin1_General_BIN;
                WITH numbered AS (
                    SELECT Slug, ROW_NUMBER() OVER (PARTITION BY Slug ORDER BY Id) AS Number FROM Businesses)
                UPDATE numbered SET Slug = Slug + N'-' + CAST(Number AS nvarchar(10)) WHERE Number > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_Slug",
                table: "Businesses",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Businesses_Slug",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Businesses");
        }
    }
}

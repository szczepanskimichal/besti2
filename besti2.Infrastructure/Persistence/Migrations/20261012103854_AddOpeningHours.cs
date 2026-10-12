using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOpeningHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpeningHours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    OpensAt = table.Column<TimeOnly>(type: "time", nullable: false),
                    ClosesAt = table.Column<TimeOnly>(type: "time", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpeningHours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpeningHours_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "OpeningHours",
                columns: new[] { "Id", "BusinessId", "ClosesAt", "DayOfWeek", "OpensAt" },
                values: new object[,]
                {
                    { new Guid("c0000000-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111"), new TimeOnly(17, 0, 0), 1, new TimeOnly(9, 0, 0) },
                    { new Guid("c0000000-0000-0000-0000-000000000002"), new Guid("11111111-1111-1111-1111-111111111111"), new TimeOnly(17, 0, 0), 2, new TimeOnly(9, 0, 0) },
                    { new Guid("c0000000-0000-0000-0000-000000000003"), new Guid("11111111-1111-1111-1111-111111111111"), new TimeOnly(17, 0, 0), 3, new TimeOnly(9, 0, 0) },
                    { new Guid("c0000000-0000-0000-0000-000000000004"), new Guid("11111111-1111-1111-1111-111111111111"), new TimeOnly(19, 0, 0), 4, new TimeOnly(9, 0, 0) },
                    { new Guid("c0000000-0000-0000-0000-000000000005"), new Guid("11111111-1111-1111-1111-111111111111"), new TimeOnly(17, 0, 0), 5, new TimeOnly(9, 0, 0) },
                    { new Guid("c0000000-0000-0000-0000-000000000006"), new Guid("11111111-1111-1111-1111-111111111111"), new TimeOnly(15, 0, 0), 6, new TimeOnly(10, 0, 0) },
                    { new Guid("c0000000-0000-0000-0000-000000000007"), new Guid("22222222-2222-2222-2222-222222222222"), new TimeOnly(15, 0, 0), 1, new TimeOnly(7, 0, 0) },
                    { new Guid("c0000000-0000-0000-0000-000000000008"), new Guid("22222222-2222-2222-2222-222222222222"), new TimeOnly(15, 0, 0), 2, new TimeOnly(7, 0, 0) },
                    { new Guid("c0000000-0000-0000-0000-000000000009"), new Guid("22222222-2222-2222-2222-222222222222"), new TimeOnly(15, 0, 0), 3, new TimeOnly(7, 0, 0) },
                    { new Guid("c0000000-0000-0000-0000-000000000010"), new Guid("22222222-2222-2222-2222-222222222222"), new TimeOnly(15, 0, 0), 4, new TimeOnly(7, 0, 0) },
                    { new Guid("c0000000-0000-0000-0000-000000000011"), new Guid("22222222-2222-2222-2222-222222222222"), new TimeOnly(15, 0, 0), 5, new TimeOnly(7, 0, 0) },
                    { new Guid("c0000000-0000-0000-0000-000000000012"), new Guid("33333333-3333-3333-3333-333333333333"), new TimeOnly(16, 0, 0), 1, new TimeOnly(8, 0, 0) },
                    { new Guid("c0000000-0000-0000-0000-000000000013"), new Guid("33333333-3333-3333-3333-333333333333"), new TimeOnly(16, 0, 0), 2, new TimeOnly(8, 0, 0) },
                    { new Guid("c0000000-0000-0000-0000-000000000014"), new Guid("33333333-3333-3333-3333-333333333333"), new TimeOnly(16, 0, 0), 3, new TimeOnly(8, 0, 0) },
                    { new Guid("c0000000-0000-0000-0000-000000000015"), new Guid("33333333-3333-3333-3333-333333333333"), new TimeOnly(16, 0, 0), 4, new TimeOnly(8, 0, 0) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpeningHours_BusinessId_DayOfWeek",
                table: "OpeningHours",
                columns: new[] { "BusinessId", "DayOfWeek" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpeningHours");
        }
    }
}

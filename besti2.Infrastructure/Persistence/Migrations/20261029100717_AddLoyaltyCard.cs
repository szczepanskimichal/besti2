using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltyCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LoyaltyReward",
                table: "Businesses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LoyaltyStartUtc",
                table: "Businesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoyaltyVisits",
                table: "Businesses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LoyaltyRedemptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RedeemedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyRedemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyRedemptions_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "LoyaltyReward", "LoyaltyStartUtc", "LoyaltyVisits" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "LoyaltyReward", "LoyaltyStartUtc", "LoyaltyVisits" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "LoyaltyReward", "LoyaltyStartUtc", "LoyaltyVisits" },
                values: new object[] { null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyRedemptions_BusinessId_CustomerUserId",
                table: "LoyaltyRedemptions",
                columns: new[] { "BusinessId", "CustomerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyRedemptions_CustomerUserId",
                table: "LoyaltyRedemptions",
                column: "CustomerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoyaltyRedemptions");

            migrationBuilder.DropColumn(
                name: "LoyaltyReward",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "LoyaltyStartUtc",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "LoyaltyVisits",
                table: "Businesses");
        }
    }
}

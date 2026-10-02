using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeBusinessModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Salons_SalonId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Salons_SalonId",
                table: "Services");

            migrationBuilder.DropTable(
                name: "Salons");

            migrationBuilder.RenameColumn(
                name: "SalonId",
                table: "Services",
                newName: "BusinessId");

            migrationBuilder.RenameIndex(
                name: "IX_Services_SalonId",
                table: "Services",
                newName: "IX_Services_BusinessId");

            migrationBuilder.RenameColumn(
                name: "SalonId",
                table: "Employees",
                newName: "BusinessId");

            migrationBuilder.RenameIndex(
                name: "IX_Employees_SalonId",
                table: "Employees",
                newName: "IX_Employees_BusinessId");

            migrationBuilder.AlterColumn<decimal>(
                name: "PriceNok",
                table: "Services",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "PriceType",
                table: "Services",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Bookings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Bookings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrgNumber = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    City = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Businesses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_OrgNumber",
                table: "Businesses",
                column: "OrgNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Businesses_BusinessId",
                table: "Employees",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Businesses_BusinessId",
                table: "Services",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Businesses_BusinessId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Businesses_BusinessId",
                table: "Services");

            migrationBuilder.DropTable(
                name: "Businesses");

            migrationBuilder.DropColumn(
                name: "PriceType",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "BusinessId",
                table: "Services",
                newName: "SalonId");

            migrationBuilder.RenameIndex(
                name: "IX_Services_BusinessId",
                table: "Services",
                newName: "IX_Services_SalonId");

            migrationBuilder.RenameColumn(
                name: "BusinessId",
                table: "Employees",
                newName: "SalonId");

            migrationBuilder.RenameIndex(
                name: "IX_Employees_BusinessId",
                table: "Employees",
                newName: "IX_Employees_SalonId");

            migrationBuilder.AlterColumn<decimal>(
                name: "PriceNok",
                table: "Services",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Salons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salons", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Salons_SalonId",
                table: "Employees",
                column: "SalonId",
                principalTable: "Salons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Salons_SalonId",
                table: "Services",
                column: "SalonId",
                principalTable: "Salons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

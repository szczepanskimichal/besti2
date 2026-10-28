using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InviteId",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookingInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InviterUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    InviterPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InviterNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Units = table.Column<int>(type: "int", nullable: false),
                    Persons = table.Column<int>(type: "int", nullable: false),
                    OrganizerJoins = table.Column<bool>(type: "bit", nullable: false),
                    PayMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnsweredUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingInvites_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingInvites_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_InviteId",
                table: "Bookings",
                column: "InviteId",
                filter: "[InviteId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BookingInvites_BusinessId",
                table: "BookingInvites",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingInvites_ExpiresUtc",
                table: "BookingInvites",
                column: "ExpiresUtc",
                filter: "[ClosedUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BookingInvites_InviterUserId",
                table: "BookingInvites",
                column: "InviterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingInvites_ServiceId",
                table: "BookingInvites",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingInvites_TokenHash",
                table: "BookingInvites",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingInvites");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_InviteId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "InviteId",
                table: "Bookings");
        }
    }
}

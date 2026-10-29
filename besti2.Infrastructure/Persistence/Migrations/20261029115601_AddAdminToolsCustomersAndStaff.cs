using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminToolsCustomersAndStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HiddenReason",
                table: "Reviews",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HiddenUtc",
                table: "Reviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoFileName",
                table: "Reviews",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "BreakEnd",
                table: "OpeningHours",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "BreakStart",
                table: "OpeningHours",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffEmail",
                table: "Employees",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StaffInviteExpiresUtc",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffInviteTokenHash",
                table: "Employees",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffUserId",
                table: "Employees",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuspendedReason",
                table: "Businesses",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspendedUtc",
                table: "Businesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SoonNoticeSentUtc",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReminderEmailOff",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SoonNoticeOff",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AdminActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CustomerPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerNotes_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewHelpfuls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewHelpfuls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewHelpfuls_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "Reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "SuspendedReason", "SuspendedUtc" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "SuspendedReason", "SuspendedUtc" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "SuspendedReason", "SuspendedUtc" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("b1111111-0000-0000-0000-000000000001"),
                columns: new[] { "StaffEmail", "StaffInviteExpiresUtc", "StaffInviteTokenHash", "StaffUserId" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("b1111111-0000-0000-0000-000000000002"),
                columns: new[] { "StaffEmail", "StaffInviteExpiresUtc", "StaffInviteTokenHash", "StaffUserId" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("b2222222-0000-0000-0000-000000000001"),
                columns: new[] { "StaffEmail", "StaffInviteExpiresUtc", "StaffInviteTokenHash", "StaffUserId" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("b3333333-0000-0000-0000-000000000001"),
                columns: new[] { "StaffEmail", "StaffInviteExpiresUtc", "StaffInviteTokenHash", "StaffUserId" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000001"),
                columns: new[] { "BreakEnd", "BreakStart" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000002"),
                columns: new[] { "BreakEnd", "BreakStart" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000003"),
                columns: new[] { "BreakEnd", "BreakStart" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000004"),
                columns: new[] { "BreakEnd", "BreakStart" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000005"),
                columns: new[] { "BreakEnd", "BreakStart" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000006"),
                columns: new[] { "BreakEnd", "BreakStart" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000007"),
                columns: new[] { "BreakEnd", "BreakStart" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000008"),
                columns: new[] { "BreakEnd", "BreakStart" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000009"),
                columns: new[] { "BreakEnd", "BreakStart" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000010"),
                columns: new[] { "BreakEnd", "BreakStart" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000011"),
                columns: new[] { "BreakEnd", "BreakStart" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000012"),
                columns: new[] { "BreakEnd", "BreakStart" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000013"),
                columns: new[] { "BreakEnd", "BreakStart" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000014"),
                columns: new[] { "BreakEnd", "BreakStart" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000015"),
                columns: new[] { "BreakEnd", "BreakStart" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_StaffInviteTokenHash",
                table: "Employees",
                column: "StaffInviteTokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_StaffUserId",
                table: "Employees",
                column: "StaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminActions_BusinessId",
                table: "AdminActions",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminActions_CreatedUtc",
                table: "AdminActions",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerNotes_BusinessId_CustomerPhone",
                table: "CustomerNotes",
                columns: new[] { "BusinessId", "CustomerPhone" },
                unique: true,
                filter: "[CustomerPhone] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerNotes_BusinessId_CustomerUserId",
                table: "CustomerNotes",
                columns: new[] { "BusinessId", "CustomerUserId" },
                unique: true,
                filter: "[CustomerUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerNotes_CustomerUserId",
                table: "CustomerNotes",
                column: "CustomerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewHelpfuls_ReviewId_UserId",
                table: "ReviewHelpfuls",
                columns: new[] { "ReviewId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewHelpfuls_UserId",
                table: "ReviewHelpfuls",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminActions");

            migrationBuilder.DropTable(
                name: "CustomerNotes");

            migrationBuilder.DropTable(
                name: "ReviewHelpfuls");

            migrationBuilder.DropIndex(
                name: "IX_Employees_StaffInviteTokenHash",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_StaffUserId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "HiddenReason",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "HiddenUtc",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "PhotoFileName",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "BreakEnd",
                table: "OpeningHours");

            migrationBuilder.DropColumn(
                name: "BreakStart",
                table: "OpeningHours");

            migrationBuilder.DropColumn(
                name: "StaffEmail",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "StaffInviteExpiresUtc",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "StaffInviteTokenHash",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "StaffUserId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SuspendedReason",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "SuspendedUtc",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "SoonNoticeSentUtc",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CreatedUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ReminderEmailOff",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SoonNoticeOff",
                table: "AspNetUsers");
        }
    }
}

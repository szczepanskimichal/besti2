using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeBusinessEmailOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Businesses",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Businesses SET Email = '' WHERE Email IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Businesses",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);
            // The login e-mail was public until now: remove it from the business page
            migrationBuilder.Sql(
                "UPDATE b SET Email = NULL FROM Businesses b WHERE EXISTS (SELECT 1 FROM AspNetUsers u " +
                "WHERE u.BusinessId = b.Id AND u.NormalizedEmail = UPPER(b.Email))");
        }
    }
}

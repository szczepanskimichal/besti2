using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace besti2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFreeCancelHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing businesses get the default: free cancellation until 24 hours before the visit
            migrationBuilder.AddColumn<int>(
                name: "FreeCancelHours",
                table: "Businesses",
                type: "int",
                nullable: false,
                defaultValue: 24);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FreeCancelHours",
                table: "Businesses");
        }
    }
}

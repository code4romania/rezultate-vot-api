using Microsoft.EntityFrameworkCore.Migrations;

namespace ElectionResults.Core.Migrations
{
    public partial class AddedShortNameForCounty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShortName",
                table: "counties",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShortName",
                table: "counties");
        }
    }
}

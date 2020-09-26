using Microsoft.EntityFrameworkCore.Migrations;

namespace ElectionResults.Core.Migrations
{
    public partial class AddedLivePropertyForElection : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Live",
                table: "elections",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Live",
                table: "elections");
        }
    }
}

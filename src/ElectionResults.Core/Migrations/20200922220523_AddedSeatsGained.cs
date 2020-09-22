using Microsoft.EntityFrameworkCore.Migrations;

namespace ElectionResults.Core.Migrations
{
    public partial class AddedSeatsGained : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeatsGained",
                table: "candidateresults",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeatsGained",
                table: "candidateresults");
        }
    }
}

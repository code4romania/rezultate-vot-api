using Microsoft.EntityFrameworkCore.Migrations;

namespace ElectionResults.Core.Migrations
{
    public partial class AddedSiruta : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CountySiruta",
                table: "localities",
                nullable: false,
                defaultValue: 0);

            /*migrationBuilder.AddColumn<int>(
                name: "Siruta",
                table: "localities",
                nullable: false,
                defaultValue: 0);*/

           
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountySiruta",
                table: "localities");

            migrationBuilder.DropColumn(
                name: "Siruta",
                table: "localities");

            migrationBuilder.DropColumn(
                name: "BallotPosition",
                table: "candidateresults");
        }
    }
}

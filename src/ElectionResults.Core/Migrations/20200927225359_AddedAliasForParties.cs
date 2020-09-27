using Microsoft.EntityFrameworkCore.Migrations;

namespace ElectionResults.Core.Migrations
{
    public partial class AddedAliasForParties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*migrationBuilder.AddColumn<string>(
                name: "Alias",
                table: "parties",
                nullable: true);*/
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Alias",
                table: "parties");
        }
    }
}

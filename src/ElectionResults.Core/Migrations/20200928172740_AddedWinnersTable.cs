using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

namespace ElectionResults.Core.Migrations
{
    public partial class AddedWinnersTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Winners",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true),
                    Votes = table.Column<int>(nullable: false),
                    CandidateId = table.Column<int>(nullable: true),
                    PartyId = table.Column<int>(nullable: true),
                    BallotId = table.Column<int>(nullable: true),
                    TurnoutId = table.Column<int>(nullable: true),
                    Division = table.Column<int>(nullable: false),
                    CountyId = table.Column<int>(nullable: true),
                    CountryId = table.Column<int>(nullable: true),
                    LocalityId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Winners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Winners_ballots_BallotId",
                        column: x => x.BallotId,
                        principalTable: "ballots",
                        principalColumn: "BallotId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Winners_candidateresults_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidateresults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Winners_parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Winners_turnouts_TurnoutId",
                        column: x => x.TurnoutId,
                        principalTable: "turnouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Winners_BallotId",
                table: "Winners",
                column: "BallotId");

            migrationBuilder.CreateIndex(
                name: "IX_Winners_CandidateId",
                table: "Winners",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_Winners_PartyId",
                table: "Winners",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_Winners_TurnoutId",
                table: "Winners",
                column: "TurnoutId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Winners");
        }
    }
}

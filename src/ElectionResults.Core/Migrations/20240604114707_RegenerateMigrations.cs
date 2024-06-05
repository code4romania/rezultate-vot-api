using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ElectionResults.Core.Migrations
{
    /// <inheritdoc />
    public partial class RegenerateMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(127)", maxLength: 127, nullable: false),
                    Name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "varchar(256)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false),
                    UserName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: true),
                    SecurityStamp = table.Column<string>(type: "longtext", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "longtext", nullable: true),
                    PhoneNumber = table.Column<string>(type: "longtext", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "authors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true),
                    Avatar = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authors", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "counties",
                columns: table => new
                {
                    CountyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true),
                    ShortName = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_counties", x => x.CountyId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_countries", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "elections",
                columns: table => new
                {
                    ElectionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true),
                    Subtitle = table.Column<string>(type: "longtext", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Live = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_elections", x => x.ElectionId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "observations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    BallotId = table.Column<int>(type: "int", nullable: false),
                    CoveredPollingPlaces = table.Column<int>(type: "int", nullable: false),
                    CoveredCounties = table.Column<int>(type: "int", nullable: false),
                    ObserverCount = table.Column<int>(type: "int", nullable: false),
                    MessageCount = table.Column<int>(type: "int", nullable: false),
                    IssueCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_observations", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "parties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true),
                    ShortName = table.Column<string>(type: "longtext", nullable: true),
                    LogoUrl = table.Column<string>(type: "longtext", nullable: true),
                    Color = table.Column<string>(type: "longtext", nullable: true),
                    Alias = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parties", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "partyresults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    BallotId = table.Column<int>(type: "int", nullable: false),
                    PartyId = table.Column<int>(type: "int", nullable: false),
                    Votes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partyresults", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "turnouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    BallotId = table.Column<int>(type: "int", nullable: false),
                    CountyId = table.Column<int>(type: "int", nullable: true),
                    LocalityId = table.Column<int>(type: "int", nullable: true),
                    EligibleVoters = table.Column<int>(type: "int", nullable: false),
                    TotalVotes = table.Column<int>(type: "int", nullable: false),
                    NullVotes = table.Column<int>(type: "int", nullable: false),
                    VotesByMail = table.Column<int>(type: "int", nullable: false),
                    ValidVotes = table.Column<int>(type: "int", nullable: false),
                    TotalSeats = table.Column<int>(type: "int", nullable: false),
                    Coefficient = table.Column<int>(type: "int", nullable: false),
                    Threshold = table.Column<int>(type: "int", nullable: false),
                    Circumscription = table.Column<int>(type: "int", nullable: false),
                    MinVotes = table.Column<int>(type: "int", nullable: false),
                    Division = table.Column<int>(type: "int", nullable: false),
                    Mandates = table.Column<int>(type: "int", nullable: false),
                    CorrespondenceVotes = table.Column<int>(type: "int", nullable: false),
                    PermanentListsVotes = table.Column<int>(type: "int", nullable: false),
                    SpecialListsVotes = table.Column<int>(type: "int", nullable: false),
                    SuplimentaryVotes = table.Column<int>(type: "int", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_turnouts", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    RoleId = table.Column<string>(type: "varchar(127)", nullable: false),
                    ClaimType = table.Column<string>(type: "longtext", nullable: true),
                    ClaimValue = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false),
                    ClaimType = table.Column<string>(type: "longtext", nullable: true),
                    ClaimValue = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "varchar(127)", maxLength: 127, nullable: false),
                    ProviderKey = table.Column<string>(type: "varchar(127)", maxLength: 127, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "longtext", nullable: true),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "varchar(127)", maxLength: 127, nullable: false),
                    RoleId = table.Column<string>(type: "varchar(127)", maxLength: 127, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "varchar(127)", maxLength: 127, nullable: false),
                    LoginProvider = table.Column<string>(type: "varchar(127)", maxLength: 127, nullable: false),
                    Name = table.Column<string>(type: "varchar(127)", maxLength: 127, nullable: false),
                    Value = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "localities",
                columns: table => new
                {
                    LocalityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CountyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    Siruta = table.Column<int>(type: "int", nullable: false),
                    CountySiruta = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_localities", x => x.LocalityId);
                    table.ForeignKey(
                        name: "FK_localities_counties_CountyId",
                        column: x => x.CountyId,
                        principalTable: "counties",
                        principalColumn: "CountyId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ballots",
                columns: table => new
                {
                    BallotId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true),
                    BallotType = table.Column<int>(type: "int", nullable: false),
                    TurnoutId = table.Column<int>(type: "int", nullable: true),
                    ElectionId = table.Column<int>(type: "int", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ballots", x => x.BallotId);
                    table.ForeignKey(
                        name: "FK_ballots_elections_ElectionId",
                        column: x => x.ElectionId,
                        principalTable: "elections",
                        principalColumn: "ElectionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ballots_turnouts_TurnoutId",
                        column: x => x.TurnoutId,
                        principalTable: "turnouts",
                        principalColumn: "Id");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "articles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ElectionId = table.Column<int>(type: "int", nullable: false),
                    BallotId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AuthorId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: true),
                    Body = table.Column<string>(type: "longtext", nullable: true),
                    Link = table.Column<string>(type: "longtext", nullable: true),
                    Embed = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_articles_authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_articles_ballots_BallotId",
                        column: x => x.BallotId,
                        principalTable: "ballots",
                        principalColumn: "BallotId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_articles_elections_ElectionId",
                        column: x => x.ElectionId,
                        principalTable: "elections",
                        principalColumn: "ElectionId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "candidateresults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Votes = table.Column<int>(type: "int", nullable: false),
                    BallotId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true),
                    ShortName = table.Column<string>(type: "longtext", nullable: true),
                    PartyName = table.Column<string>(type: "longtext", nullable: true),
                    PartyId = table.Column<int>(type: "int", nullable: true),
                    YesVotes = table.Column<int>(type: "int", nullable: false),
                    NoVotes = table.Column<int>(type: "int", nullable: false),
                    SeatsGained = table.Column<int>(type: "int", nullable: false),
                    Division = table.Column<int>(type: "int", nullable: false),
                    CountyId = table.Column<int>(type: "int", nullable: true),
                    LocalityId = table.Column<int>(type: "int", nullable: true),
                    TotalSeats = table.Column<int>(type: "int", nullable: false),
                    Seats1 = table.Column<int>(type: "int", nullable: false),
                    Seats2 = table.Column<int>(type: "int", nullable: false),
                    OverElectoralThreshold = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    BallotPosition = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidateresults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_candidateresults_ballots_BallotId",
                        column: x => x.BallotId,
                        principalTable: "ballots",
                        principalColumn: "BallotId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_candidateresults_counties_CountyId",
                        column: x => x.CountyId,
                        principalTable: "counties",
                        principalColumn: "CountyId");
                    table.ForeignKey(
                        name: "FK_candidateresults_localities_LocalityId",
                        column: x => x.LocalityId,
                        principalTable: "localities",
                        principalColumn: "LocalityId");
                    table.ForeignKey(
                        name: "FK_candidateresults_parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "parties",
                        principalColumn: "Id");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "articlepictures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articlepictures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_articlepictures_articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "winners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true),
                    Votes = table.Column<int>(type: "int", nullable: false),
                    CandidateId = table.Column<int>(type: "int", nullable: true),
                    PartyId = table.Column<int>(type: "int", nullable: true),
                    BallotId = table.Column<int>(type: "int", nullable: true),
                    TurnoutId = table.Column<int>(type: "int", nullable: true),
                    Division = table.Column<int>(type: "int", nullable: false),
                    CountyId = table.Column<int>(type: "int", nullable: true),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    LocalityId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_winners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_winners_ballots_BallotId",
                        column: x => x.BallotId,
                        principalTable: "ballots",
                        principalColumn: "BallotId");
                    table.ForeignKey(
                        name: "FK_winners_candidateresults_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidateresults",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_winners_parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "parties",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_winners_turnouts_TurnoutId",
                        column: x => x.TurnoutId,
                        principalTable: "turnouts",
                        principalColumn: "Id");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_articlepictures_ArticleId",
                table: "articlepictures",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_articles_AuthorId",
                table: "articles",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_articles_BallotId",
                table: "articles",
                column: "BallotId");

            migrationBuilder.CreateIndex(
                name: "IX_articles_ElectionId",
                table: "articles",
                column: "ElectionId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ballots_ElectionId",
                table: "ballots",
                column: "ElectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ballots_TurnoutId",
                table: "ballots",
                column: "TurnoutId");

            migrationBuilder.CreateIndex(
                name: "IX_candidateresults_BallotId",
                table: "candidateresults",
                column: "BallotId");

            migrationBuilder.CreateIndex(
                name: "IX_candidateresults_CountyId",
                table: "candidateresults",
                column: "CountyId");

            migrationBuilder.CreateIndex(
                name: "IX_candidateresults_LocalityId",
                table: "candidateresults",
                column: "LocalityId");

            migrationBuilder.CreateIndex(
                name: "IX_candidateresults_PartyId",
                table: "candidateresults",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_localities_CountyId",
                table: "localities",
                column: "CountyId");

            migrationBuilder.CreateIndex(
                name: "IX_winners_BallotId",
                table: "winners",
                column: "BallotId");

            migrationBuilder.CreateIndex(
                name: "IX_winners_CandidateId",
                table: "winners",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_winners_PartyId",
                table: "winners",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_winners_TurnoutId",
                table: "winners",
                column: "TurnoutId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "articlepictures");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "countries");

            migrationBuilder.DropTable(
                name: "observations");

            migrationBuilder.DropTable(
                name: "partyresults");

            migrationBuilder.DropTable(
                name: "winners");

            migrationBuilder.DropTable(
                name: "articles");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "candidateresults");

            migrationBuilder.DropTable(
                name: "authors");

            migrationBuilder.DropTable(
                name: "ballots");

            migrationBuilder.DropTable(
                name: "localities");

            migrationBuilder.DropTable(
                name: "parties");

            migrationBuilder.DropTable(
                name: "elections");

            migrationBuilder.DropTable(
                name: "turnouts");

            migrationBuilder.DropTable(
                name: "counties");
        }
    }
}

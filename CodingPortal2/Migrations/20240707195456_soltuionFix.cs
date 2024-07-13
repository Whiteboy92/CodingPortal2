using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodingPortal2.Migrations
{
    public partial class soltuionFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Plagiarisms",
                columns: table => new
                {
                    PlagiarismId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserSolutionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plagiarisms", x => x.PlagiarismId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AssignmentGroup",
                columns: table => new
                {
                    AssignmentsInGroupAssignmentId = table.Column<int>(type: "int", nullable: false),
                    GroupsGroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentGroup", x => new { x.AssignmentsInGroupAssignmentId, x.GroupsGroupId });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalPoints = table.Column<int>(type: "int", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UploadFrequency = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    PathToTests = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProgrammingLanguage = table.Column<int>(type: "int", nullable: false),
                    CreatorUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.AssignmentId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Login = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PermissionLevel = table.Column<int>(type: "int", nullable: false),
                    AssignmentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Year = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Semester = table.Column<int>(type: "int", nullable: false),
                    CreatorUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.GroupId);
                    table.ForeignKey(
                        name: "FK_Groups_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserAssignmentDates",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    AssignmentTime = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    DeadLineDateTime = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LastUploadDateTime = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    TimeToNextUpload = table.Column<TimeSpan>(type: "time(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAssignmentDates", x => new { x.UserId, x.AssignmentId });
                    table.ForeignKey(
                        name: "FK_UserAssignmentDates_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAssignmentDates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserAssignmentSolutions",
                columns: table => new
                {
                    UserAssignmentSolutionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PlagiarismId = table.Column<int>(type: "int", nullable: false),
                    Solution = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NoFormatSolution = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UploadDateTime = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    TestPassed = table.Column<int>(type: "int", nullable: false),
                    TotalTests = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAssignmentSolutions", x => x.UserAssignmentSolutionId);
                    table.ForeignKey(
                        name: "FK_UserAssignmentSolutions_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAssignmentSolutions_Plagiarisms_PlagiarismId",
                        column: x => x.PlagiarismId,
                        principalTable: "Plagiarisms",
                        principalColumn: "PlagiarismId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAssignmentSolutions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => new { x.UserId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_UserGroups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGroups_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlagiarismEntries",
                columns: table => new
                {
                    PlagiarismEntryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlagiarisedSolutionId = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<double>(type: "double", nullable: false),
                    PlagiarismId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlagiarismEntries", x => x.PlagiarismEntryId);
                    table.ForeignKey(
                        name: "FK_PlagiarismEntries_Plagiarisms_PlagiarismId",
                        column: x => x.PlagiarismId,
                        principalTable: "Plagiarisms",
                        principalColumn: "PlagiarismId");
                    table.ForeignKey(
                        name: "FK_PlagiarismEntries_UserAssignmentSolutions_PlagiarisedSolutio~",
                        column: x => x.PlagiarisedSolutionId,
                        principalTable: "UserAssignmentSolutions",
                        principalColumn: "UserAssignmentSolutionId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentGroup_GroupsGroupId",
                table: "AssignmentGroup",
                column: "GroupsGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_CreatorUserId",
                table: "Assignments",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_CreatorUserId",
                table: "Groups",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlagiarismEntries_PlagiarisedSolutionId",
                table: "PlagiarismEntries",
                column: "PlagiarisedSolutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlagiarismEntries_PlagiarismId",
                table: "PlagiarismEntries",
                column: "PlagiarismId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAssignmentDates_AssignmentId",
                table: "UserAssignmentDates",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAssignmentSolutions_AssignmentId",
                table: "UserAssignmentSolutions",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAssignmentSolutions_PlagiarismId",
                table: "UserAssignmentSolutions",
                column: "PlagiarismId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAssignmentSolutions_UserId",
                table: "UserAssignmentSolutions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_GroupId",
                table: "UserGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AssignmentId",
                table: "Users",
                column: "AssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentGroup_Assignments_AssignmentsInGroupAssignmentId",
                table: "AssignmentGroup",
                column: "AssignmentsInGroupAssignmentId",
                principalTable: "Assignments",
                principalColumn: "AssignmentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentGroup_Groups_GroupsGroupId",
                table: "AssignmentGroup",
                column: "GroupsGroupId",
                principalTable: "Groups",
                principalColumn: "GroupId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Users_CreatorUserId",
                table: "Assignments",
                column: "CreatorUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Assignments_AssignmentId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "AssignmentGroup");

            migrationBuilder.DropTable(
                name: "PlagiarismEntries");

            migrationBuilder.DropTable(
                name: "UserAssignmentDates");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropTable(
                name: "UserAssignmentSolutions");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Plagiarisms");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

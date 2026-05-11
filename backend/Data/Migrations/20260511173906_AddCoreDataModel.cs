using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resumaire.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCoreDataModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BaseResumes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    ContentJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseResumes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaseResumes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Link = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DateApplied = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    SelectedBaseResumeId = table.Column<Guid>(type: "uuid", nullable: true),
                    SelectedTailoredResumeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jobs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Jobs_BaseResumes_SelectedBaseResumeId",
                        column: x => x.SelectedBaseResumeId,
                        principalTable: "BaseResumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TailoredResumes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceBaseResumeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceBaseResumeRevision = table.Column<int>(type: "integer", nullable: false),
                    SourceBaseResumeContentJson = table.Column<string>(type: "jsonb", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContentJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TailoredResumes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TailoredResumes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TailoredResumes_BaseResumes_SourceBaseResumeId",
                        column: x => x.SourceBaseResumeId,
                        principalTable: "BaseResumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TailoredResumes_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TailoringSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    TailoredResumeId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceBaseResumeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewState = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetSection = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OriginalContentJson = table.Column<string>(type: "jsonb", nullable: true),
                    SuggestedContentJson = table.Column<string>(type: "jsonb", nullable: false),
                    AcceptedContentJson = table.Column<string>(type: "jsonb", nullable: true),
                    Rationale = table.Column<string>(type: "text", nullable: true),
                    AiNotes = table.Column<string>(type: "text", nullable: true),
                    SourceEvidenceJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TailoringSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TailoringSuggestions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TailoringSuggestions_BaseResumes_SourceBaseResumeId",
                        column: x => x.SourceBaseResumeId,
                        principalTable: "BaseResumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TailoringSuggestions_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TailoringSuggestions_TailoredResumes_TailoredResumeId",
                        column: x => x.TailoredResumeId,
                        principalTable: "TailoredResumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaseResumes_UserId",
                table: "BaseResumes",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_SelectedBaseResumeId",
                table: "Jobs",
                column: "SelectedBaseResumeId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_SelectedTailoredResumeId",
                table: "Jobs",
                column: "SelectedTailoredResumeId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_UserId_Status",
                table: "Jobs",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_UserId_UpdatedAt",
                table: "Jobs",
                columns: new[] { "UserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TailoredResumes_JobId",
                table: "TailoredResumes",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_TailoredResumes_SourceBaseResumeId",
                table: "TailoredResumes",
                column: "SourceBaseResumeId");

            migrationBuilder.CreateIndex(
                name: "IX_TailoredResumes_UserId_JobId",
                table: "TailoredResumes",
                columns: new[] { "UserId", "JobId" });

            migrationBuilder.CreateIndex(
                name: "IX_TailoredResumes_UserId_JobId_VersionNumber",
                table: "TailoredResumes",
                columns: new[] { "UserId", "JobId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TailoringSuggestions_JobId",
                table: "TailoringSuggestions",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_TailoringSuggestions_SourceBaseResumeId",
                table: "TailoringSuggestions",
                column: "SourceBaseResumeId");

            migrationBuilder.CreateIndex(
                name: "IX_TailoringSuggestions_TailoredResumeId",
                table: "TailoringSuggestions",
                column: "TailoredResumeId");

            migrationBuilder.CreateIndex(
                name: "IX_TailoringSuggestions_UserId_JobId_ReviewState",
                table: "TailoringSuggestions",
                columns: new[] { "UserId", "JobId", "ReviewState" });

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_TailoredResumes_SelectedTailoredResumeId",
                table: "Jobs",
                column: "SelectedTailoredResumeId",
                principalTable: "TailoredResumes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_BaseResumes_SelectedBaseResumeId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_TailoredResumes_BaseResumes_SourceBaseResumeId",
                table: "TailoredResumes");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_TailoredResumes_SelectedTailoredResumeId",
                table: "Jobs");

            migrationBuilder.DropTable(
                name: "TailoringSuggestions");

            migrationBuilder.DropTable(
                name: "BaseResumes");

            migrationBuilder.DropTable(
                name: "TailoredResumes");

            migrationBuilder.DropTable(
                name: "Jobs");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resumaire.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSuggestionTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Operation",
                table: "TailoringSuggestions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SourceBaseResumeRevision",
                table: "TailoringSuggestions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetPath",
                table: "TailoringSuggestions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Operation",
                table: "TailoringSuggestions");

            migrationBuilder.DropColumn(
                name: "SourceBaseResumeRevision",
                table: "TailoringSuggestions");

            migrationBuilder.DropColumn(
                name: "TargetPath",
                table: "TailoringSuggestions");
        }
    }
}

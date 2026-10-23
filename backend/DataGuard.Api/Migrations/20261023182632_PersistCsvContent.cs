using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataGuard.Api.Migrations
{
    /// <inheritdoc />
    public partial class PersistCsvContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CsvContent",
                table: "Datasets",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CsvContent",
                table: "Datasets");
        }
    }
}

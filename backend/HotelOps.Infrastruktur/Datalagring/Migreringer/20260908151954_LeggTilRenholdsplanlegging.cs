using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOps.Infrastruktur.Datalagring.Migreringer
{
    /// <inheritdoc />
    public partial class LeggTilRenholdsplanlegging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnsvarligRenholder",
                table: "Rom",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Renholdsprioritet",
                table: "Rom",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Normal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnsvarligRenholder",
                table: "Rom");

            migrationBuilder.DropColumn(
                name: "Renholdsprioritet",
                table: "Rom");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOps.Infrastruktur.Datalagring.Migreringer
{
    /// <inheritdoc />
    public partial class OpprettRom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rom",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nummer = table.Column<string>(type: "text", nullable: false),
                    Etasje = table.Column<int>(type: "integer", nullable: false),
                    Beleggsstatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Rengjøringsstatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Driftsstatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rom", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rom_Nummer",
                table: "Rom",
                column: "Nummer",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rom");
        }
    }
}

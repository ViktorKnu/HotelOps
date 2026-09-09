using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOps.Infrastruktur.Datalagring.Migreringer
{
    /// <inheritdoc />
    public partial class LeggTilRomversjon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Versjon",
                table: "Rom",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Versjon",
                table: "Rom");
        }
    }
}

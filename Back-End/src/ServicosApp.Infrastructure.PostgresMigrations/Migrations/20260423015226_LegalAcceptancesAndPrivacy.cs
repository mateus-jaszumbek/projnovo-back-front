using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicosApp.Infrastructure.PostgresMigrations.Migrations
{
    /// <inheritdoc />
    public partial class LegalAcceptancesAndPrivacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PoliticaPrivacidadeAceitaEmUtc",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoliticaPrivacidadeVersaoAceita",
                table: "usuarios",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermosUsoAceitoEmUtc",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermosUsoVersaoAceita",
                table: "usuarios",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PoliticaPrivacidadeAceitaEmUtc",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "PoliticaPrivacidadeVersaoAceita",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "TermosUsoAceitoEmUtc",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "TermosUsoVersaoAceita",
                table: "usuarios");
        }
    }
}

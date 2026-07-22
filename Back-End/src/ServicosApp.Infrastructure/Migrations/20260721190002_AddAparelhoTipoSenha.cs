using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicosApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAparelhoTipoSenha : Migration 
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PadraoDesenho",
                table: "aparelhos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoSenha",
                table: "aparelhos",
                type: "TEXT",
                nullable: false,
                defaultValue: "NENHUMA");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PadraoDesenho",
                table: "aparelhos");

            migrationBuilder.DropColumn(
                name: "TipoSenha",
                table: "aparelhos");
        }
    }
}

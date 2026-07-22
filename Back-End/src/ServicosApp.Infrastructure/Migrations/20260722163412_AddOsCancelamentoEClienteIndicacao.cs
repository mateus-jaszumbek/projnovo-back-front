using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicosApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOsCancelamentoEClienteIndicacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MotivoCancelamento",
                table: "ordens_servico",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusAntesCancelamento",
                table: "ordens_servico",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IndicadoPorTerceiro",
                table: "clientes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NomeIndicacao",
                table: "clientes",
                type: "TEXT",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MotivoCancelamento",
                table: "ordens_servico");

            migrationBuilder.DropColumn(
                name: "StatusAntesCancelamento",
                table: "ordens_servico");

            migrationBuilder.DropColumn(
                name: "IndicadoPorTerceiro",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "NomeIndicacao",
                table: "clientes");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicosApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoItemVendaItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "PecaId",
                table: "venda_itens",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "ServicoCatalogoId",
                table: "venda_itens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoItem",
                table: "venda_itens",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "PECA");

            migrationBuilder.CreateIndex(
                name: "IX_venda_itens_ServicoCatalogoId",
                table: "venda_itens",
                column: "ServicoCatalogoId");

            migrationBuilder.AddForeignKey(
                name: "FK_venda_itens_servicos_catalogo_ServicoCatalogoId",
                table: "venda_itens",
                column: "ServicoCatalogoId",
                principalTable: "servicos_catalogo",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_venda_itens_servicos_catalogo_ServicoCatalogoId",
                table: "venda_itens");

            migrationBuilder.DropIndex(
                name: "IX_venda_itens_ServicoCatalogoId",
                table: "venda_itens");

            migrationBuilder.DropColumn(
                name: "ServicoCatalogoId",
                table: "venda_itens");

            migrationBuilder.DropColumn(
                name: "TipoItem",
                table: "venda_itens");

            migrationBuilder.AlterColumn<Guid>(
                name: "PecaId",
                table: "venda_itens",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}

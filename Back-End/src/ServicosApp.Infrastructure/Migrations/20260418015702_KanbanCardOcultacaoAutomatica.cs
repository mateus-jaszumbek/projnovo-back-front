using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicosApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class KanbanCardOcultacaoAutomatica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataEntradaColunaAtual",
                table: "KanbanCard",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataOcultado",
                table: "KanbanCard",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OcultoDoQuadro",
                table: "KanbanCard",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_KanbanCard_EmpresaId_KanbanColunaId_OcultoDoQuadro",
                table: "KanbanCard",
                columns: new[] { "EmpresaId", "KanbanColunaId", "OcultoDoQuadro" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KanbanCard_EmpresaId_KanbanColunaId_OcultoDoQuadro",
                table: "KanbanCard");

            migrationBuilder.DropColumn(
                name: "DataEntradaColunaAtual",
                table: "KanbanCard");

            migrationBuilder.DropColumn(
                name: "DataOcultado",
                table: "KanbanCard");

            migrationBuilder.DropColumn(
                name: "OcultoDoQuadro",
                table: "KanbanCard");
        }
    }
}

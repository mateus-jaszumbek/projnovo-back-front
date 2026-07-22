using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicosApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdemServicoReabertura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrigemReaberturaId",
                table: "ordens_servico",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ordens_servico_OrigemReaberturaId",
                table: "ordens_servico",
                column: "OrigemReaberturaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ordens_servico_ordens_servico_OrigemReaberturaId",
                table: "ordens_servico",
                column: "OrigemReaberturaId",
                principalTable: "ordens_servico",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ordens_servico_ordens_servico_OrigemReaberturaId",
                table: "ordens_servico");

            migrationBuilder.DropIndex(
                name: "IX_ordens_servico_OrigemReaberturaId",
                table: "ordens_servico");

            migrationBuilder.DropColumn(
                name: "OrigemReaberturaId",
                table: "ordens_servico");
        }
    }
}

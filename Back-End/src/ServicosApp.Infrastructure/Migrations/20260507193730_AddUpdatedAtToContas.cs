using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicosApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedAtToContas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "contas_receber",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "contas_pagar",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "contas_receber");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "contas_pagar");
        }
    }
}

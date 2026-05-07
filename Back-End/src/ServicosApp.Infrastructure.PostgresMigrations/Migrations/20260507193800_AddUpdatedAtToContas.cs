using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicosApp.Infrastructure.PostgresMigrations.Migrations
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
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "contas_pagar",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
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

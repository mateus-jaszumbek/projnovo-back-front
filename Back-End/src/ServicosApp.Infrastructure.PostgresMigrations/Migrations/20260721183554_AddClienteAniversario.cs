using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicosApp.Infrastructure.PostgresMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteAniversario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DataAniversario",
                table: "clientes",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataAniversario",
                table: "clientes");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicosApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientePortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EhLojista",
                table: "clientes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PortalAtivo",
                table: "clientes",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "PortalToken",
                table: "clientes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                "UPDATE clientes SET PortalToken = lower(hex(randomblob(16))) WHERE PortalToken = '' OR PortalToken IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_clientes_PortalToken",
                table: "clientes",
                column: "PortalToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_clientes_PortalToken",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "EhLojista",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "PortalAtivo",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "PortalToken",
                table: "clientes");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicosApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriaPeca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoriaPecaId",
                table: "pecas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "categorias_peca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias_peca", x => x.Id);
                    table.ForeignKey(
                        name: "FK_categorias_peca_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pecas_CategoriaPecaId",
                table: "pecas",
                column: "CategoriaPecaId");

            migrationBuilder.CreateIndex(
                name: "IX_categorias_peca_EmpresaId_Nome",
                table: "categorias_peca",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_pecas_categorias_peca_CategoriaPecaId",
                table: "pecas",
                column: "CategoriaPecaId",
                principalTable: "categorias_peca",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Converte automaticamente os valores de texto livre já usados em Peca.Categoria
            // em categorias cadastradas de verdade, e vincula as peças existentes a elas.
            migrationBuilder.Sql(@"
INSERT INTO categorias_peca (""Id"", ""EmpresaId"", ""Nome"", ""Ativo"", ""CreatedAt"", ""UpdatedAt"")
SELECT
    lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-4' || substr(hex(randomblob(2)),2) || '-' ||
        substr('89ab',abs(random()) % 4 + 1,1) || substr(hex(randomblob(2)),2) || '-' || hex(randomblob(6))),
    t.""EmpresaId"", t.""Categoria"", 1, datetime('now'), datetime('now')
FROM (
    SELECT DISTINCT ""EmpresaId"" AS ""EmpresaId"", trim(""Categoria"") AS ""Categoria""
    FROM pecas
    WHERE ""Categoria"" IS NOT NULL AND trim(""Categoria"") <> ''
) t;

UPDATE pecas
SET ""CategoriaPecaId"" = (
    SELECT c.""Id"" FROM categorias_peca c
    WHERE c.""EmpresaId"" = pecas.""EmpresaId"" AND c.""Nome"" = trim(pecas.""Categoria"")
)
WHERE ""Categoria"" IS NOT NULL AND trim(""Categoria"") <> '';
");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "pecas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pecas_categorias_peca_CategoriaPecaId",
                table: "pecas");

            migrationBuilder.DropTable(
                name: "categorias_peca");

            migrationBuilder.DropIndex(
                name: "IX_pecas_CategoriaPecaId",
                table: "pecas");

            migrationBuilder.DropColumn(
                name: "CategoriaPecaId",
                table: "pecas");

            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "pecas",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }
    }
}

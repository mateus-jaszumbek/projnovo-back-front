using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicosApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPagamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cobrancas_pagamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Canal = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OrigemTipo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    OrigemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    QrCodeBase64 = table.Column<string>(type: "text", nullable: true),
                    QrCodePayload = table.Column<string>(type: "text", nullable: true),
                    MensagemErro = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PagoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cobrancas_pagamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cobrancas_pagamento_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "configuracoes_pagamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AccessTokenEncrypted = table.Column<string>(type: "text", nullable: true),
                    PublicKey = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    PosId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    UserIdExterno = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    WebhookSecret = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SuportaMaquininha = table.Column<bool>(type: "INTEGER", nullable: false),
                    SuportaPix = table.Column<bool>(type: "INTEGER", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracoes_pagamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_configuracoes_pagamento_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cobrancas_pagamento_EmpresaId",
                table: "cobrancas_pagamento",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_cobrancas_pagamento_EmpresaId_ExternalId",
                table: "cobrancas_pagamento",
                columns: new[] { "EmpresaId", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_cobrancas_pagamento_EmpresaId_OrigemTipo_OrigemId",
                table: "cobrancas_pagamento",
                columns: new[] { "EmpresaId", "OrigemTipo", "OrigemId" });

            migrationBuilder.CreateIndex(
                name: "IX_configuracoes_pagamento_EmpresaId",
                table: "configuracoes_pagamento",
                column: "EmpresaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_configuracoes_pagamento_Provider_WebhookSecret",
                table: "configuracoes_pagamento",
                columns: new[] { "Provider", "WebhookSecret" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cobrancas_pagamento");

            migrationBuilder.DropTable(
                name: "configuracoes_pagamento");
        }
    }
}

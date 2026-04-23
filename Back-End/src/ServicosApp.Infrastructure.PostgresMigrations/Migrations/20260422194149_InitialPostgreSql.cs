using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicosApp.Infrastructure.PostgresMigrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "empresas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RazaoSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NomeFantasia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cnpj = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: false),
                    InscricaoEstadual = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    InscricaoMunicipal = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    RegimeTributario = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Cep = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SenhaHash = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    IsSuperAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TipoPessoa = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CpfCnpj = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Cep = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_clientes_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "configuracoes_fiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ambiente = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RegimeTributario = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SerieNfce = table.Column<int>(type: "integer", nullable: false),
                    SerieNfe = table.Column<int>(type: "integer", nullable: false),
                    SerieNfse = table.Column<int>(type: "integer", nullable: false),
                    ProximoNumeroNfce = table.Column<long>(type: "bigint", nullable: false),
                    ProximoNumeroNfe = table.Column<long>(type: "bigint", nullable: false),
                    ProximoNumeroNfse = table.Column<long>(type: "bigint", nullable: false),
                    ProvedorFiscal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MunicipioCodigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CnaePrincipal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ItemListaServico = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    NaturezaOperacaoPadrao = table.Column<string>(type: "text", nullable: true),
                    IssRetidoPadrao = table.Column<bool>(type: "boolean", nullable: false),
                    AliquotaIssPadrao = table.Column<decimal>(type: "numeric", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracoes_fiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_configuracoes_fiscais_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "credenciais_fiscais_empresas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoDocumentoFiscal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Provedor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UrlBase = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClientId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClientSecretEncrypted = table.Column<string>(type: "text", nullable: true),
                    UsuarioApi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SenhaApiEncrypted = table.Column<string>(type: "text", nullable: true),
                    CertificadoBase64Encrypted = table.Column<string>(type: "text", nullable: true),
                    CertificadoSenhaEncrypted = table.Column<string>(type: "text", nullable: true),
                    TokenAcesso = table.Column<string>(type: "text", nullable: true),
                    TokenExpiraEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credenciais_fiscais_empresas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_credenciais_fiscais_empresas_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fornecedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TipoPessoa = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CpfCnpj = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Contato = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    WhatsApp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProdutosFornecidos = table.Column<string>(type: "text", nullable: true),
                    MensagemPadrao = table.Column<string>(type: "text", nullable: true),
                    Cep = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fornecedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fornecedores_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KanbanFluxo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanbanFluxo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KanbanFluxo_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "modulos_personalizados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Chave = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modulos_personalizados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_modulos_personalizados_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "regras_fiscais_produtos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoDocumentoFiscal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UfOrigem = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    UfDestino = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    RegimeTributario = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Ncm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Cfop = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CstCsosn = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Cest = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OrigemMercadoria = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    AliquotaIcms = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AliquotaPis = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AliquotaCofins = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regras_fiscais_produtos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_regras_fiscais_produtos_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "servicos_catalogo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ValorPadrao = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CodigoInterno = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TempoEstimadoMinutos = table.Column<int>(type: "integer", nullable: true),
                    GarantiaDias = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servicos_catalogo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_servicos_catalogo_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tecnicos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Especialidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tecnicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tecnicos_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "caixas_diarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataCaixa = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorAbertura = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorFechamentoSistema = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorFechamentoInformado = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Diferenca = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AbertoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    FechadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DataAbertura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataFechamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_caixas_diarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_caixas_diarios_usuarios_AbertoPor",
                        column: x => x.AbertoPor,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_caixas_diarios_usuarios_FechadoPor",
                        column: x => x.FechadoPor,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documentos_fiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoDocumento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OrigemTipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OrigemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Numero = table.Column<long>(type: "bigint", nullable: false),
                    Serie = table.Column<int>(type: "integer", nullable: false),
                    SerieRps = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    NumeroRps = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Ambiente = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClienteNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ClienteCpfCnpj = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ClienteEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClienteTelefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ClienteCep = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ClienteLogradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClienteNumero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ClienteComplemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClienteBairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClienteCidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClienteUf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    ClienteMunicipioCodigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DataEmissao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataCompetencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataAutorizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataCancelamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChaveAcesso = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Protocolo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CodigoVerificacao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LinkConsulta = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    NumeroExterno = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Lote = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ValorServicos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorProdutos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Desconto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    XmlConteudo = table.Column<string>(type: "text", nullable: true),
                    XmlUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PdfUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CodigoRejeicao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    MensagemRejeicao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PayloadEnvio = table.Column<string>(type: "text", nullable: true),
                    PayloadRetorno = table.Column<string>(type: "text", nullable: true),
                    MotivoCancelamento = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documentos_fiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_documentos_fiscais_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_documentos_fiscais_usuarios_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuario_empresas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Perfil = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NivelAcesso = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_empresas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_usuario_empresas_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_usuario_empresas_usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "aparelhos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Marca = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Modelo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Cor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Imei = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    SenhaAparelho = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Acessorios = table.Column<string>(type: "text", nullable: true),
                    EstadoFisico = table.Column<string>(type: "text", nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aparelhos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_aparelhos_clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_aparelhos_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contas_receber",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrigemTipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    OrigemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DataEmissao = table.Column<DateOnly>(type: "date", nullable: false),
                    DataVencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorRecebido = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FormaPagamento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contas_receber", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contas_receber_clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroVenda = table.Column<long>(type: "bigint", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FormaPagamento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Desconto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DataVenda = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vendas_clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendas_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vendas_usuarios_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contas_pagar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Fornecedor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DataEmissao = table.Column<DateOnly>(type: "date", nullable: false),
                    DataVencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorPago = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contas_pagar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contas_pagar_fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "pecas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CodigoInterno = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Sku = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Marca = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModeloCompativel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Ncm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Cest = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CfopPadraoNfe = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CfopPadraoNfce = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CstCsosn = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    OrigemMercadoria = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Unidade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustoUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PrecoVenda = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GarantiaDias = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    EstoqueAtual = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EstoqueMinimo = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pecas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pecas_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pecas_fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "KanbanColuna",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KanbanFluxoId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeInterno = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NomePublico = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Cor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Sistema = table.Column<bool>(type: "boolean", nullable: false),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false),
                    VisivelCliente = table.Column<bool>(type: "boolean", nullable: false),
                    GeraEventoCliente = table.Column<bool>(type: "boolean", nullable: false),
                    EtapaFinal = table.Column<bool>(type: "boolean", nullable: false),
                    TipoFinalizacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PermiteEnvioWhatsApp = table.Column<bool>(type: "boolean", nullable: false),
                    DescricaoPublica = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanbanColuna", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KanbanColuna_KanbanFluxo_KanbanFluxoId",
                        column: x => x.KanbanFluxoId,
                        principalTable: "KanbanFluxo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KanbanColuna_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "campos_modulo_layout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuloPersonalizadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampoChave = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Aba = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "Principal"),
                    Linha = table.Column<int>(type: "integer", nullable: false),
                    Posicao = table.Column<int>(type: "integer", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campos_modulo_layout", x => x.Id);
                    table.ForeignKey(
                        name: "FK_campos_modulo_layout_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_campos_modulo_layout_modulos_personalizados_ModuloPersonali~",
                        column: x => x.ModuloPersonalizadoId,
                        principalTable: "modulos_personalizados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "campos_personalizados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuloPersonalizadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Chave = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Obrigatorio = table.Column<bool>(type: "boolean", nullable: false),
                    Aba = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "Principal"),
                    Linha = table.Column<int>(type: "integer", nullable: false),
                    Posicao = table.Column<int>(type: "integer", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Placeholder = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ValorPadrao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    OpcoesJson = table.Column<string>(type: "text", nullable: true),
                    ExportarExcel = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ExportarExcelResumo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ExportarPdf = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campos_personalizados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_campos_personalizados_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_campos_personalizados_modulos_personalizados_ModuloPersonal~",
                        column: x => x.ModuloPersonalizadoId,
                        principalTable: "modulos_personalizados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "registros_personalizados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuloPersonalizadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrigemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValoresJson = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registros_personalizados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registros_personalizados_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_registros_personalizados_modulos_personalizados_ModuloPerso~",
                        column: x => x.ModuloPersonalizadoId,
                        principalTable: "modulos_personalizados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "caixa_lancamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaixaDiarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OrigemTipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    OrigemId = table.Column<Guid>(type: "uuid", nullable: true),
                    FormaPagamento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_caixa_lancamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_caixa_lancamentos_caixas_diarios_CaixaDiarioId",
                        column: x => x.CaixaDiarioId,
                        principalTable: "caixas_diarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_caixa_lancamentos_usuarios_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "eventos_fiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentoFiscalId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoEvento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Protocolo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Mensagem = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PayloadEnvio = table.Column<string>(type: "text", nullable: true),
                    PayloadRetorno = table.Column<string>(type: "text", nullable: true),
                    DataEvento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_fiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_eventos_fiscais_documentos_fiscais_DocumentoFiscalId",
                        column: x => x.DocumentoFiscalId,
                        principalTable: "documentos_fiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_eventos_fiscais_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_eventos_fiscais_usuarios_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "integracoes_fiscais_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentoFiscalId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoOperacao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Tentativas = table.Column<int>(type: "integer", nullable: false),
                    RequestPayload = table.Column<string>(type: "text", nullable: true),
                    ResponsePayload = table.Column<string>(type: "text", nullable: true),
                    MensagemErro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProcessadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integracoes_fiscais_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_integracoes_fiscais_jobs_documentos_fiscais_DocumentoFiscal~",
                        column: x => x.DocumentoFiscalId,
                        principalTable: "documentos_fiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_integracoes_fiscais_jobs_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ordens_servico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroOs = table.Column<long>(type: "bigint", nullable: false),
                    DocumentoFiscalId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    AparelhoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TecnicoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DefeitoRelatado = table.Column<string>(type: "text", nullable: false),
                    Diagnostico = table.Column<string>(type: "text", nullable: true),
                    LaudoTecnico = table.Column<string>(type: "text", nullable: true),
                    ValorMaoObra = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    ValorPecas = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Desconto = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    DataEntrada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataPrevisao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataAprovacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataConclusao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataEntrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GarantiaDias = table.Column<int>(type: "integer", nullable: false),
                    ObservacoesInternas = table.Column<string>(type: "text", nullable: true),
                    ObservacoesCliente = table.Column<string>(type: "text", nullable: true),
                    FotosJson = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    KanbanColunaAtualId = table.Column<Guid>(type: "uuid", nullable: true),
                    TrackingToken = table.Column<string>(type: "text", nullable: false),
                    TrackingPublicoAtivo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ordens_servico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ordens_servico_aparelhos_AparelhoId",
                        column: x => x.AparelhoId,
                        principalTable: "aparelhos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ordens_servico_clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ordens_servico_documentos_fiscais_DocumentoFiscalId",
                        column: x => x.DocumentoFiscalId,
                        principalTable: "documentos_fiscais",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ordens_servico_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ordens_servico_tecnicos_TecnicoId",
                        column: x => x.TecnicoId,
                        principalTable: "tecnicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ordens_servico_usuarios_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ordens_servico_usuarios_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documentos_fiscais_itens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentoFiscalId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoItem = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ServicoCatalogoId = table.Column<Guid>(type: "uuid", nullable: true),
                    PecaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Desconto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Ncm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Cnae = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ItemListaServico = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Cfop = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Cest = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CstCsosn = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    OrigemMercadoria = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    BaseIss = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    AliquotaIss = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    ValorIss = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    IssRetido = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    BaseIcms = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    AliquotaIcms = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    ValorIcms = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    BasePis = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    AliquotaPis = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    ValorPis = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    BaseCofins = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    AliquotaCofins = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    ValorCofins = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documentos_fiscais_itens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_documentos_fiscais_itens_documentos_fiscais_DocumentoFiscal~",
                        column: x => x.DocumentoFiscalId,
                        principalTable: "documentos_fiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_documentos_fiscais_itens_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_documentos_fiscais_itens_pecas_PecaId",
                        column: x => x.PecaId,
                        principalTable: "pecas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_documentos_fiscais_itens_servicos_catalogo_ServicoCatalogoId",
                        column: x => x.ServicoCatalogoId,
                        principalTable: "servicos_catalogo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "estoque_movimentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PecaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoMovimento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    CustoUnitario = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    OrigemTipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    OrigemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Observacao = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DataMovimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estoque_movimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_estoque_movimentos_pecas_PecaId",
                        column: x => x.PecaId,
                        principalTable: "pecas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_estoque_movimentos_usuarios_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fornecedores_mensagens_historico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uuid", nullable: false),
                    PecaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Canal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Assunto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Mensagem = table.Column<string>(type: "text", nullable: false),
                    QuantidadeSolicitada = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    EnviadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fornecedores_mensagens_historico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fornecedores_mensagens_historico_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fornecedores_mensagens_historico_fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fornecedores_mensagens_historico_pecas_PecaId",
                        column: x => x.PecaId,
                        principalTable: "pecas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "venda_itens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PecaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CustoUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Desconto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_venda_itens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_venda_itens_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_venda_itens_pecas_PecaId",
                        column: x => x.PecaId,
                        principalTable: "pecas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_venda_itens_vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KanbanCard",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KanbanColunaId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicTrackingToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PublicTrackingAtivo = table.Column<bool>(type: "boolean", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    DataEntradaColunaAtual = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OcultoDoQuadro = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DataOcultado = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanbanCard", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KanbanCard_KanbanColuna_KanbanColunaId",
                        column: x => x.KanbanColunaId,
                        principalTable: "KanbanColuna",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KanbanCard_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KanbanCard_ordens_servico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "ordens_servico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KanbanTarefaPrivada",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    KanbanColunaId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Titulo = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanbanTarefaPrivada", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KanbanTarefaPrivada_KanbanColuna_KanbanColunaId",
                        column: x => x.KanbanColunaId,
                        principalTable: "KanbanColuna",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KanbanTarefaPrivada_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KanbanTarefaPrivada_ordens_servico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "ordens_servico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OrdemServicoKanbanHistorico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ColunaOrigemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ColunaDestinoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeColunaOrigem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NomeColunaDestino = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    HistoricoPublico = table.Column<bool>(type: "boolean", nullable: false),
                    TituloPublico = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DescricaoPublica = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PublicTrackingToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DataMovimentacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdemServicoKanbanHistorico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdemServicoKanbanHistorico_KanbanColuna_ColunaDestinoId",
                        column: x => x.ColunaDestinoId,
                        principalTable: "KanbanColuna",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrdemServicoKanbanHistorico_KanbanColuna_ColunaOrigemId",
                        column: x => x.ColunaOrigemId,
                        principalTable: "KanbanColuna",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrdemServicoKanbanHistorico_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrdemServicoKanbanHistorico_ordens_servico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "ordens_servico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ordens_servico_itens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoItem = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ServicoCatalogoId = table.Column<Guid>(type: "uuid", nullable: true),
                    PecaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    CustoUnitario = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Desconto = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    GarantiaDias = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ordens_servico_itens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ordens_servico_itens_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ordens_servico_itens_ordens_servico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "ordens_servico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ordens_servico_itens_pecas_PecaId",
                        column: x => x.PecaId,
                        principalTable: "pecas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ordens_servico_itens_servicos_catalogo_ServicoCatalogoId",
                        column: x => x.ServicoCatalogoId,
                        principalTable: "servicos_catalogo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aparelhos_ClienteId",
                table: "aparelhos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_aparelhos_EmpresaId",
                table: "aparelhos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_aparelhos_EmpresaId_Imei",
                table: "aparelhos",
                columns: new[] { "EmpresaId", "Imei" });

            migrationBuilder.CreateIndex(
                name: "IX_aparelhos_EmpresaId_SerialNumber",
                table: "aparelhos",
                columns: new[] { "EmpresaId", "SerialNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_caixa_lancamentos_CaixaDiarioId",
                table: "caixa_lancamentos",
                column: "CaixaDiarioId");

            migrationBuilder.CreateIndex(
                name: "IX_caixa_lancamentos_CreatedBy",
                table: "caixa_lancamentos",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_caixa_lancamentos_EmpresaId",
                table: "caixa_lancamentos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_caixas_diarios_AbertoPor",
                table: "caixas_diarios",
                column: "AbertoPor");

            migrationBuilder.CreateIndex(
                name: "IX_caixas_diarios_EmpresaId_DataCaixa",
                table: "caixas_diarios",
                columns: new[] { "EmpresaId", "DataCaixa" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_caixas_diarios_FechadoPor",
                table: "caixas_diarios",
                column: "FechadoPor");

            migrationBuilder.CreateIndex(
                name: "IX_campos_modulo_layout_EmpresaId",
                table: "campos_modulo_layout",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_campos_modulo_layout_EmpresaId_ModuloPersonalizadoId_CampoC~",
                table: "campos_modulo_layout",
                columns: new[] { "EmpresaId", "ModuloPersonalizadoId", "CampoChave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_campos_modulo_layout_ModuloPersonalizadoId",
                table: "campos_modulo_layout",
                column: "ModuloPersonalizadoId");

            migrationBuilder.CreateIndex(
                name: "IX_campos_personalizados_EmpresaId",
                table: "campos_personalizados",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_campos_personalizados_EmpresaId_ModuloPersonalizadoId_Chave",
                table: "campos_personalizados",
                columns: new[] { "EmpresaId", "ModuloPersonalizadoId", "Chave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_campos_personalizados_ModuloPersonalizadoId",
                table: "campos_personalizados",
                column: "ModuloPersonalizadoId");

            migrationBuilder.CreateIndex(
                name: "IX_campos_personalizados_ModuloPersonalizadoId_Aba_Linha_Posic~",
                table: "campos_personalizados",
                columns: new[] { "ModuloPersonalizadoId", "Aba", "Linha", "Posicao" });

            migrationBuilder.CreateIndex(
                name: "IX_clientes_EmpresaId",
                table: "clientes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_clientes_EmpresaId_CpfCnpj",
                table: "clientes",
                columns: new[] { "EmpresaId", "CpfCnpj" });

            migrationBuilder.CreateIndex(
                name: "IX_clientes_EmpresaId_Nome",
                table: "clientes",
                columns: new[] { "EmpresaId", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_configuracoes_fiscais_EmpresaId",
                table: "configuracoes_fiscais",
                column: "EmpresaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contas_pagar_DataVencimento",
                table: "contas_pagar",
                column: "DataVencimento");

            migrationBuilder.CreateIndex(
                name: "IX_contas_pagar_EmpresaId",
                table: "contas_pagar",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_contas_pagar_FornecedorId",
                table: "contas_pagar",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_contas_receber_ClienteId",
                table: "contas_receber",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_contas_receber_DataVencimento",
                table: "contas_receber",
                column: "DataVencimento");

            migrationBuilder.CreateIndex(
                name: "IX_contas_receber_EmpresaId",
                table: "contas_receber",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_credenciais_fiscais_empresas_EmpresaId",
                table: "credenciais_fiscais_empresas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_credenciais_fiscais_empresas_EmpresaId_TipoDocumentoFiscal_~",
                table: "credenciais_fiscais_empresas",
                columns: new[] { "EmpresaId", "TipoDocumentoFiscal", "Provedor" });

            migrationBuilder.CreateIndex(
                name: "IX_documentos_fiscais_ClienteId",
                table: "documentos_fiscais",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_fiscais_CreatedBy",
                table: "documentos_fiscais",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_fiscais_EmpresaId",
                table: "documentos_fiscais",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_fiscais_EmpresaId_Status",
                table: "documentos_fiscais",
                columns: new[] { "EmpresaId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_documentos_fiscais_EmpresaId_TipoDocumento_Numero_Serie",
                table: "documentos_fiscais",
                columns: new[] { "EmpresaId", "TipoDocumento", "Numero", "Serie" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documentos_fiscais_EmpresaId_TipoDocumento_OrigemTipo_Orige~",
                table: "documentos_fiscais",
                columns: new[] { "EmpresaId", "TipoDocumento", "OrigemTipo", "OrigemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documentos_fiscais_itens_DocumentoFiscalId",
                table: "documentos_fiscais_itens",
                column: "DocumentoFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_fiscais_itens_EmpresaId",
                table: "documentos_fiscais_itens",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_fiscais_itens_PecaId",
                table: "documentos_fiscais_itens",
                column: "PecaId");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_fiscais_itens_ServicoCatalogoId",
                table: "documentos_fiscais_itens",
                column: "ServicoCatalogoId");

            migrationBuilder.CreateIndex(
                name: "IX_empresas_Cnpj",
                table: "empresas",
                column: "Cnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_estoque_movimentos_CreatedBy",
                table: "estoque_movimentos",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_estoque_movimentos_EmpresaId",
                table: "estoque_movimentos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_estoque_movimentos_EmpresaId_OrigemTipo_OrigemId",
                table: "estoque_movimentos",
                columns: new[] { "EmpresaId", "OrigemTipo", "OrigemId" });

            migrationBuilder.CreateIndex(
                name: "IX_estoque_movimentos_PecaId",
                table: "estoque_movimentos",
                column: "PecaId");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_fiscais_CreatedBy",
                table: "eventos_fiscais",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_fiscais_DocumentoFiscalId",
                table: "eventos_fiscais",
                column: "DocumentoFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_fiscais_EmpresaId",
                table: "eventos_fiscais",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_fiscais_EmpresaId_TipoEvento_Status",
                table: "eventos_fiscais",
                columns: new[] { "EmpresaId", "TipoEvento", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_fornecedores_EmpresaId",
                table: "fornecedores",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_fornecedores_EmpresaId_CpfCnpj",
                table: "fornecedores",
                columns: new[] { "EmpresaId", "CpfCnpj" });

            migrationBuilder.CreateIndex(
                name: "IX_fornecedores_EmpresaId_Nome",
                table: "fornecedores",
                columns: new[] { "EmpresaId", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_fornecedores_mensagens_historico_EmpresaId",
                table: "fornecedores_mensagens_historico",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_fornecedores_mensagens_historico_EmpresaId_EnviadoEm",
                table: "fornecedores_mensagens_historico",
                columns: new[] { "EmpresaId", "EnviadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_fornecedores_mensagens_historico_FornecedorId",
                table: "fornecedores_mensagens_historico",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_fornecedores_mensagens_historico_PecaId",
                table: "fornecedores_mensagens_historico",
                column: "PecaId");

            migrationBuilder.CreateIndex(
                name: "IX_integracoes_fiscais_jobs_DocumentoFiscalId",
                table: "integracoes_fiscais_jobs",
                column: "DocumentoFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_integracoes_fiscais_jobs_EmpresaId",
                table: "integracoes_fiscais_jobs",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_integracoes_fiscais_jobs_EmpresaId_Status_TipoOperacao",
                table: "integracoes_fiscais_jobs",
                columns: new[] { "EmpresaId", "Status", "TipoOperacao" });

            migrationBuilder.CreateIndex(
                name: "IX_KanbanCard_EmpresaId_KanbanColunaId_OcultoDoQuadro",
                table: "KanbanCard",
                columns: new[] { "EmpresaId", "KanbanColunaId", "OcultoDoQuadro" });

            migrationBuilder.CreateIndex(
                name: "IX_KanbanCard_EmpresaId_OrdemServicoId",
                table: "KanbanCard",
                columns: new[] { "EmpresaId", "OrdemServicoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KanbanCard_KanbanColunaId",
                table: "KanbanCard",
                column: "KanbanColunaId");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanCard_OrdemServicoId",
                table: "KanbanCard",
                column: "OrdemServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanCard_PublicTrackingToken",
                table: "KanbanCard",
                column: "PublicTrackingToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KanbanColuna_EmpresaId_KanbanFluxoId_Ordem",
                table: "KanbanColuna",
                columns: new[] { "EmpresaId", "KanbanFluxoId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_KanbanColuna_KanbanFluxoId",
                table: "KanbanColuna",
                column: "KanbanFluxoId");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanFluxo_EmpresaId_Tipo_UsuarioId",
                table: "KanbanFluxo",
                columns: new[] { "EmpresaId", "Tipo", "UsuarioId" });

            migrationBuilder.CreateIndex(
                name: "IX_KanbanTarefaPrivada_EmpresaId_UsuarioId_KanbanColunaId_Ordem",
                table: "KanbanTarefaPrivada",
                columns: new[] { "EmpresaId", "UsuarioId", "KanbanColunaId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_KanbanTarefaPrivada_KanbanColunaId",
                table: "KanbanTarefaPrivada",
                column: "KanbanColunaId");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanTarefaPrivada_OrdemServicoId",
                table: "KanbanTarefaPrivada",
                column: "OrdemServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_modulos_personalizados_EmpresaId",
                table: "modulos_personalizados",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_modulos_personalizados_EmpresaId_Chave",
                table: "modulos_personalizados",
                columns: new[] { "EmpresaId", "Chave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdemServicoKanbanHistorico_ColunaDestinoId",
                table: "OrdemServicoKanbanHistorico",
                column: "ColunaDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdemServicoKanbanHistorico_ColunaOrigemId",
                table: "OrdemServicoKanbanHistorico",
                column: "ColunaOrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdemServicoKanbanHistorico_EmpresaId_OrdemServicoId_DataMo~",
                table: "OrdemServicoKanbanHistorico",
                columns: new[] { "EmpresaId", "OrdemServicoId", "DataMovimentacao" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdemServicoKanbanHistorico_OrdemServicoId",
                table: "OrdemServicoKanbanHistorico",
                column: "OrdemServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_ordens_servico_AparelhoId",
                table: "ordens_servico",
                column: "AparelhoId");

            migrationBuilder.CreateIndex(
                name: "IX_ordens_servico_ClienteId",
                table: "ordens_servico",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ordens_servico_CreatedBy",
                table: "ordens_servico",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ordens_servico_DocumentoFiscalId",
                table: "ordens_servico",
                column: "DocumentoFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_ordens_servico_EmpresaId",
                table: "ordens_servico",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ordens_servico_EmpresaId_NumeroOs",
                table: "ordens_servico",
                columns: new[] { "EmpresaId", "NumeroOs" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ordens_servico_EmpresaId_Status",
                table: "ordens_servico",
                columns: new[] { "EmpresaId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ordens_servico_TecnicoId",
                table: "ordens_servico",
                column: "TecnicoId");

            migrationBuilder.CreateIndex(
                name: "IX_ordens_servico_UpdatedBy",
                table: "ordens_servico",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ordens_servico_itens_EmpresaId",
                table: "ordens_servico_itens",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ordens_servico_itens_OrdemServicoId",
                table: "ordens_servico_itens",
                column: "OrdemServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_ordens_servico_itens_PecaId",
                table: "ordens_servico_itens",
                column: "PecaId");

            migrationBuilder.CreateIndex(
                name: "IX_ordens_servico_itens_ServicoCatalogoId",
                table: "ordens_servico_itens",
                column: "ServicoCatalogoId");

            migrationBuilder.CreateIndex(
                name: "IX_pecas_EmpresaId_Nome",
                table: "pecas",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pecas_FornecedorId",
                table: "pecas",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_registros_personalizados_EmpresaId",
                table: "registros_personalizados",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_registros_personalizados_EmpresaId_ModuloPersonalizadoId_Or~",
                table: "registros_personalizados",
                columns: new[] { "EmpresaId", "ModuloPersonalizadoId", "OrigemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_registros_personalizados_ModuloPersonalizadoId",
                table: "registros_personalizados",
                column: "ModuloPersonalizadoId");

            migrationBuilder.CreateIndex(
                name: "IX_regras_fiscais_produtos_EmpresaId",
                table: "regras_fiscais_produtos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_regras_fiscais_produtos_EmpresaId_TipoDocumentoFiscal_UfOri~",
                table: "regras_fiscais_produtos",
                columns: new[] { "EmpresaId", "TipoDocumentoFiscal", "UfOrigem", "UfDestino", "RegimeTributario", "Ncm" });

            migrationBuilder.CreateIndex(
                name: "IX_servicos_catalogo_EmpresaId_CodigoInterno",
                table: "servicos_catalogo",
                columns: new[] { "EmpresaId", "CodigoInterno" });

            migrationBuilder.CreateIndex(
                name: "IX_servicos_catalogo_EmpresaId_Nome",
                table: "servicos_catalogo",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tecnicos_EmpresaId_Nome",
                table: "tecnicos",
                columns: new[] { "EmpresaId", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_usuario_empresas_EmpresaId",
                table: "usuario_empresas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_empresas_UsuarioId_EmpresaId",
                table: "usuario_empresas",
                columns: new[] { "UsuarioId", "EmpresaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_Email",
                table: "usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_venda_itens_EmpresaId",
                table: "venda_itens",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_venda_itens_PecaId",
                table: "venda_itens",
                column: "PecaId");

            migrationBuilder.CreateIndex(
                name: "IX_venda_itens_VendaId",
                table: "venda_itens",
                column: "VendaId");

            migrationBuilder.CreateIndex(
                name: "IX_vendas_ClienteId",
                table: "vendas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_vendas_CreatedBy",
                table: "vendas",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_vendas_EmpresaId_NumeroVenda",
                table: "vendas",
                columns: new[] { "EmpresaId", "NumeroVenda" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "caixa_lancamentos");

            migrationBuilder.DropTable(
                name: "campos_modulo_layout");

            migrationBuilder.DropTable(
                name: "campos_personalizados");

            migrationBuilder.DropTable(
                name: "configuracoes_fiscais");

            migrationBuilder.DropTable(
                name: "contas_pagar");

            migrationBuilder.DropTable(
                name: "contas_receber");

            migrationBuilder.DropTable(
                name: "credenciais_fiscais_empresas");

            migrationBuilder.DropTable(
                name: "documentos_fiscais_itens");

            migrationBuilder.DropTable(
                name: "estoque_movimentos");

            migrationBuilder.DropTable(
                name: "eventos_fiscais");

            migrationBuilder.DropTable(
                name: "fornecedores_mensagens_historico");

            migrationBuilder.DropTable(
                name: "integracoes_fiscais_jobs");

            migrationBuilder.DropTable(
                name: "KanbanCard");

            migrationBuilder.DropTable(
                name: "KanbanTarefaPrivada");

            migrationBuilder.DropTable(
                name: "OrdemServicoKanbanHistorico");

            migrationBuilder.DropTable(
                name: "ordens_servico_itens");

            migrationBuilder.DropTable(
                name: "registros_personalizados");

            migrationBuilder.DropTable(
                name: "regras_fiscais_produtos");

            migrationBuilder.DropTable(
                name: "usuario_empresas");

            migrationBuilder.DropTable(
                name: "venda_itens");

            migrationBuilder.DropTable(
                name: "caixas_diarios");

            migrationBuilder.DropTable(
                name: "KanbanColuna");

            migrationBuilder.DropTable(
                name: "ordens_servico");

            migrationBuilder.DropTable(
                name: "servicos_catalogo");

            migrationBuilder.DropTable(
                name: "modulos_personalizados");

            migrationBuilder.DropTable(
                name: "pecas");

            migrationBuilder.DropTable(
                name: "vendas");

            migrationBuilder.DropTable(
                name: "KanbanFluxo");

            migrationBuilder.DropTable(
                name: "aparelhos");

            migrationBuilder.DropTable(
                name: "documentos_fiscais");

            migrationBuilder.DropTable(
                name: "tecnicos");

            migrationBuilder.DropTable(
                name: "fornecedores");

            migrationBuilder.DropTable(
                name: "clientes");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "empresas");
        }
    }
}

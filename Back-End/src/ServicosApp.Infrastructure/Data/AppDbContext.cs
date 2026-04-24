using Microsoft.EntityFrameworkCore;
using ServicosApp.Domain.Entities;

namespace ServicosApp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<UsuarioEmpresa> UsuarioEmpresas => Set<UsuarioEmpresa>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<FornecedorMensagemHistorico> FornecedoresMensagensHistorico => Set<FornecedorMensagemHistorico>();
    public DbSet<Aparelho> Aparelhos => Set<Aparelho>();
    public DbSet<Tecnico> Tecnicos => Set<Tecnico>();
    public DbSet<ServicoCatalogo> ServicosCatalogo => Set<ServicoCatalogo>();
    public DbSet<OrdemServico> OrdensServico => Set<OrdemServico>();
    public DbSet<OrdemServicoItem> OrdensServicoItens => Set<OrdemServicoItem>();
    public DbSet<Peca> Pecas => Set<Peca>();
    public DbSet<EstoqueMovimento> EstoqueMovimentos => Set<EstoqueMovimento>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<VendaItem> VendaItens => Set<VendaItem>();
    public DbSet<CaixaDiario> CaixasDiarios => Set<CaixaDiario>();
    public DbSet<CaixaLancamento> CaixaLancamentos => Set<CaixaLancamento>();
    public DbSet<ContaReceber> ContasReceber => Set<ContaReceber>();
    public DbSet<ContaPagar> ContasPagar => Set<ContaPagar>();
    public DbSet<ModuloPersonalizado> ModulosPersonalizados => Set<ModuloPersonalizado>();
    public DbSet<CampoPersonalizado> CamposPersonalizados => Set<CampoPersonalizado>();
    public DbSet<CampoModuloLayout> CamposModuloLayout => Set<CampoModuloLayout>();
    public DbSet<RegistroPersonalizado> RegistrosPersonalizados => Set<RegistroPersonalizado>();
    public DbSet<KanbanFluxo> KanbanFluxos => Set<KanbanFluxo>();
    public DbSet<KanbanColuna> KanbanColunas => Set<KanbanColuna>();
    public DbSet<KanbanCard> KanbanCards => Set<KanbanCard>();
    public DbSet<KanbanTarefaPrivada> KanbanTarefasPrivadas => Set<KanbanTarefaPrivada>();
    public DbSet<OrdemServicoKanbanHistorico> OrdemServicoKanbanHistoricos => Set<OrdemServicoKanbanHistorico>();

    public DbSet<ConfiguracaoFiscal> ConfiguracoesFiscais => Set<ConfiguracaoFiscal>();
    public DbSet<DocumentoFiscal> DocumentosFiscais => Set<DocumentoFiscal>();
    public DbSet<DocumentoFiscalItem> DocumentosFiscaisItens => Set<DocumentoFiscalItem>();
    public DbSet<EventoFiscal> EventosFiscais => Set<EventoFiscal>();
    public DbSet<CredencialFiscalEmpresa> CredenciaisFiscaisEmpresas => Set<CredencialFiscalEmpresa>();
    public DbSet<IntegracaoFiscalJob> IntegracoesFiscaisJobs => Set<IntegracaoFiscalJob>();
    public DbSet<RegraFiscalProduto> RegrasFiscaisProdutos => Set<RegraFiscalProduto>();


    public override int SaveChanges()
    {
        PrepararAuditoria();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        PrepararAuditoria();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void PrepararAuditoria()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<EntityBase>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                    entry.Entity.Id = Guid.NewGuid();

                entry.Entity.CreatedAt = utcNow;
                entry.Entity.UpdatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<EstoqueMovimento>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                    entry.Entity.Id = Guid.NewGuid();

                entry.Entity.CreatedAt = utcNow;
                entry.Entity.UpdatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<CaixaDiario>())
        {
            if (entry.State == EntityState.Added && entry.Entity.Id == Guid.Empty)
                entry.Entity.Id = Guid.NewGuid();
        }

        foreach (var entry in ChangeTracker.Entries<CaixaLancamento>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                    entry.Entity.Id = Guid.NewGuid();

                entry.Entity.CreatedAt = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ContaReceber>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                    entry.Entity.Id = Guid.NewGuid();

                entry.Entity.CreatedAt = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ContaPagar>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                    entry.Entity.Id = Guid.NewGuid();

                entry.Entity.CreatedAt = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<KanbanColuna>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                    entry.Entity.Id = Guid.NewGuid();

                entry.Entity.CreatedAt = utcNow;
                entry.Entity.UpdatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<KanbanCard>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                    entry.Entity.Id = Guid.NewGuid();

                entry.Entity.CreatedAt = utcNow;
                entry.Entity.UpdatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =========================
        // EMPRESA
        // =========================
        modelBuilder.Entity<Empresa>(entity =>
        {
            entity.ToTable("empresas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();

            entity.Property(x => x.RazaoSocial).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NomeFantasia).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Cnpj).HasMaxLength(18).IsRequired();
            entity.Property(x => x.InscricaoEstadual).HasMaxLength(30);
            entity.Property(x => x.InscricaoMunicipal).HasMaxLength(30);
            entity.Property(x => x.RegimeTributario).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200);
            entity.Property(x => x.Telefone).HasMaxLength(20);
            entity.Property(x => x.Cep).HasMaxLength(10);
            entity.Property(x => x.Logradouro).HasMaxLength(200);
            entity.Property(x => x.Numero).HasMaxLength(20);
            entity.Property(x => x.Complemento).HasMaxLength(100);
            entity.Property(x => x.Bairro).HasMaxLength(100);
            entity.Property(x => x.Cidade).HasMaxLength(100);
            entity.Property(x => x.Uf).HasMaxLength(2);
            entity.Property(x => x.LogoUrl).HasColumnType("text");

            entity.HasIndex(x => x.Cnpj).IsUnique();
        });

        // =========================
        // USUARIO
        // =========================
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();

            entity.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.SenhaHash).HasColumnType("text").IsRequired();
            entity.Property(x => x.TermosUsoVersaoAceita).HasMaxLength(30);
            entity.Property(x => x.PoliticaPrivacidadeVersaoAceita).HasMaxLength(30);

            entity.HasIndex(x => x.Email).IsUnique();
        });

        // =========================
        // USUARIO_EMPRESA
        // =========================
        modelBuilder.Entity<UsuarioEmpresa>(entity =>
        {
            entity.ToTable("usuario_empresas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();

            entity.Property(x => x.Perfil).HasMaxLength(30).IsRequired();
            entity.Property(x => x.NivelAcesso).HasDefaultValue(5);

            entity.HasIndex(x => new { x.UsuarioId, x.EmpresaId }).IsUnique();

            entity.HasOne(x => x.Usuario)
                .WithMany(x => x.UsuarioEmpresas)
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Empresa)
                .WithMany(x => x.UsuarioEmpresas)
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================
        // CLIENTE
        // =========================
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("clientes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();

            entity.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            entity.Property(x => x.TipoPessoa).HasMaxLength(10).IsRequired();
            entity.Property(x => x.CpfCnpj).HasMaxLength(20);
            entity.Property(x => x.Telefone).HasMaxLength(20);
            entity.Property(x => x.Email).HasMaxLength(200);
            entity.Property(x => x.Cep).HasMaxLength(10);
            entity.Property(x => x.Logradouro).HasMaxLength(200);
            entity.Property(x => x.Numero).HasMaxLength(20);
            entity.Property(x => x.Complemento).HasMaxLength(100);
            entity.Property(x => x.Bairro).HasMaxLength(100);
            entity.Property(x => x.Cidade).HasMaxLength(100);
            entity.Property(x => x.Uf).HasMaxLength(2);
            entity.Property(x => x.Observacoes).HasColumnType("text");

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => new { x.EmpresaId, x.Nome });
            entity.HasIndex(x => new { x.EmpresaId, x.CpfCnpj });

            entity.HasOne(x => x.Empresa)
                .WithMany(x => x.Clientes)
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Fornecedor>(entity =>
        {
            entity.ToTable("fornecedores");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();

            entity.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            entity.Property(x => x.TipoPessoa).HasMaxLength(10).IsRequired();
            entity.Property(x => x.CpfCnpj).HasMaxLength(20);
            entity.Property(x => x.Contato).HasMaxLength(150);
            entity.Property(x => x.Telefone).HasMaxLength(20);
            entity.Property(x => x.WhatsApp).HasMaxLength(20);
            entity.Property(x => x.Email).HasMaxLength(200);
            entity.Property(x => x.ProdutosFornecidos).HasColumnType("text");
            entity.Property(x => x.MensagemPadrao).HasColumnType("text");
            entity.Property(x => x.Cep).HasMaxLength(10);
            entity.Property(x => x.Logradouro).HasMaxLength(200);
            entity.Property(x => x.Numero).HasMaxLength(20);
            entity.Property(x => x.Complemento).HasMaxLength(100);
            entity.Property(x => x.Bairro).HasMaxLength(100);
            entity.Property(x => x.Cidade).HasMaxLength(100);
            entity.Property(x => x.Uf).HasMaxLength(2);
            entity.Property(x => x.Observacoes).HasColumnType("text");
            entity.Property(x => x.Ativo).HasDefaultValue(true);

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => new { x.EmpresaId, x.Nome });
            entity.HasIndex(x => new { x.EmpresaId, x.CpfCnpj });

            entity.HasOne(x => x.Empresa)
                .WithMany()
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FornecedorMensagemHistorico>(entity =>
        {
            entity.ToTable("fornecedores_mensagens_historico");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Canal).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Assunto).HasMaxLength(200);
            entity.Property(x => x.Mensagem).HasColumnType("text").IsRequired();
            entity.Property(x => x.QuantidadeSolicitada).HasColumnType("decimal(18,3)");

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => x.FornecedorId);
            entity.HasIndex(x => new { x.EmpresaId, x.EnviadoEm });

            entity.HasOne(x => x.Empresa)
                .WithMany()
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Fornecedor)
                .WithMany()
                .HasForeignKey(x => x.FornecedorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Peca)
                .WithMany()
                .HasForeignKey(x => x.PecaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // =========================
        // APARELHO
        // =========================
        modelBuilder.Entity<Aparelho>(entity =>
        {
            entity.ToTable("aparelhos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();

            entity.Property(x => x.Marca).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Modelo).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Cor).HasMaxLength(50);
            entity.Property(x => x.Imei).HasMaxLength(30);
            entity.Property(x => x.SerialNumber).HasMaxLength(60);
            entity.Property(x => x.SenhaAparelho).HasMaxLength(100);
            entity.Property(x => x.Acessorios).HasColumnType("text");
            entity.Property(x => x.EstadoFisico).HasColumnType("text");
            entity.Property(x => x.Observacoes).HasColumnType("text");
            entity.Property(x => x.Ativo).HasDefaultValue(true);

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => x.ClienteId);
            entity.HasIndex(x => new { x.EmpresaId, x.Imei });
            entity.HasIndex(x => new { x.EmpresaId, x.SerialNumber });

            entity.HasOne(x => x.Empresa)
                .WithMany(x => x.Aparelhos)
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Cliente)
                .WithMany(x => x.Aparelhos)
                .HasForeignKey(x => x.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================
        // TECNICO
        // =========================
        modelBuilder.Entity<Tecnico>(entity =>
        {
            entity.ToTable("tecnicos");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Nome)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Telefone)
                .HasMaxLength(20);

            entity.Property(x => x.Email)
                .HasMaxLength(150);

            entity.Property(x => x.Especialidade)
                .HasMaxLength(100);

            entity.Property(x => x.Observacoes)
                .HasMaxLength(500);

            entity.Property(x => x.Ativo)
                .HasDefaultValue(true);

            entity.HasIndex(x => new { x.EmpresaId, x.Nome });
        });

        // =========================
        // ORDEM SERVICO
        // =========================
        modelBuilder.Entity<OrdemServico>(entity =>
        {
            entity.ToTable("ordens_servico");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();

            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.DefeitoRelatado).HasColumnType("text");
            entity.Property(x => x.Diagnostico).HasColumnType("text");
            entity.Property(x => x.LaudoTecnico).HasColumnType("text");
            entity.Property(x => x.ObservacoesInternas).HasColumnType("text");
            entity.Property(x => x.ObservacoesCliente).HasColumnType("text");
            entity.Property(x => x.FotosJson).HasColumnType("text");

            entity.Property(x => x.ValorMaoObra).HasPrecision(14, 2);
            entity.Property(x => x.ValorPecas).HasPrecision(14, 2);
            entity.Property(x => x.Desconto).HasPrecision(14, 2);
            entity.Property(x => x.ValorTotal).HasPrecision(14, 2);

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => x.ClienteId);
            entity.HasIndex(x => x.AparelhoId);
            entity.HasIndex(x => new { x.EmpresaId, x.Status });
            entity.HasIndex(x => new { x.EmpresaId, x.NumeroOs }).IsUnique();

            entity.HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Aparelho)
                .WithMany()
                .HasForeignKey(x => x.AparelhoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Tecnico)
                .WithMany(x => x.OrdensServico)
                .HasForeignKey(x => x.TecnicoId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.UsuarioCriacao)
                .WithMany()
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.UsuarioAtualizacao)
                .WithMany()
                .HasForeignKey(x => x.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================
        // ORDEM SERVICO ITEM
        // =========================
        modelBuilder.Entity<OrdemServicoItem>(entity =>
        {
            entity.ToTable("ordens_servico_itens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();

            entity.Property(x => x.TipoItem).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Ordem).HasDefaultValue(0);

            entity.Property(x => x.Quantidade).HasPrecision(14, 3);
            entity.Property(x => x.CustoUnitario).HasPrecision(14, 2);
            entity.Property(x => x.ValorUnitario).HasPrecision(14, 2);
            entity.Property(x => x.Desconto).HasPrecision(14, 2);
            entity.Property(x => x.ValorTotal).HasPrecision(14, 2);

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => x.OrdemServicoId);

            entity.HasOne(x => x.OrdemServico)
                .WithMany(x => x.Itens)
                .HasForeignKey(x => x.OrdemServicoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ServicoCatalogo)
                .WithMany(x => x.ItensOrdemServico)
                .HasForeignKey(x => x.ServicoCatalogoId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.Peca)
                .WithMany(x => x.ItensOrdemServico)
                .HasForeignKey(x => x.PecaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // =========================
        // PECA
        // =========================
        modelBuilder.Entity<Peca>(entity =>
        {
            entity.ToTable("pecas");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Nome)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.CodigoInterno)
                .HasMaxLength(50);

            entity.Property(x => x.Sku)
                .HasMaxLength(80);

            entity.Property(x => x.Descricao)
                .HasMaxLength(500);

            entity.Property(x => x.Categoria)
                .HasMaxLength(100);

            entity.Property(x => x.Marca)
                .HasMaxLength(100);

            entity.Property(x => x.ModeloCompativel)
                .HasMaxLength(150);

            entity.Property(x => x.Ncm)
                .HasMaxLength(20);

            entity.Property(x => x.Cest)
                .HasMaxLength(20);

            entity.Property(x => x.CfopPadraoNfe)
                .HasMaxLength(10);

            entity.Property(x => x.CfopPadraoNfce)
                .HasMaxLength(10);

            entity.Property(x => x.CstCsosn)
                .HasMaxLength(10);

            entity.Property(x => x.OrigemMercadoria)
                .HasMaxLength(2);

            entity.Property(x => x.Unidade)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(x => x.CustoUnitario)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.PrecoVenda)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.EstoqueAtual)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.EstoqueMinimo)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.GarantiaDias)
                .HasDefaultValue(0);

            entity.Property(x => x.Ativo)
                .HasDefaultValue(true);

            entity.HasIndex(x => new { x.EmpresaId, x.Nome }).IsUnique();
            entity.HasIndex(x => x.FornecedorId);

            entity.HasOne(x => x.Fornecedor)
                .WithMany()
                .HasForeignKey(x => x.FornecedorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // =========================
        // ESTOQUE MOVIMENTO
        // =========================
        modelBuilder.Entity<EstoqueMovimento>(entity =>
        {
            entity.ToTable("estoque_movimentos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();

            entity.Property(x => x.TipoMovimento).HasMaxLength(30).IsRequired();
            entity.Property(x => x.OrigemTipo).HasMaxLength(30);
            entity.Property(x => x.Observacao).HasColumnType("text");

            entity.Property(x => x.Quantidade).HasPrecision(14, 3);
            entity.Property(x => x.CustoUnitario).HasPrecision(14, 2);

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => x.PecaId);
            entity.HasIndex(x => new { x.EmpresaId, x.OrigemTipo, x.OrigemId });

            entity.HasOne(x => x.Peca)
                .WithMany(x => x.MovimentosEstoque)
                .HasForeignKey(x => x.PecaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.UsuarioCriacao)
                .WithMany()
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Venda>(entity =>
        {
            entity.ToTable("vendas");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.NumeroVenda).IsRequired();

            entity.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(x => x.FormaPagamento)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(x => x.Subtotal).HasColumnType("numeric(18,2)");
            entity.Property(x => x.Desconto).HasColumnType("numeric(18,2)");
            entity.Property(x => x.ValorTotal).HasColumnType("numeric(18,2)");

            entity.Property(x => x.Observacoes).HasMaxLength(1000);
            entity.Property(x => x.Ativo).HasDefaultValue(true);

            entity.HasIndex(x => new { x.EmpresaId, x.NumeroVenda }).IsUnique();

            entity.HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.UsuarioCriacao)
                .WithMany()
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VendaItem>(entity =>
        {
            entity.ToTable("venda_itens");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Descricao)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Quantidade).HasColumnType("numeric(18,2)");
            entity.Property(x => x.CustoUnitario).HasColumnType("numeric(18,2)");
            entity.Property(x => x.ValorUnitario).HasColumnType("numeric(18,2)");
            entity.Property(x => x.Desconto).HasColumnType("numeric(18,2)");
            entity.Property(x => x.ValorTotal).HasColumnType("numeric(18,2)");

            entity.HasOne(x => x.Venda)
                .WithMany(x => x.Itens)
                .HasForeignKey(x => x.VendaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Peca)
                .WithMany(x => x.ItensVenda)
                .HasForeignKey(x => x.PecaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CaixaDiario>(entity =>
        {
            entity.ToTable("caixas_diarios");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.DataCaixa).IsRequired();

            entity.Property(x => x.ValorAbertura).HasColumnType("numeric(18,2)");
            entity.Property(x => x.ValorFechamentoSistema).HasColumnType("numeric(18,2)");
            entity.Property(x => x.ValorFechamentoInformado).HasColumnType("numeric(18,2)");
            entity.Property(x => x.Diferenca).HasColumnType("numeric(18,2)");

            entity.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(x => x.Observacoes)
                .HasMaxLength(1000);

            entity.Property(x => x.Ativo)
                .HasDefaultValue(true);

            entity.HasIndex(x => new { x.EmpresaId, x.DataCaixa })
                .IsUnique();

            entity.HasOne(x => x.UsuarioAbertura)
                .WithMany()
                .HasForeignKey(x => x.AbertoPor)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.UsuarioFechamento)
                .WithMany()
                .HasForeignKey(x => x.FechadoPor)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CaixaLancamento>(entity =>
        {
            entity.ToTable("caixa_lancamentos");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Tipo)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(x => x.OrigemTipo)
                .HasMaxLength(30);

            entity.Property(x => x.FormaPagamento)
                .HasMaxLength(30);

            entity.Property(x => x.Valor)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.Observacao)
                .HasMaxLength(1000);

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => x.CaixaDiarioId);

            entity.HasOne(x => x.CaixaDiario)
                .WithMany(x => x.Lancamentos)
                .HasForeignKey(x => x.CaixaDiarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.UsuarioCriacao)
                .WithMany()
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ContaReceber>(entity =>
        {
            entity.ToTable("contas_receber");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.OrigemTipo)
                .HasMaxLength(30);

            entity.Property(x => x.Descricao)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Valor)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.ValorRecebido)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(x => x.FormaPagamento)
                .HasMaxLength(30);

            entity.Property(x => x.Observacoes)
                .HasMaxLength(1000);

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => x.DataVencimento);

            entity.HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ContaPagar>(entity =>
        {
            entity.ToTable("contas_pagar");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Descricao)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Fornecedor)
                .HasMaxLength(200);

            entity.HasOne(x => x.FornecedorCadastro)
                .WithMany()
                .HasForeignKey(x => x.FornecedorId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(x => x.Categoria)
                .HasMaxLength(100);

            entity.Property(x => x.Valor)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.ValorPago)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(x => x.Observacoes)
                .HasMaxLength(1000);

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => x.FornecedorId);
            entity.HasIndex(x => x.DataVencimento);
        });

        modelBuilder.Entity<ModuloPersonalizado>(entity =>
        {
            entity.ToTable("modulos_personalizados");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Nome)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Chave)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(x => x.Descricao)
                .HasMaxLength(300);

            entity.Property(x => x.Ativo)
                .HasDefaultValue(true);

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => new { x.EmpresaId, x.Chave }).IsUnique();

            entity.HasOne(x => x.Empresa)
                .WithMany()
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CampoPersonalizado>(entity =>
        {
            entity.ToTable("campos_personalizados");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Nome)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Chave)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(x => x.Tipo)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(x => x.Placeholder)
                .HasMaxLength(150);

            entity.Property(x => x.ValorPadrao)
                .HasMaxLength(300);

            entity.Property(x => x.OpcoesJson)
                .HasColumnType("text");

            entity.Property(x => x.ExportarExcel)
                .HasDefaultValue(true);

            entity.Property(x => x.ExportarExcelResumo)
                .HasDefaultValue(false);

            entity.Property(x => x.ExportarPdf)
                .HasDefaultValue(true);

            entity.Property(x => x.Aba)
                .IsRequired()
                .HasMaxLength(80)
                .HasDefaultValue("Principal");

            entity.Property(x => x.Ativo)
                .HasDefaultValue(true);

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => x.ModuloPersonalizadoId);
            entity.HasIndex(x => new { x.EmpresaId, x.ModuloPersonalizadoId, x.Chave }).IsUnique();
            entity.HasIndex(x => new { x.ModuloPersonalizadoId, x.Aba, x.Linha, x.Posicao });

            entity.HasOne(x => x.Empresa)
                .WithMany()
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ModuloPersonalizado)
                .WithMany(x => x.Campos)
                .HasForeignKey(x => x.ModuloPersonalizadoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CampoModuloLayout>(entity =>
        {
            entity.ToTable("campos_modulo_layout");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.CampoChave)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(x => x.Aba)
                .IsRequired()
                .HasMaxLength(80)
                .HasDefaultValue("Principal");

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => x.ModuloPersonalizadoId);
            entity.HasIndex(x => new { x.EmpresaId, x.ModuloPersonalizadoId, x.CampoChave }).IsUnique();

            entity.HasOne(x => x.Empresa)
                .WithMany()
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ModuloPersonalizado)
                .WithMany()
                .HasForeignKey(x => x.ModuloPersonalizadoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RegistroPersonalizado>(entity =>
        {
            entity.ToTable("registros_personalizados");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.ValoresJson)
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.Ativo)
                .HasDefaultValue(true);

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => x.ModuloPersonalizadoId);
            entity.HasIndex(x => new { x.EmpresaId, x.ModuloPersonalizadoId, x.OrigemId }).IsUnique();

            entity.HasOne(x => x.Empresa)
                .WithMany()
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ModuloPersonalizado)
                .WithMany(x => x.Registros)
                .HasForeignKey(x => x.ModuloPersonalizadoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KanbanFluxo>(entity =>
        {
            entity.ToTable("KanbanFluxo");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Tipo).HasMaxLength(20).IsRequired();

            entity.HasIndex(x => new { x.EmpresaId, x.Tipo, x.UsuarioId });
        });

        modelBuilder.Entity<KanbanColuna>(entity =>
        {
            entity.ToTable("KanbanColuna");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.NomeInterno).HasMaxLength(100).IsRequired();
            entity.Property(x => x.NomePublico).HasMaxLength(100);
            entity.Property(x => x.Cor).HasMaxLength(20).IsRequired();
            entity.Property(x => x.DescricaoPublica).HasMaxLength(500);
            entity.Property(x => x.TipoFinalizacao).HasMaxLength(20);

            entity.HasOne(x => x.KanbanFluxo)
                .WithMany(x => x.Colunas)
                .HasForeignKey(x => x.KanbanFluxoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.EmpresaId, x.KanbanFluxoId, x.Ordem });
        });

        modelBuilder.Entity<KanbanCard>(entity =>
        {
            entity.ToTable("KanbanCard");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.PublicTrackingToken).HasMaxLength(64).IsRequired();

            entity.Property(x => x.OcultoDoQuadro)
                .HasDefaultValue(false);

            entity.HasIndex(x => new { x.EmpresaId, x.OrdemServicoId }).IsUnique();
            entity.HasIndex(x => x.PublicTrackingToken).IsUnique();
            entity.HasIndex(x => new { x.EmpresaId, x.KanbanColunaId, x.OcultoDoQuadro });

            entity.HasOne(x => x.KanbanColuna)
                .WithMany(x => x.Cards)
                .HasForeignKey(x => x.KanbanColunaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.OrdemServico)
                .WithMany()
                .HasForeignKey(x => x.OrdemServicoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KanbanTarefaPrivada>(entity =>
        {
            entity.ToTable("KanbanTarefaPrivada");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Titulo).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Descricao).HasMaxLength(1000);

            entity.HasOne(x => x.KanbanColuna)
                .WithMany(x => x.TarefasPrivadas)
                .HasForeignKey(x => x.KanbanColunaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.OrdemServico)
                .WithMany()
                .HasForeignKey(x => x.OrdemServicoId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => new { x.EmpresaId, x.UsuarioId, x.KanbanColunaId, x.Ordem });
        });

        modelBuilder.Entity<OrdemServicoKanbanHistorico>(entity =>
        {
            entity.ToTable("OrdemServicoKanbanHistorico");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.NomeColunaOrigem).HasMaxLength(100);
            entity.Property(x => x.NomeColunaDestino).HasMaxLength(100).IsRequired();
            entity.Property(x => x.TituloPublico).HasMaxLength(100);
            entity.Property(x => x.DescricaoPublica).HasMaxLength(500);
            entity.Property(x => x.PublicTrackingToken).HasMaxLength(64).IsRequired();

            entity.HasOne(x => x.OrdemServico)
                .WithMany()
                .HasForeignKey(x => x.OrdemServicoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ColunaOrigem)
                .WithMany()
                .HasForeignKey(x => x.ColunaOrigemId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.ColunaDestino)
                .WithMany()
                .HasForeignKey(x => x.ColunaDestinoId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(x => new { x.EmpresaId, x.OrdemServicoId, x.DataMovimentacao });
        });

        modelBuilder.Entity<ServicoCatalogo>(entity =>
        {
            entity.ToTable("servicos_catalogo");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Nome)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Descricao)
                .HasMaxLength(500);

            entity.Property(x => x.CodigoInterno)
                .HasMaxLength(50);

            entity.Property(x => x.ValorPadrao)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.TempoEstimadoMinutos);

            entity.Property(x => x.GarantiaDias)
                .HasDefaultValue(0);

            entity.Property(x => x.Ativo)
                .HasDefaultValue(true);

            entity.HasIndex(x => new { x.EmpresaId, x.Nome })
                .IsUnique();

            entity.HasIndex(x => new { x.EmpresaId, x.CodigoInterno })
                .IsUnique(false);
        });

        // =========================
        // CONFIGURACAO FISCAL
        // =========================
        modelBuilder.Entity<ConfiguracaoFiscal>(entity =>
        {
            entity.ToTable("configuracoes_fiscais");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Ambiente)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(x => x.RegimeTributario)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.ProvedorFiscal)
                .HasMaxLength(100);

            entity.Property(x => x.MunicipioCodigo)
                .HasMaxLength(20);

            entity.Property(x => x.CnaePrincipal)
                .HasMaxLength(20);

            entity.Property(x => x.ItemListaServico)
                .HasMaxLength(20);

            entity.Property(x => x.CodigoTributarioMunicipio)
                .HasMaxLength(40);

            entity.Property(x => x.Ativo)
                .HasDefaultValue(true);

            entity.HasIndex(x => x.EmpresaId)
                .IsUnique();
        });

        // =========================
        // CREDENCIAL FISCAL EMPRESA
        // =========================
        modelBuilder.Entity<CredencialFiscalEmpresa>(entity =>
        {
            entity.ToTable("credenciais_fiscais_empresas");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.TipoDocumentoFiscal)
                    .HasConversion<string>()
                    .IsRequired()
                    .HasMaxLength(20);

            entity.Property(x => x.Provedor)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.UrlBase)
                .HasMaxLength(500);

            entity.Property(x => x.ClientId)
                .HasMaxLength(200);

            entity.Property(x => x.ClientSecretEncrypted)
                .HasColumnType("text");

            entity.Property(x => x.UsuarioApi)
                .HasMaxLength(200);

            entity.Property(x => x.SenhaApiEncrypted)
                .HasColumnType("text");

            entity.Property(x => x.CertificadoBase64Encrypted)
                .HasColumnType("text");

            entity.Property(x => x.CertificadoSenhaEncrypted)
                .HasColumnType("text");

            entity.Property(x => x.TokenAcesso)
                .HasColumnType("text");

            entity.Property(x => x.Ativo)
                .HasDefaultValue(true);

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => new { x.EmpresaId, x.TipoDocumentoFiscal, x.Provedor });
        });

        // =========================
        // DOCUMENTO FISCAL
        // =========================
        modelBuilder.Entity<DocumentoFiscal>(entity =>
{
    entity.ToTable("documentos_fiscais");

    entity.HasKey(x => x.Id);

    entity.Property(x => x.TipoDocumento)
        .IsRequired()
        .HasMaxLength(20);

    entity.Property(x => x.OrigemTipo)
        .IsRequired()
        .HasMaxLength(30);

    entity.Property(x => x.Status)
        .IsRequired()
        .HasMaxLength(30);

    entity.Property(x => x.Ambiente)
        .IsRequired()
        .HasMaxLength(20);

    entity.Property(x => x.ClienteNome)
        .IsRequired()
        .HasMaxLength(200);

    entity.Property(x => x.ClienteCpfCnpj)
        .HasMaxLength(20);

    entity.Property(x => x.ClienteEmail)
        .HasMaxLength(200);

    entity.Property(x => x.ClienteTelefone)
        .HasMaxLength(20);

    entity.Property(x => x.ClienteCep)
        .HasMaxLength(10);

    entity.Property(x => x.ClienteLogradouro)
        .HasMaxLength(200);

    entity.Property(x => x.ClienteNumero)
        .HasMaxLength(20);

    entity.Property(x => x.ClienteComplemento)
        .HasMaxLength(100);

    entity.Property(x => x.ClienteBairro)
        .HasMaxLength(100);

    entity.Property(x => x.ClienteCidade)
        .HasMaxLength(100);

    entity.Property(x => x.ClienteUf)
        .HasMaxLength(2);

    entity.Property(x => x.ClienteMunicipioCodigo)
        .HasMaxLength(20);

    entity.Property(x => x.SerieRps)
        .HasMaxLength(20);

    entity.Property(x => x.ChaveAcesso)
        .HasMaxLength(80);

    entity.Property(x => x.Protocolo)
        .HasMaxLength(100);

    entity.Property(x => x.CodigoVerificacao)
        .HasMaxLength(100);

    entity.Property(x => x.LinkConsulta)
        .HasMaxLength(1000);

    entity.Property(x => x.NumeroExterno)
        .HasMaxLength(100);

    entity.Property(x => x.Lote)
        .HasMaxLength(100);

    entity.Property(x => x.CodigoRejeicao)
        .HasMaxLength(30);

    entity.Property(x => x.MensagemRejeicao)
        .HasMaxLength(1000);

    entity.Property(x => x.MotivoCancelamento)
        .HasMaxLength(1000);

    entity.Property(x => x.XmlConteudo)
        .HasColumnType("text");

    entity.Property(x => x.PayloadEnvio)
        .HasColumnType("text");

    entity.Property(x => x.PayloadRetorno)
        .HasColumnType("text");

    entity.Property(x => x.XmlUrl)
        .HasMaxLength(1000);

    entity.Property(x => x.PdfUrl)
        .HasMaxLength(1000);

    entity.Property(x => x.ValorServicos)
        .HasColumnType("numeric(18,2)");

    entity.Property(x => x.ValorProdutos)
        .HasColumnType("numeric(18,2)");

    entity.Property(x => x.Desconto)
        .HasColumnType("numeric(18,2)");

    entity.Property(x => x.ValorTotal)
        .HasColumnType("numeric(18,2)");

    entity.Property(x => x.GerarContaReceberQuandoAutorizar)
        .HasDefaultValue(false);

    entity.HasIndex(x => x.EmpresaId);
    entity.HasIndex(x => new { x.EmpresaId, x.TipoDocumento, x.Numero, x.Serie }).IsUnique();
    entity.HasIndex(x => new { x.EmpresaId, x.TipoDocumento, x.OrigemTipo, x.OrigemId }).IsUnique();
    entity.HasIndex(x => new { x.EmpresaId, x.Status });
    entity.HasIndex(x => x.ClienteId);

    entity.HasOne(x => x.UsuarioCriacao)
        .WithMany()
        .HasForeignKey(x => x.CreatedBy)
        .OnDelete(DeleteBehavior.Restrict);
});

        // =========================
        // DOCUMENTO FISCAL ITEM
        // =========================
        modelBuilder.Entity<DocumentoFiscalItem>(entity =>
        {
            entity.ToTable("documentos_fiscais_itens");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.TipoItem)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(x => x.Descricao)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(x => x.Ncm)
                .HasMaxLength(20);

            entity.Property(x => x.Cnae)
                .HasMaxLength(20);

            entity.Property(x => x.ItemListaServico)
                .HasMaxLength(20);

            entity.Property(x => x.Cfop)
                .HasMaxLength(10);

            entity.Property(x => x.Cest)
                .HasMaxLength(20);

            entity.Property(x => x.CstCsosn)
                .HasMaxLength(10);

            entity.Property(x => x.OrigemMercadoria)
                .HasMaxLength(2);

            entity.Property(x => x.Quantidade)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.ValorUnitario)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.Desconto)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.ValorTotal)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.BaseIss)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.AliquotaIss)
                .HasColumnType("numeric(18,4)");

            entity.Property(x => x.ValorIss)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.IssRetido)
                .HasDefaultValue(false);

            entity.Property(x => x.BaseIcms)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.AliquotaIcms)
                .HasColumnType("numeric(18,4)");

            entity.Property(x => x.ValorIcms)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.BasePis)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.AliquotaPis)
                .HasColumnType("numeric(18,4)");

            entity.Property(x => x.ValorPis)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.BaseCofins)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.AliquotaCofins)
                .HasColumnType("numeric(18,4)");

            entity.Property(x => x.ValorCofins)
                .HasColumnType("numeric(18,2)");

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => x.DocumentoFiscalId);

            entity.HasOne(x => x.DocumentoFiscal)
                .WithMany(x => x.Itens)
                .HasForeignKey(x => x.DocumentoFiscalId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ServicoCatalogo)
                .WithMany()
                .HasForeignKey(x => x.ServicoCatalogoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Peca)
                .WithMany()
                .HasForeignKey(x => x.PecaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================
        // REGRA FISCAL PRODUTO
        // =========================
        modelBuilder.Entity<RegraFiscalProduto>(entity =>
        {
            entity.ToTable("regras_fiscais_produtos");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.TipoDocumentoFiscal)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(x => x.UfOrigem)
                .HasMaxLength(2);

            entity.Property(x => x.UfDestino)
                .HasMaxLength(2);

            entity.Property(x => x.RegimeTributario)
                .HasMaxLength(50);

            entity.Property(x => x.Ncm)
                .HasMaxLength(20);

            entity.Property(x => x.Cfop)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(x => x.CstCsosn)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(x => x.Cest)
                .HasMaxLength(20);

            entity.Property(x => x.OrigemMercadoria)
                .IsRequired()
                .HasMaxLength(2);

            entity.Property(x => x.AliquotaIcms)
                .HasColumnType("numeric(18,4)");

            entity.Property(x => x.AliquotaPis)
                .HasColumnType("numeric(18,4)");

            entity.Property(x => x.AliquotaCofins)
                .HasColumnType("numeric(18,4)");

            entity.Property(x => x.Observacoes)
                .HasMaxLength(1000);

            entity.Property(x => x.Ativo)
                .HasDefaultValue(true);

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => new
            {
                x.EmpresaId,
                x.TipoDocumentoFiscal,
                x.UfOrigem,
                x.UfDestino,
                x.RegimeTributario,
                x.Ncm
            });
        });

        // =========================
        // EVENTO FISCAL
        // =========================
        modelBuilder.Entity<EventoFiscal>(entity =>
        {
            entity.ToTable("eventos_fiscais");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.TipoEvento)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(x => x.Protocolo)
                .HasMaxLength(100);

            entity.Property(x => x.Mensagem)
                .HasMaxLength(1000);

            entity.Property(x => x.PayloadEnvio)
                .HasColumnType("text");

            entity.Property(x => x.PayloadRetorno)
                .HasColumnType("text");

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => x.DocumentoFiscalId);
            entity.HasIndex(x => new { x.EmpresaId, x.TipoEvento, x.Status });

            entity.HasOne(x => x.DocumentoFiscal)
                .WithMany(x => x.Eventos)
                .HasForeignKey(x => x.DocumentoFiscalId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.UsuarioCriacao)
                .WithMany()
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================
        // INTEGRACAO FISCAL JOB
        // =========================
        modelBuilder.Entity<IntegracaoFiscalJob>(entity =>
        {
            entity.ToTable("integracoes_fiscais_jobs");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.TipoOperacao)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(x => x.RequestPayload)
                .HasColumnType("text");

            entity.Property(x => x.ResponsePayload)
                .HasColumnType("text");

            entity.Property(x => x.MensagemErro)
                .HasMaxLength(2000);

            entity.HasIndex(x => x.EmpresaId);
            entity.HasIndex(x => x.DocumentoFiscalId);
            entity.HasIndex(x => new { x.EmpresaId, x.Status, x.TipoOperacao });

            entity.HasOne(x => x.DocumentoFiscal)
                .WithMany()
                .HasForeignKey(x => x.DocumentoFiscalId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<DocumentoFiscal>()
    .Property(x => x.TipoDocumento)
    .HasConversion<string>();

        modelBuilder.Entity<DocumentoFiscal>()
            .Property(x => x.OrigemTipo)
            .HasConversion<string>();

        modelBuilder.Entity<DocumentoFiscal>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<DocumentoFiscal>()
            .Property(x => x.Ambiente)
            .HasConversion<string>();

        modelBuilder.Entity<DocumentoFiscalItem>()
            .Property(x => x.TipoItem)
            .HasConversion<string>();

        modelBuilder.Entity<EventoFiscal>()
            .Property(x => x.TipoEvento)
            .HasConversion<string>();

        modelBuilder.Entity<EventoFiscal>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<ConfiguracaoFiscal>()
            .Property(x => x.Ambiente)
            .HasConversion<string>();

    }
}

using Korp.Faturamento.Application.NotasFiscais;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Korp.Faturamento.Infrastructure.Persistencia;

public sealed class ItemNotaFiscalConfiguracao : IEntityTypeConfiguration<ItemNotaFiscal>
{
    public void Configure(EntityTypeBuilder<ItemNotaFiscal> construtor)
    {
        construtor.ToTable("itens_nota_fiscal");

        construtor.HasKey(i => i.Id);
        construtor.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();

        construtor.Property(i => i.NotaFiscalId).HasColumnName("nota_fiscal_id").IsRequired();
        construtor.Property(i => i.ProdutoId).HasColumnName("produto_id").IsRequired();

        construtor.Property(i => i.CodigoProduto)
            .HasColumnName("codigo_produto").HasMaxLength(30).IsRequired();

        construtor.Property(i => i.DescricaoProduto)
            .HasColumnName("descricao_produto").HasMaxLength(200).IsRequired();

        construtor.Property(i => i.Quantidade).HasColumnName("quantidade").IsRequired();

        construtor.HasIndex(i => new { i.NotaFiscalId, i.ProdutoId })
            .IsUnique()
            .HasDatabaseName("ix_itens_nota_fiscal_nota_produto");
    }
}
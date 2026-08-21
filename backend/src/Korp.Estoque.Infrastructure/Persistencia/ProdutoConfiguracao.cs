using Korp.Estoque.Application.Produtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Korp.Estoque.Infrastructure.Persistencia;

public sealed class ProdutoConfiguracao : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> construtor)
    {
        construtor.ToTable("produtos");

        construtor.HasKey(p => p.Id);
        construtor.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        construtor.Property(p => p.Codigo)
            .HasColumnName("codigo").HasMaxLength(30).IsRequired();

        construtor.Property(p => p.Descricao)
            .HasColumnName("descricao").HasMaxLength(200).IsRequired();

        construtor.Property(p => p.Saldo)
            .HasColumnName("saldo").IsRequired();

        construtor.HasIndex(p => p.Codigo)
            .IsUnique()
            .HasDatabaseName("ix_produtos_codigo");
    }
}
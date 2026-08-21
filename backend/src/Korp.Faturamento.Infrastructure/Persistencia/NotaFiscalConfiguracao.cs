using Korp.Faturamento.Application.NotasFiscais;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Korp.Faturamento.Infrastructure.Persistencia;

public sealed class NotaFiscalConfiguracao : IEntityTypeConfiguration<NotaFiscal>
{
    public void Configure(EntityTypeBuilder<NotaFiscal> construtor)
    {
        construtor.ToTable("notas_fiscais");

        construtor.HasKey(n => n.Id);
        construtor.Property(n => n.Id).HasColumnName("id").ValueGeneratedNever();

        construtor.Property(n => n.Numero).HasColumnName("numero").IsRequired();
        construtor.HasIndex(n => n.Numero)
            .IsUnique()
            .HasDatabaseName("ix_notas_fiscais_numero");

        construtor.Property(n => n.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        construtor.Property(n => n.CriadaEm).HasColumnName("criada_em").IsRequired();
        construtor.Property(n => n.FechadaEm).HasColumnName("fechada_em");
        construtor.Metadata
            .FindNavigation(nameof(NotaFiscal.Itens))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        construtor.HasMany(n => n.Itens)
            .WithOne()
            .HasForeignKey(i => i.NotaFiscalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
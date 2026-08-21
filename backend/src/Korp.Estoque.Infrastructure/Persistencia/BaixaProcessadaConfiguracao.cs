using Korp.Estoque.Application.Baixas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Korp.Estoque.Infrastructure.Persistencia;

public sealed class BaixaProcessadaConfiguracao : IEntityTypeConfiguration<BaixaProcessada>
{
    public void Configure(EntityTypeBuilder<BaixaProcessada> construtor)
    {
        construtor.ToTable("baixas_processadas");

        construtor.HasKey(b => b.NotaFiscalId);
        construtor.Property(b => b.NotaFiscalId)
            .HasColumnName("nota_fiscal_id").ValueGeneratedNever();

        construtor.Property(b => b.RespostaJson)
            .HasColumnName("resposta_json").HasColumnType("jsonb").IsRequired();

        construtor.Property(b => b.ProcessadaEm)
            .HasColumnName("processada_em").IsRequired();
    }
}
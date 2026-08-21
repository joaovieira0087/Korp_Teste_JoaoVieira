using System.Net;
using Korp.Faturamento.Application.Integracao;
using Korp.Faturamento.Application.NotasFiscais;
using Korp.Faturamento.Infrastructure.Integracao;
using Korp.Faturamento.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Timeout;

namespace Korp.Faturamento.Infrastructure;

public static class InjecaoDependencia
{
    public static IServiceCollection AdicionarInfraestrutura(
        this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddDbContext<FaturamentoDbContext>(opcoes =>
            opcoes.UseNpgsql(configuracao.GetConnectionString("Postgres")));

        servicos.AddScoped<INotaFiscalRepositorio, NotaFiscalRepositorio>();

        servicos.AddHttpClient<IEstoqueClient, EstoqueHttpClient>(cliente =>
        {
            cliente.BaseAddress = new Uri(
                configuracao["Servicos:Estoque:UrlBase"]
                ?? throw new InvalidOperationException(
                    "Servicos:Estoque:UrlBase não configurado."));
        })
        .AddResilienceHandler("estoque", construtor =>
        {
            construtor.AddTimeout(TimeSpan.FromSeconds(20));

            construtor.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(300),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(r => (int)r.StatusCode >= 500
                                    || r.StatusCode == HttpStatusCode.RequestTimeout)
            });

            construtor.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 4,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(15),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
            });

            construtor.AddTimeout(TimeSpan.FromSeconds(5));
        });

        return servicos;
    }
}
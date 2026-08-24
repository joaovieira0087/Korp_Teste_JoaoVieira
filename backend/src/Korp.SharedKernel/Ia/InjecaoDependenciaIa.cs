using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Korp.SharedKernel.Ia;
using Microsoft.Extensions.DependencyInjection;

namespace Korp.SharedKernel.Ia;

public static class InjecaoDependenciaIa
{
    public static IServiceCollection AdicionarIa(
        this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.Configure<OpcoesIa>(configuracao.GetSection(OpcoesIa.Secao));

        var opcoes = configuracao.GetSection(OpcoesIa.Secao).Get<OpcoesIa>() ?? new OpcoesIa();

        servicos.AddHttpClient<IClienteIa, ClienteOpenAi>(cliente =>
        {
            cliente.BaseAddress = new Uri(opcoes.UrlBase);
            cliente.Timeout = TimeSpan.FromSeconds(opcoes.TimeoutSegundos);

            if (opcoes.Habilitada)
            {
                cliente.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", opcoes.ChaveApi);
            }
        });

        return servicos;
    }
}
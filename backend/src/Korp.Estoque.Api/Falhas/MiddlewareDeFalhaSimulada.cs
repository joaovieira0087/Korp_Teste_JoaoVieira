using Microsoft.AspNetCore.Mvc;

namespace Korp.Estoque.Api.Falhas;

public sealed class MiddlewareDeFalhaSimulada(RequestDelegate proximo)
{
    private const string RotaDeControle = "/api/simulacao-de-falha";

    public async Task InvokeAsync(HttpContext contexto, ControleDeFalha controle)
    {
        var caminho = contexto.Request.Path.Value ?? string.Empty;

        var ehRotaDeControle = caminho.StartsWith(
            RotaDeControle, StringComparison.OrdinalIgnoreCase);

        if (controle.Ativa && !ehRotaDeControle)
        {
            if (controle.Modo is ModoDeFalha.Lentidao)
            {
                await Task.Delay(TimeSpan.FromSeconds(15), contexto.RequestAborted);
            }
            else
            {
                contexto.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

                await contexto.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Serviço indisponível",
                    Detail = "O serviço de estoque está temporariamente fora do ar."
                }, contexto.RequestAborted);

                return;
            }
        }

        await proximo(contexto);
    }
}
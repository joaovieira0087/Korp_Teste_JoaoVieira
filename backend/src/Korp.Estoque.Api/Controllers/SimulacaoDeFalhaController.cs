using Korp.Estoque.Api.Falhas;
using Microsoft.AspNetCore.Mvc;

namespace Korp.Estoque.Api.Controllers;

[ApiController]
[Route("api/simulacao-de-falha")]
[Produces("application/json")]
public sealed class SimulacaoDeFalhaController(ControleDeFalha controle) : ControllerBase
{
    public sealed record Estado(bool Ativa, string Modo);
    public sealed record AtivarRequisicao(ModoDeFalha Modo);

    [HttpGet]
    public ActionResult<Estado> Consultar()
        => Ok(new Estado(controle.Ativa, controle.Modo.ToString()));

    [HttpPost("ativar")]
    public ActionResult<Estado> Ativar(AtivarRequisicao requisicao)
    {
        controle.Ativar(requisicao.Modo);
        return Ok(new Estado(controle.Ativa, controle.Modo.ToString()));
    }

    [HttpPost("desativar")]
    public ActionResult<Estado> Desativar()
    {
        controle.Desativar();
        return Ok(new Estado(controle.Ativa, controle.Modo.ToString()));
    }
}
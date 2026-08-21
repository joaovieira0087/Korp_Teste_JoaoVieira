namespace Korp.SharedKernel.Excecoes;

/// <summary>
/// Base de todo erro previsível de negócio. Quem lança sabe que isso pode
/// acontecer no fluxo normal — não é bug, é regra.
/// </summary>
public abstract class ExcecaoDominio(string mensagem) : Exception(mensagem);

/// <summary>Entrada inválida ou regra de negócio violada. Vira HTTP 400.</summary>
public sealed class ExcecaoRegraDeNegocio(string mensagem) : ExcecaoDominio(mensagem);

/// <summary>O recurso pedido não existe. Vira HTTP 404.</summary>
public sealed class ExcecaoNaoEncontrado(string mensagem) : ExcecaoDominio(mensagem);

/// <summary>O recurso existe, mas o estado atual impede a operação. Vira HTTP 409.</summary>
public sealed class ExcecaoConflito(string mensagem) : ExcecaoDominio(mensagem);

/// <summary> dependência externa fora do ar. Vira HTTP 503.</summary>
public sealed class ExcecaoServicoIndisponivel(string mensagem) : ExcecaoDominio(mensagem);
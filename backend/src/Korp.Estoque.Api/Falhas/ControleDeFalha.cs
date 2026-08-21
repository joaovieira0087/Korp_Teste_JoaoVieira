namespace Korp.Estoque.Api.Falhas;

public enum ModoDeFalha
{
    Indisponivel = 1,
    Lentidao = 2
}

/// <summary>
/// Interruptor em memória para simular indisponibilidade do serviço.
/// Registrado como singleton — o estado vale para o processo inteiro.
/// </summary>
public sealed class ControleDeFalha
{
    public bool Ativa { get; private set; }
    public ModoDeFalha Modo { get; private set; } = ModoDeFalha.Indisponivel;

    public void Ativar(ModoDeFalha modo)
    {
        Ativa = true;
        Modo = modo;
    }

    public void Desativar() => Ativa = false;
}
namespace Korp.SharedKernel.Ia;

public sealed class OpcoesIa
{
    public const string Secao = "Ia";

    public string ChaveApi { get; set; } = string.Empty;
    public string Modelo { get; set; } = "gpt-4o-mini";
    public string UrlBase { get; set; } = "https://api.openai.com/v1/";
    public int TimeoutSegundos { get; set; } = 12;

    public bool Habilitada => !string.IsNullOrWhiteSpace(ChaveApi);
}
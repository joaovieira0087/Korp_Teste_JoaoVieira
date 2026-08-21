using Korp.Estoque.Api.Falhas;
using Korp.Estoque.Application;
using Korp.Estoque.Application.Baixas;
using Korp.Estoque.Application.Produtos;
using Korp.Estoque.Infrastructure;
using Korp.Estoque.Infrastructure.Persistencia;
using Korp.SharedKernel.Web;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var construtor = WebApplication.CreateBuilder(args);

construtor.Services.AddControllers()
    .AddJsonOptions(opcoes =>
    {
        opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
construtor.Services.AddOpenApi();

// Tratamento de erros: ProblemDetails + o tratador global do SharedKernel.
construtor.Services.AddProblemDetails();
construtor.Services.AddExceptionHandler<TratadorGlobalDeExcecoes>();

construtor.Services.AdicionarInfraestrutura(construtor.Configuration);
construtor.Services.AddScoped<ServicoProdutos>();

// Registros para resiliência e baixas de estoque
construtor.Services.AddSingleton<ControleDeFalha>();
construtor.Services.AddScoped<ServicoBaixas>();

var app = construtor.Build();

app.UseExceptionHandler();
app.UseMiddleware<MiddlewareDeFalhaSimulada>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    using var escopo = app.Services.CreateScope();
    await escopo.ServiceProvider.GetRequiredService<EstoqueDbContext>()
        .Database.MigrateAsync();
}

app.MapControllers();

app.Run();
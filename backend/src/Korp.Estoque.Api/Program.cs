using System.Text.Json.Serialization;
using Korp.Estoque.Api.Falhas;
using Korp.Estoque.Application.Baixas;
using Korp.Estoque.Application.Comum;
using Korp.Estoque.Application.Produtos;
using Korp.Estoque.Infrastructure;
using Korp.Estoque.Infrastructure.Persistencia;
using Korp.SharedKernel.Web;
using Microsoft.EntityFrameworkCore;

var construtor = WebApplication.CreateBuilder(args);

construtor.Services.AddControllers()
    .AddJsonOptions(opcoes =>
    {
        opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

construtor.Services.AddOpenApi();

// Tratamento de erros: ProblemDetails + tratador global
construtor.Services.AddProblemDetails();
construtor.Services.AddExceptionHandler<TratadorGlobalDeExcecoes>();

construtor.Services.AdicionarInfraestrutura(construtor.Configuration);
construtor.Services.AddScoped<ServicoProdutos>();

// Registros para resiliência, baixas de estoque e transação
construtor.Services.AddSingleton<ControleDeFalha>();
construtor.Services.AddScoped<ServicoBaixas>();
construtor.Services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();

const string PoliticaCors = "frontend";

construtor.Services.AddCors(opcoes =>
    opcoes.AddPolicy(PoliticaCors, politica => politica
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = construtor.Build();

app.UseExceptionHandler();
app.UseCors(PoliticaCors);
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
using Korp.Faturamento.Application.NotasFiscais;
using Korp.Faturamento.Infrastructure;
using Korp.Faturamento.Infrastructure.Persistencia;
using Korp.SharedKernel.Web;
using Microsoft.EntityFrameworkCore;

var construtor = WebApplication.CreateBuilder(args);

construtor.Services.AddControllers();
construtor.Services.AddOpenApi();

construtor.Services.AddProblemDetails();
construtor.Services.AddExceptionHandler<TratadorGlobalDeExcecoes>();

construtor.Services.AdicionarInfraestrutura(construtor.Configuration);
construtor.Services.AddScoped<ServicoNotasFiscais>();

const string PoliticaCors = "frontend";

construtor.Services.AddCors(opcoes =>
    opcoes.AddPolicy(PoliticaCors, politica => politica
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = construtor.Build();

app.UseExceptionHandler();
app.UseCors(PoliticaCors);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var escopo = app.Services.CreateScope();
    await escopo.ServiceProvider.GetRequiredService<FaturamentoDbContext>()
        .Database.MigrateAsync();
}

app.MapControllers();

app.Run();
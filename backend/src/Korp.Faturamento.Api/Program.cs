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

var app = construtor.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var escopo = app.Services.CreateScope();
    await escopo.ServiceProvider.GetRequiredService<FaturamentoDbContext>()
        .Database.MigrateAsync();
}

app.MapControllers();

app.Run();
using Korp.Estoque.Application.Produtos;
using Korp.Estoque.Infrastructure;
using Korp.Estoque.Infrastructure.Persistencia;
using Korp.SharedKernel.Web;
using Microsoft.EntityFrameworkCore;

var construtor = WebApplication.CreateBuilder(args);

construtor.Services.AddControllers();
construtor.Services.AddOpenApi();

// Tratamento de erros: ProblemDetails + o tratador global do SharedKernel.
construtor.Services.AddProblemDetails();
construtor.Services.AddExceptionHandler<TratadorGlobalDeExcecoes>();

construtor.Services.AdicionarInfraestrutura(construtor.Configuration);
construtor.Services.AddScoped<ServicoProdutos>();

var app = construtor.Build();

// Precisa vir antes de tudo para capturar exceção de qualquer middleware adiante.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Conveniência de demonstração: aplica migrations pendentes ao subir.
    // Em produção isso seria um passo separado do pipeline de deploy.
    using var escopo = app.Services.CreateScope();
    await escopo.ServiceProvider.GetRequiredService<EstoqueDbContext>()
        .Database.MigrateAsync();
}

app.MapControllers();

app.Run();
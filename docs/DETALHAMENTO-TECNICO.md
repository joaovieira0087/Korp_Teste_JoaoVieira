# Detalhamento técnico

Respostas aos itens solicitados na especificação do teste, seguidas das decisões de arquitetura que sustentam a solução.

---

## 1. Quais ciclos de vida do Angular foram utilizados

Três, cada um com uma razão específica.

### `ngOnInit`
Usado em todos os componentes de tela (`Dashboard`, `ProdutoLista`, `NotaLista`, `NotaDetalhe`) para a carga inicial de dados e montagem dos pipelines reativos.

Deliberadamente **não** utilizamos o construtor para isso. O construtor fica reservado à injeção de dependências, sem efeito colateral — no momento em que ele executa, as propriedades de entrada do componente ainda não foram resolvidas.

### `ngOnDestroy`
Presente em todos os componentes que realizam inscrições em Observables. Emite um `Subject` que encerra todas as inscrições ativas:

```typescript
private readonly destruido$ = new Subject<void>();

ngOnDestroy(): void {
  this.destruido$.next();
  this.destruido$.complete();
}
```

Sem isso, uma requisição HTTP em andamento continuaria viva após o usuário navegar para outra rota, tentando atualizar um componente já destruído. É vazamento de memória, e o sintoma costuma ser silencioso até virar degradação de desempenho.

### `ngAfterViewInit`
Usado em `NotaDetalhe` para focar o seletor de produto ao abrir a tela.

Precisa ser este e não o `ngOnInit` porque `@ViewChild` só está preenchido depois que o Angular montou o template. No `ngOnInit`, o elemento ainda não existe no DOM e a referência seria `undefined`.

```typescript
@ViewChild('seletorProduto') private seletorProduto?: ElementRef<HTMLElement>;

ngAfterViewInit(): void {
  this.seletorProduto?.nativeElement.focus();
}
```

**Resumo da distinção:** `ngOnInit` roda quando os dados de entrada estão prontos; `ngAfterViewInit`, quando a view está montada; `ngOnDestroy`, na limpeza.

---

## 2. Uso da biblioteca RxJS

RxJS é usado em três frentes.

### Busca reativa com cancelamento (`ProdutoLista`)

```typescript
this.filtro.valueChanges
  .pipe(
    debounceTime(350),
    distinctUntilChanged(),
    switchMap(termo => {
      this.carregando.set(true);
      return this.produtoService.listar(termo);
    }),
    takeUntil(this.destruido$)
  )
  .subscribe(...);
```

- **`debounceTime(350)`** — aguarda o usuário parar de digitar antes de emitir. Digitar "parafuso" dispara uma requisição, não oito.
- **`distinctUntilChanged()`** — descarta emissões com o mesmo valor. Digitar e apagar um caractere não refaz a busca.
- **`switchMap`** — o mais importante: ao iniciar uma nova busca, **cancela a anterior**, inclusive a requisição HTTP em voo. Sem ele, se a busca por "para" demorasse mais que a busca por "parafuso", o resultado desatualizado chegaria por último e sobrescreveria o correto. É a condição de corrida clássica de campo de busca, eliminada por construção.

### Encerramento de inscrições (`takeUntil`)
Aplicado em toda inscrição de todos os componentes, combinado com o `Subject` do `ngOnDestroy`. Uma linha por inscrição elimina a classe inteira de vazamento por componente destruído.

### Controle de estado visual (`finalize`)
Usado em todas as operações com indicador de carregamento:

```typescript
.pipe(finalize(() => this.carregando.set(false)), takeUntil(this.destruido$))
```

O `finalize` executa quando o Observable termina — **tanto no sucesso quanto no erro**. Desligar o spinner apenas no ramo de sucesso deixaria a tela travada em "carregando" sempre que houvesse falha.

### Observables como base do `HttpClient`
Todos os métodos dos serviços retornam `Observable`. Uma consequência importante: **a requisição só é disparada no `subscribe`**. Chamar `produtoService.listar()` sem se inscrever não gera tráfego nenhum — comportamento diferente de `Promise`, que executa no momento da criação.

### Interceptor funcional
O tratamento global de erros usa `catchError` + `throwError` para notificar sem consumir o erro (detalhado no item 7).

---

## 3. Outras bibliotecas utilizadas e finalidade

### Backend

| Biblioteca | Finalidade |
|---|---|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Provider do EF Core para PostgreSQL. Traduz LINQ para SQL e expõe recursos específicos do banco, como `EF.Functions.ILike` e o tipo `jsonb`. |
| `Microsoft.EntityFrameworkCore.Design` | Ferramentas de linha de comando para geração e aplicação de migrations. |
| `Microsoft.Extensions.Http.Resilience` | Pipeline de resiliência sobre Polly para o `HttpClient` — retry, circuit breaker e timeouts na comunicação entre serviços. |
| `Microsoft.Extensions.Http` | `IHttpClientFactory`, para gerenciamento correto do ciclo de vida de conexões (evita esgotamento de sockets e respeita mudanças de DNS). |

### Frontend

| Biblioteca | Finalidade |
|---|---|
| `@angular/material` | Componentes visuais (detalhado no item 4). |
| `rxjs` | Programação reativa (detalhado no item 2). |
| `@angular/common` | `DatePipe` para formatação de datas em pt-BR. As datas chegam do backend em ISO 8601 UTC; o pipe converte para o fuso do navegador e formata em `dd/MM/yyyy HH:mm`. Dispensou bibliotecas externas de data. |
| `@angular/forms` | Reactive Forms, com validações espelhando as regras das entidades do backend. |

---

## 4. Bibliotecas de componentes visuais

**Angular Material**, exclusivamente. Componentes utilizados:

| Componente | Onde |
|---|---|
| `MatTable` | Listagens de produtos, notas e itens |
| `MatFormField` / `MatInput` / `MatSelect` | Todos os formulários |
| `MatDialog` | Cadastro e edição de produtos |
| `MatSnackBar` | Feedback de sucesso e erro (via interceptor) |
| `MatProgressSpinner` / `MatProgressBar` | Indicadores de processamento |
| `MatButtonToggle` | Filtro de status das notas |
| `MatCard` | Dashboard e detalhe da nota |
| `MatToolbar` | Navegação principal |
| `MatTooltip` | Explicação do selo de origem dos textos de IA |

A escolha se justifica pela cobertura: tabela, formulário com validação visual, diálogo, spinner e notificação são exatamente o que as telas exigem, com acessibilidade e comportamento responsivo já resolvidos. Uma biblioteca só, sem misturar sistemas de design.

---

## 5. Gerenciamento de dependências no Golang

**Não aplicável.** A implementação foi feita em C# / .NET 10, conforme a alternativa permitida pelo enunciado.

O equivalente no ecossistema .NET é o NuGet, com os pacotes declarados nos arquivos `.csproj` de cada projeto. A restauração é automática no `dotnet build` ou `dotnet restore`, e o arquivo de lock garante versões reproduzíveis entre máquinas.

---

## 6. Frameworks utilizados no C#

| Framework | Uso |
|---|---|
| **ASP.NET Core 10** | APIs REST. Optamos por **Controllers** em vez de Minimal APIs, por deixarem a separação de responsabilidades mais explícita e o roteamento mais legível para quem revisa o código. |
| **Entity Framework Core 10** | ORM. Configuração via `IEntityTypeConfiguration`, mantendo as entidades de domínio livres de anotações de persistência. |
| **Polly** (via `Microsoft.Extensions.Http.Resilience`) | Resiliência na comunicação entre microsserviços. |
| **Microsoft.Extensions.DependencyInjection** | Injeção de dependências nativa. Cada camada registra os próprios serviços por método de extensão (`AdicionarInfraestrutura`, `AdicionarIa`). |
| **Microsoft.Extensions.Logging** | Log estruturado, com `traceId` correlacionando resposta de erro e registro no log. |

---

## 7. Tratamento de erros e exceções no backend

O princípio de partida: **distinguir erro previsível de erro imprevisível**.

Erros previsíveis fazem parte do fluxo normal do negócio — código já cadastrado, produto inexistente, saldo insuficiente. O usuário precisa saber exatamente o que houve. Erros imprevisíveis são defeitos, e o usuário não pode ver detalhe algum deles.

### Hierarquia de exceções de domínio

No `Korp.SharedKernel`, sem qualquer referência a HTTP:

```csharp
public abstract class ExcecaoDominio(string mensagem) : Exception(mensagem);

public sealed class ExcecaoRegraDeNegocio      : ExcecaoDominio;  // → 400
public sealed class ExcecaoNaoEncontrado       : ExcecaoDominio;  // → 404
public sealed class ExcecaoConflito            : ExcecaoDominio;  // → 409
public sealed class ExcecaoServicoIndisponivel : ExcecaoDominio;  // → 503
```

A tradução para HTTP acontece na borda, mantendo o domínio limpo.

### Tratador global (`IExceptionHandler`)

Um único ponto converte exceção em resposta:

```csharp
var (status, titulo) = excecao switch
{
    ExcecaoNaoEncontrado       => (404, "Recurso não encontrado"),
    ExcecaoConflito            => (409, "Conflito de estado"),
    ExcecaoRegraDeNegocio      => (400, "Regra de negócio violada"),
    ExcecaoServicoIndisponivel => (503, "Serviço indisponível"),
    DbUpdateException e when EhViolacaoDeChaveDuplicada(e)
                               => (409, "Operação concorrente"),
    _                          => (500, "Erro interno")
};
```

Consequências práticas:

- **Nenhum controller possui `try/catch`.** Eles apenas traduzem HTTP em chamada de método. Uma exceção lançada na entidade de domínio atravessa todas as camadas sem ser tocada.
- **Adicionar um novo tipo de erro é adicionar uma linha.** Quando o `ExcecaoServicoIndisponivel` foi criado para a integração entre serviços, nenhum controller precisou mudar.
- **Um único tratador serve os dois microsserviços**, por viver no `SharedKernel`. Erros têm formato idêntico em todo o sistema.

### Formato de resposta: ProblemDetails (RFC 7807)

```json
{
  "title": "Conflito de estado",
  "status": 409,
  "detail": "Saldo insuficiente para o produto PRF-M8: disponível 8, solicitado 99999.",
  "instance": "POST /api/notas-fiscais/{id}/imprimir",
  "traceId": "00-8977d6fbbe5e348029df0d7358d7a710-559ab85c2b6266f4-00"
}
```

Padrão da indústria, em vez de um formato proprietário.

### Erro 500 não revela detalhes

```csharp
Detail = status is 500
    ? "Ocorreu um erro inesperado. Tente novamente em instantes."
    : excecao.Message
```

A mensagem real vai para o log com `LogError`; o cliente recebe texto genérico. Expor `excecao.Message` num 500 vazaria nomes de tabelas, caminhos de arquivo e, eventualmente, credenciais.

O **`traceId` presente na resposta** costura as duas pontas: o usuário informa o código, e o desenvolvedor localiza o stack trace exato no log.

### Falhas entre microsserviços

O `EstoqueHttpClient` separa dois tipos de falha, com tratamentos distintos:

**Falha de transporte** — a requisição não chegou ou a resposta não voltou. Traduzida para `ExcecaoServicoIndisponivel` com mensagem que informa explicitamente que **nenhuma alteração foi feita**, evitando que o usuário acredite ter perdido a nota:

```csharp
catch (BrokenCircuitException) { ... }
catch (OperationCanceledException) when (!cancelamento.IsCancellationRequested) { ... }
catch (HttpRequestException) { ... }
```

A cláusula `when (!cancelamento.IsCancellationRequested)` distingue "o timeout estourou" de "o usuário cancelou a requisição" — no segundo caso não houve falha alguma.

**Falha de negócio** — a requisição chegou e o Estoque recusou. A mensagem original é **preservada** e repassada ao usuário:

```csharp
throw resposta.StatusCode switch
{
    HttpStatusCode.Conflict   => new ExcecaoConflito(problema),
    HttpStatusCode.NotFound   => new ExcecaoNaoEncontrado(problema),
    HttpStatusCode.BadRequest => new ExcecaoRegraDeNegocio(problema),
    _ => new ExcecaoServicoIndisponivel("...")
};
```

Por isso "Saldo insuficiente para o produto PRF-M8: disponível 8, solicitado 99999" — mensagem nascida na entidade `Produto` do serviço de Estoque — chega íntegra ao navegador, atravessando dois microsserviços.

### Resiliência (Polly)

Pipeline configurado em camadas, da mais externa para a mais interna:

```csharp
construtor.AddTimeout(TimeSpan.FromSeconds(20));   // teto total
construtor.AddRetry(...);                          // 3 tentativas, backoff + jitter
construtor.AddCircuitBreaker(...);                 // 50% falha em 30s → abre por 15s
construtor.AddTimeout(TimeSpan.FromSeconds(5));    // por tentativa
```

A ordem é significativa: cada política envolve as seguintes. Se o timeout por tentativa fosse o mais externo, cancelaria todo o retry na primeira demora.

O **jitter** adiciona variação aleatória ao intervalo entre tentativas, evitando que várias requisições que falharam juntas repitam no mesmo instante e agravem a sobrecarga do serviço já comprometido.

O **circuit breaker** protege as duas pontas: o Faturamento não mantém threads bloqueadas aguardando timeout, e o Estoque ganha folga para se recuperar.

### Retry exige idempotência

Introduzir retry cria um risco: se o Estoque processar a baixa e a resposta se perder, a repetição debitaria o saldo duas vezes. Por isso a baixa é idempotente (item seguinte). **Retry e idempotência são um par** — implementar um sem o outro deixa o sistema pior do que estava.

### Tratamento no frontend

Um interceptor funcional espelha a estrutura do backend:

```typescript
export const erroInterceptor: HttpInterceptorFn = (requisicao, proximo) =>
  proximo(requisicao).pipe(
    catchError((resposta: HttpErrorResponse) => {
      notificacao.erro(traduzir(resposta));
      return throwError(() => resposta);   // notifica, mas não consome
    })
  );
```

Três decisões:

- **`status === 0` é tratado primeiro** — significa que a requisição não chegou a sair (serviço fora do ar ou bloqueio de CORS), e merece mensagem específica.
- **`problema.detail` tem prioridade** sobre qualquer texto genérico. É a mensagem produzida pelo backend com conhecimento do domínio.
- **`throwError` repassa o erro adiante.** Um interceptor que consome o erro silenciosamente deixaria os componentes com o spinner ligado indefinidamente.

---

## 8. Uso de LINQ

LINQ é usado em duas modalidades distintas, e a diferença entre elas é relevante.

### LINQ to Entities — traduzido para SQL

Nos repositórios, as expressões são convertidas pelo provider Npgsql em SQL e executadas no banco. Nenhuma linha desnecessária trafega.

**Busca com filtro** (`ProdutoRepositorio`):

```csharp
var consulta = contexto.Produtos.AsNoTracking();

if (!string.IsNullOrWhiteSpace(filtro))
{
    var termo = $"%{filtro.Trim()}%";
    consulta = consulta.Where(p =>
        EF.Functions.ILike(p.Codigo, termo) ||
        EF.Functions.ILike(p.Descricao, termo));
}

return await consulta.OrderBy(p => p.Codigo).ToListAsync(cancelamento);
```

Gera `SELECT ... WHERE codigo ILIKE $1 OR descricao ILIKE $1 ORDER BY codigo`. O `EF.Functions.ILike` mapeia para o operador `ILIKE` do PostgreSQL (busca sem diferenciar maiúsculas).

**`AsNoTracking()`** é aplicado nas consultas de leitura, dispensando o rastreamento de mudanças do EF. Já em `ObterPorIdAsync` ele é omitido de propósito, porque a atualização depende de o EF detectar as alterações na entidade.

**Consulta em lote, evitando N+1:**

```csharp
await contexto.Produtos
    .Where(p => identificadores.Contains(p.Id))
    .ToListAsync(cancelamento);
```

`Contains` sobre uma coleção vira `WHERE id = ANY(...)`. Uma consulta para todos os produtos da baixa, em vez de uma por item dentro de um laço.

**Agregação sem trafegar linhas:**

```csharp
await contexto.Produtos
    .GroupBy(p => 1)
    .Select(g => new ResumoEstoqueResposta(
        g.Count(),
        g.Sum(p => p.Saldo),
        g.Sum(p => p.Saldo == 0 ? 1 : 0)))
    .FirstOrDefaultAsync(cancelamento);
```

Agrupar por uma constante coloca a tabela inteira num único grupo, e o EF traduz para `SELECT COUNT(*), SUM(saldo), SUM(CASE WHEN saldo = 0 THEN 1 ELSE 0 END) FROM produtos`. Uma consulta, três agregações, zero linha trafegada.

### LINQ to Objects — em memória

No `AnalistaNotas`, sobre coleções já materializadas:

```csharp
var topProduto = notas
    .SelectMany(n => n.Itens)
    .GroupBy(i => i.CodigoProduto)
    .Select(g => new { Codigo = g.Key, Quantidade = g.Sum(i => i.Quantidade) })
    .OrderByDescending(x => x.Quantidade)
    .FirstOrDefault();
```

`SelectMany` achata os itens de todas as notas numa sequência única; `GroupBy` agrupa por produto; `Sum` totaliza dentro de cada grupo.

### A distinção que importa

Enquanto a consulta é `IQueryable`, ela vive no banco. Ao virar `IEnumerable`, passa para a memória da aplicação.

O erro clássico é materializar cedo demais: `contexto.Produtos.ToList().Where(...)` traz a tabela inteira e filtra em C#. Funciona com 50 registros e derruba a aplicação com 500 mil. Todas as consultas deste projeto mantêm a composição em `IQueryable` até o `ToListAsync` final.

---

## Decisões de arquitetura

### Separação em camadas

Cada microsserviço tem três projetos:

- **`Application`** — entidades, regras de negócio e interfaces. Não referencia nenhum outro projeto além do `SharedKernel`. Não conhece Entity Framework nem HTTP.
- **`Infrastructure`** — implementa as interfaces. Aqui vivem o EF Core, o PostgreSQL e os clientes HTTP.
- **`Api`** — controllers, injeção de dependências e middlewares.

A dependência aponta para dentro: `Infrastructure` conhece `Application`, nunca o contrário. Isso é verificado pelo compilador, não pela disciplina do desenvolvedor.

Optamos por três camadas em vez de quatro (Clean Architecture completa). Para um domínio com dois agregados e meia dúzia de casos de uso, a camada adicional acrescentaria cerimônia sem ganho proporcional.

### Modelo de domínio rico

As entidades têm setters privados. Não existe caminho no sistema que produza `produto.Saldo = -5`:

```csharp
public void Debitar(int quantidade)
{
    if (quantidade <= 0)
        throw new ExcecaoRegraDeNegocio("A quantidade a debitar deve ser maior que zero.");

    if (quantidade > Saldo)
        throw new ExcecaoConflito(
            $"Saldo insuficiente para o produto {Codigo}: " +
            $"disponível {Saldo}, solicitado {quantidade}.");

    Saldo -= quantidade;
}
```

A regra do enunciado — saldo 10, nota usa 2, novo saldo 8 — está aqui, na entidade, e não espalhada por controllers. Um controller novo que esquecesse a validação não conseguiria corromper o dado.

`NotaFiscal` é um **agregado**: a coleção de itens é privada e exposta apenas como `IReadOnlyCollection`; o construtor de `ItemNotaFiscal` é `internal`. Toda modificação passa pela raiz, que centraliza a regra de status:

```csharp
private void GarantirQueEstaAberta()
{
    if (Status is not StatusNotaFiscal.Aberta)
        throw new ExcecaoConflito(
            $"A nota fiscal {Numero} está fechada e não pode ser alterada.");
}
```

Escrita uma vez, protege adição de item, remoção e impressão.

### Ordem das operações na impressão

Debitar o estoque **primeiro**, fechar a nota **depois**:

```csharp
await estoqueClient.BaixarAsync(baixa, cancelamento);  // pode lançar
nota.Fechar();
await repositorio.SalvarAlteracoesAsync(cancelamento);
```

Se a ordem fosse invertida e a baixa falhasse, seria necessária uma compensação para reabrir a nota — código adicional que também pode falhar. Do jeito implementado, uma falha na baixa faz a exceção subir antes do commit: **não há nada a desfazer**.

Resta uma janela: se o estoque for debitado e o banco do Faturamento cair antes de gravar o fechamento, teríamos saldo debitado e nota ainda aberta. Graças à idempotência, uma nova tentativa reconhece a baixa já processada, devolve sucesso e fecha a nota. **O sistema converge sozinho para o estado correto.**

### Idempotência

A baixa de estoque usa o **id da nota fiscal como chave natural**, e não um header gerado aleatoriamente pelo cliente. "A baixa da nota X" é uma operação que só pode acontecer uma vez, por definição — o que protege também contra duplo clique no botão de imprimir, cenário em que duas chaves aleatórias seriam distintas.

A tabela `baixas_processadas` tem essa chave como **chave primária**, e armazena a resposta original em `jsonb`. Repetir a operação devolve o mesmo corpo, com os mesmos `saldoAnterior` e `saldoAtual` — idempotência real, não apenas um "já processado".

Débitos e registro de idempotência são confirmados **na mesma transação**. Se fossem separados, existiria um instante em que o saldo já caiu mas o registro ainda não existe, e uma falha nesse ponto permitiria débito duplicado.

Requisições rigorosamente simultâneas passam ambas pela verificação inicial, mas apenas uma consegue inserir; a outra colide com a chave primária. Essa colisão é traduzida em `409` pelo tratador global, identificando o código `23505` do PostgreSQL.

### Concorrência

```csharp
contexto.Produtos.FromSql($"""
    SELECT * FROM produtos
    WHERE id = ANY({ordenados})
    ORDER BY id
    FOR UPDATE
    """)
```

**`FOR UPDATE`** trava as linhas até o fim da transação. No cenário do enunciado, a segunda transação aguarda, lê o saldo já atualizado como 0, e o `Debitar` recusa. Uma nota é impressa; a outra recebe `409` com mensagem correta. Sem a trava, ambas leriam saldo 1 simultaneamente e o resultado seria -1.

**`ORDER BY id`** garante ordem consistente de aquisição das travas. Duas baixas envolvendo os mesmos produtos em ordens diferentes poderiam travar em sentidos opostos e gerar deadlock. Travando sempre na mesma ordem, isso se torna impossível por construção.

Optamos por trava pessimista em vez de controle otimista (`xmin` do PostgreSQL): o resultado é determinístico e dispensa laço de retry, ao custo de espera nas transações concorrentes — aceitável para operações curtas como esta.

### Numeração sequencial

Uma SEQUENCE do PostgreSQL, obtida via `nextval`. A implementação intuitiva — `MAX(numero) + 1` — geraria números duplicados sob concorrência, pela mesma razão do saldo: leitura seguida de escrita não é atômica. `nextval` é atômico por construção.

O custo é a possibilidade de lacunas quando uma transação falha, discutida no README.

### Contratos duplicados entre serviços

O `Korp.SharedKernel` contém apenas código transversal (exceções, tratador de erros, cliente de IA). Os contratos de comunicação entre Estoque e Faturamento são **declarados separadamente em cada lado**.

Compartilhá-los criaria acoplamento em tempo de compilação: o Estoque não poderia alterar seu contrato sem quebrar o build do Faturamento. Os serviços deixariam de ser independentes e o sistema viraria um monolito distribuído — a pior combinação entre as duas arquiteturas.

### Integração com IA

Quatro decisões que definem o desenho:

**O contrato nunca lança exceção.** `IClienteIa.GerarTextoAsync` retorna `string?` — em qualquer falha (sem chave, timeout, erro de API, resposta malformada) devolve `null`, e o chamador aplica o fallback. A garantia de que a IA não derruba o fluxo principal está no tipo de retorno, não na disciplina de quem chama.

**Todos os números são calculados em C#.** Total de notas, unidades faturadas, produto mais movimentado — tudo computado com LINQ sobre os dados reais. A IA recebe as métricas prontas e apenas as redige em prosa. O caminho inverso (enviar os dados e pedir "calcule e analise") produziria relatórios com números incorretos ocasionais. Modelo de linguagem não é calculadora.

**A resposta do modelo é tratada como entrada não confiável.** Um LLM não obedece garantidamente à instrução: pode devolver 400 caracteres quando se pediu 200, ou markdown quando se pediu texto puro. O método `Sanitizar` remove formatação e trunca no limite, independentemente do que o prompt solicitou. A instrução é o pedido; o código é a garantia.

**Fallback determinístico em todas as funcionalidades.** Sem chave de API configurada, o sistema roda integralmente — o avaliador não precisa de conta na OpenAI. Na interface, um selo indica se o texto veio da IA ou do fallback, para que o usuário sempre saiba o que está lendo.

### Degradação parcial no Dashboard

O `ObterResumoAsync` é o único método do `EstoqueHttpClient` que não lança exceção. A razão: é uma leitura opcional que enriquece uma tela, não uma operação crítica.

Se o Estoque estiver indisponível, o bloco correspondente **desaparece** com um aviso explícito, e os indicadores de faturamento continuam sendo exibidos. Exibir zeros no lugar seria pior — zero é um dado, e afirmar que existem zero produtos quando na verdade não sabemos é o tipo de erro que ninguém percebe até tomar uma decisão baseada nele.

Os demais métodos do mesmo cliente continuam lançando `ExcecaoServicoIndisponivel`, porque imprimir uma nota sem o Estoque no ar precisa falhar de forma visível. Duas políticas no mesmo arquivo, cada uma justificada pela criticidade da operação.

### Validação em profundidade

As regras aparecem em três níveis, e a duplicação é intencional:

1. **Interface** — Reactive Forms dão feedback imediato, sem ida ao servidor.
2. **Domínio** — a entidade é a autoridade real; qualquer cliente da API passa por ela.
3. **Banco** — índice único em `produtos.codigo` e em `(nota_fiscal_id, produto_id)`.

A checagem em C# antes de inserir existe para produzir uma mensagem clara; a restrição no banco é a garantia efetiva, válida inclusive com múltiplas instâncias da API em execução.

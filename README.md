# Korp — Sistema de emissão de Notas Fiscais

 **C# + Angular**.

Sistema de emissão de notas fiscais construído em arquitetura de microsserviços, com controle de estoque, faturamento, tratamento de falhas entre serviços e recursos de IA.

---

## Stack

| Camada | Tecnologia |
|---|---|
| Backend | .NET 10 / C#, ASP.NET Core (Controllers) |
| Persistência | PostgreSQL 17 + Entity Framework Core (Npgsql) |
| Resiliência | Polly via `Microsoft.Extensions.Http.Resilience` |
| Frontend | Angular (standalone components + signals) + Angular Material |
| IA | OpenAI Chat Completions, com fallback determinístico |
| Infraestrutura | Docker Compose |

---

## Arquitetura

```
                     ┌────────────────────────┐
                     │   Frontend Angular     │
                     │      :4200             │
                     └───────┬────────┬───────┘
                             │        │
              ┌──────────────┘        └──────────────┐
              ▼                                      ▼
   ┌────────────────────┐   baixa de     ┌────────────────────────┐
   │ Serviço de Estoque │◄───saldo───────│ Serviço de Faturamento │
   │       :5001        │   (HTTP)       │        :5002           │
   └─────────┬──────────┘                └───────────┬────────────┘
             │                                       │
             ▼                                       ▼
     ┌───────────────┐                       ┌────────────────────┐
     │ korp_estoque  │                       │ korp_faturamento   │
     └───────────────┘                       └────────────────────┘
              └──────── PostgreSQL (um container) ────────┘
```

**Cada serviço escreve apenas na própria base.** O Faturamento nunca faz `UPDATE` na tabela de produtos — ele solicita ao Estoque, por HTTP, que a baixa seja feita. É essa fronteira que caracteriza a separação em microsserviços.

Os dois serviços compartilham apenas o projeto `Korp.SharedKernel`, com código transversal (exceções de domínio, tratador global de erros, cliente de IA). Os **contratos de comunicação entre eles são duplicados de propósito**: se fossem compartilhados, um serviço não poderia evoluir sem recompilar o outro.

---

## Como executar

### Pré-requisitos

- .NET SDK 10
- Node.js 20+ e Angular CLI
- Docker Desktop

### 1. Banco de dados

```bash
docker compose up -d
```

Sobe um PostgreSQL 17 com os dois bancos (`korp_estoque` e `korp_faturamento`), criados por script de inicialização, e um volume nomeado para persistência.

Verificação:

```bash
docker exec -it korp-postgres psql -U korp -d postgres -c "\l"
```

### 2. Backends

Em dois terminais separados, a partir de `backend/`:

```bash
dotnet run --project src/Korp.Estoque.Api        # http://localhost:5001
dotnet run --project src/Korp.Faturamento.Api    # http://localhost:5002
```

As migrations são aplicadas automaticamente na inicialização em ambiente de desenvolvimento — não é necessário rodar `dotnet ef` manualmente.

### 3. Frontend

```bash
cd frontend/korp-web
npm install
ng serve
```

Acesse **http://localhost:4200**.

### 4. IA (opcional)

**O sistema funciona integralmente sem chave de API.** Todas as funcionalidades de IA possuem fallback determinístico e a interface indica a origem de cada texto gerado.

Para habilitar a IA:

```bash
cd backend/src/Korp.Estoque.Api
dotnet user-secrets set "Ia:ChaveApi" "sk-..."

cd ../Korp.Faturamento.Api
dotnet user-secrets set "Ia:ChaveApi" "sk-..."
```

A chave é lida via User Secrets e **nunca** é versionada.

---

## Como validar cada requisito

### Obrigatório 1 — Arquitetura de microsserviços

Dois serviços independentes, com bancos separados e comunicação exclusivamente por HTTP:

- **Estoque** (`:5001`) — produtos e saldos
- **Faturamento** (`:5002`) — notas fiscais e itens

Não existe referência de projeto entre eles; o build falharia se alguém tentasse acoplá-los.

### Obrigatório 2 — Tratamento de falhas

O serviço de Estoque expõe um simulador de falha controlável, para demonstração:

```bash
# derruba o estoque
curl -X POST http://localhost:5001/api/simulacao-de-falha/ativar \
     -H "Content-Type: application/json" -d "{\"modo\":\"Indisponivel\"}"

# simula lentidão (dispara o timeout do cliente)
curl -X POST http://localhost:5001/api/simulacao-de-falha/ativar \
     -H "Content-Type: application/json" -d "{\"modo\":\"Lentidao\"}"

# restaura
curl -X POST http://localhost:5001/api/simulacao-de-falha/desativar
```

**Roteiro:** com o estoque derrubado, tente imprimir uma nota pela interface. O sistema exibe mensagem clara ("O serviço de estoque está indisponível no momento. Nenhuma alteração foi feita.") e **a nota permanece Aberta**. Restaure o serviço e imprima novamente: funciona.

O Dashboard demonstra a mesma resiliência em leitura: com o Estoque fora, o bloco de estoque é omitido com aviso explícito e os dados de faturamento continuam sendo exibidos.

### Obrigatório 3 — Conexão real com banco

PostgreSQL em container com volume nomeado. Os dados sobrevivem a `docker compose stop` / `start`.

```bash
docker exec -it korp-postgres psql -U korp -d korp_estoque -c "SELECT * FROM produtos;"
docker exec -it korp-postgres psql -U korp -d korp_faturamento -c "SELECT numero, status FROM notas_fiscais ORDER BY numero;"
```

### Opcional (a) — Tratamento de concorrência

Cenário do enunciado: produto com saldo 1 disputado por duas notas simultâneas.

1. Cadastre um produto com saldo **1**.
2. Crie duas notas, cada uma com 1 unidade desse produto.
3. Dispare as duas impressões em paralelo:

```powershell
$cliente = [System.Net.Http.HttpClient]::new()
$tarefas = @($idNota1, $idNota2) | ForEach-Object {
    $cliente.PostAsync("http://localhost:5002/api/notas-fiscais/$_/imprimir", $null)
}
[System.Threading.Tasks.Task]::WaitAll($tarefas)
foreach ($t in $tarefas) {
    "$([int]$t.Result.StatusCode) -> $($t.Result.Content.ReadAsStringAsync().Result)"
}
```

**Resultado esperado:** uma nota retorna `200` com status `Fechada`; a outra retorna `409` com "Saldo insuficiente para o produto ...: disponível 0, solicitado 1". O saldo final é `0`, nunca negativo.

Implementação: `SELECT ... FOR UPDATE` com `ORDER BY id`, dentro de transação explícita.

### Opcional (b) — Uso de Inteligência Artificial

Três funcionalidades, todas com fallback:

| Serviço | Funcionalidade | Onde ver |
|---|---|---|
| Estoque | Assistente de descrição de produto | Botão "Sugerir descrição" no formulário |
| Faturamento | Resumo executivo da nota | Botão no detalhe da nota |
| Faturamento | Análise do histórico de faturamento | Listagem de notas |
| Faturamento | Visão geral do sistema | Dashboard (carrega automaticamente) |

**Todos os números são calculados em C# com LINQ sobre os dados reais.** A IA apenas redige a prosa a partir de métricas prontas — nunca soma, conta ou infere valores.

Para validar o fallback: remova a chave (`dotnet user-secrets remove "Ia:ChaveApi"`), reinicie os serviços e repita. Tudo continua funcionando, e o selo na interface passa de "Gerado por IA" para "Gerado localmente".

### Opcional (c) — Idempotência

A baixa de estoque é idempotente por nota fiscal.

```bash
# executar duas vezes a MESMA requisição
curl -X POST http://localhost:5001/api/estoque/baixas \
     -H "Content-Type: application/json" \
     -d "{\"notaFiscalId\":\"<id>\",\"itens\":[{\"produtoId\":\"<id>\",\"quantidade\":1}]}"
```

**Resultado esperado:** as duas chamadas retornam `200` com **corpo idêntico** (mesmos `saldoAnterior` e `saldoAtual`), e o saldo do produto é debitado **uma única vez**.

---

## Funcionalidades

### Cadastro de produtos
Código, descrição e saldo. Código único (índice no banco), saldo nunca negativo, validações na entidade de domínio.

### Cadastro de notas fiscais
Numeração sequencial atômica (SEQUENCE do PostgreSQL), status inicial `Aberta`, inclusão de múltiplos produtos com quantidades. Repetir um produto na mesma nota soma a quantidade em vez de duplicar a linha.

### Impressão de notas
Botão com indicador de processamento. O fluxo:

1. Valida que a nota está `Aberta` e possui itens
2. Solicita a baixa de saldo ao serviço de Estoque
3. Somente após o sucesso da baixa, marca a nota como `Fechada`

Se a baixa falhar por qualquer motivo, a exceção sobe antes do commit e **a nota permanece Aberta** — não há compensação a executar. Notas já fechadas não podem ser impressas novamente (`409`).

### Dashboard
Visão consolidada dos dois serviços com resumo executivo gerado por IA. Degrada parcialmente: se o Estoque estiver indisponível, os indicadores de faturamento continuam sendo exibidos com aviso explícito sobre a ausência.

---

## Estrutura do projeto

```
Korp_Teste_JoaoVieira/
├── backend/
│   ├── Korp.slnx
│   └── src/
│       ├── Korp.Estoque.Api/              # controllers, DI, middlewares
│       ├── Korp.Estoque.Application/      # entidades, regras, interfaces
│       ├── Korp.Estoque.Infrastructure/   # EF Core, repositórios
│       ├── Korp.Faturamento.Api/
│       ├── Korp.Faturamento.Application/
│       ├── Korp.Faturamento.Infrastructure/
│       └── Korp.SharedKernel/             # exceções, erro HTTP, cliente de IA
├── frontend/korp-web/
│   └── src/app/
│       ├── nucleo/                        # modelos, serviços, interceptor
│       └── funcionalidades/               # dashboard, produtos, notas
├── docker/postgres/                       # script de criação dos bancos
├── docs/
│   ├── DETALHAMENTO-TECNICO.md
│   └── ROTEIRO-VIDEO.md
├── docker-compose.yml
└── README.md
```

**A dependência aponta para dentro:** `Infrastructure` conhece `Application`, nunca o contrário. A camada de negócio não tem um único `using` de Entity Framework.

---

## Endpoints

### Estoque — `http://localhost:5001`

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/produtos?filtro=` | Lista produtos, com busca opcional |
| GET | `/api/produtos/{id}` | Consulta produto |
| GET | `/api/produtos/resumo` | Indicadores agregados |
| POST | `/api/produtos` | Cadastra produto |
| PUT | `/api/produtos/{id}` | Atualiza produto |
| DELETE | `/api/produtos/{id}` | Remove produto |
| POST | `/api/produtos/assistente/descricao` | Sugestão de descrição (IA) |
| POST | `/api/estoque/baixas` | Baixa de saldo (idempotente) |
| POST | `/api/simulacao-de-falha/ativar` | Simula indisponibilidade |
| POST | `/api/simulacao-de-falha/desativar` | Restaura o serviço |

### Faturamento — `http://localhost:5002`

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/notas-fiscais?status=` | Lista notas, filtro por status |
| GET | `/api/notas-fiscais/{id}` | Consulta nota |
| POST | `/api/notas-fiscais` | Cria nota (numeração sequencial) |
| POST | `/api/notas-fiscais/{id}/itens` | Adiciona item |
| DELETE | `/api/notas-fiscais/{id}/itens/{produtoId}` | Remove item |
| POST | `/api/notas-fiscais/{id}/imprimir` | **Imprime: baixa estoque e fecha** |
| GET | `/api/notas-fiscais/{id}/resumo` | Resumo executivo (IA) |
| GET | `/api/notas-fiscais/analise` | Análise do histórico (IA) |
| GET | `/api/dashboard` | Dados consolidados + visão geral (IA) |

Todos os erros seguem o formato **ProblemDetails (RFC 7807)**, com `traceId` para correlação com os logs.

---

## Detalhamento técnico

As respostas aos itens solicitados na especificação (ciclos de vida do Angular, uso de RxJS, bibliotecas, frameworks, tratamento de erros e uso de LINQ) estão em **[docs/DETALHAMENTO-TECNICO.md](docs/DETALHAMENTO-TECNICO.md)**.

---

## Organização do repositório

O desenvolvimento foi organizado em Pull Requests por funcionalidade, cada um entregando uma unidade que compila e roda:

| PR | Escopo |
|---|---|
| 1 | Estrutura, solution e PostgreSQL via Docker |
| 2 | Cadastro de produtos, EF Core, tratamento global de erros |
| 3 | Notas fiscais, itens e numeração sequencial |
| 4 | Impressão, integração entre serviços e resiliência |
| 5 | Idempotência e tratamento de concorrência |
| 6 | Frontend Angular |
| 7 | Funcionalidades de IA e Dashboard |
| 8 | Documentação |

---

## Observações

**Numeração com lacunas.** As notas podem apresentar números não contíguos. Isso é esperado: a SEQUENCE do PostgreSQL entrega o número antes de a transação ser confirmada e não retrocede em caso de falha. É o preço da atomicidade sob concorrência — a alternativa (`MAX(numero) + 1`) geraria números duplicados quando duas requisições chegassem juntas. Para um documento fiscal real, a continuidade seria garantida por uma tabela de controle transacional.

**Migrations automáticas.** São aplicadas na inicialização apenas em desenvolvimento, para simplificar a execução do projeto. Em produção, isso seria um passo separado do pipeline de deploy — múltiplas instâncias subindo simultaneamente competiriam pela mesma migration.

**Credenciais do banco em texto no compose.** Adequado para ambiente de avaliação. Em produção, iriam para variáveis de ambiente ou gerenciador de segredos.

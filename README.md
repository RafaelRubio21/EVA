# EVA — Entidade Virtual de Apoio

Sistema de secretária virtual inteligente em **.NET 10 (ASP.NET Core Web API)** projetado para realizar a triagem automatizada de e-mails, classificar prioridades utilizando Inteligência Artificial e gerenciar lembretes de resposta para clientes Desktop e Mobile.

---

## Arquitetura e Stack Tecnológico

* **.NET 10 (`net10.0`)** / **C# 14** — ASP.NET Core Web API RESTful
* **Clean Architecture** — Separação estrita entre `Domain`, `Application`, `Infrastructure` e `Api`
* **Rich Domain Model** — Entidades ricas (`EmailRecord` e `EmailAnalysis`) com invariantes encapsuladas
* **CQRS + MediatR** — Segregação de Commands e Queries com `IPipelineBehavior` para **Logging** e **Validação (FluentValidation)**
* **Entity Framework Core 10 + SQL Server** — Mapeamentos via Fluent API (`IEntityTypeConfiguration<T>`) e Migrations
* **Background Services (`PeriodicTimer`)** — Workers assíncronos para ingestão contínua de e-mails não lidos e monitoramento de lembretes vencidos
* **Testes Unitários (xUnit)** — Cobertura das regras de domínio e handlers CQRS

```text
EVA/
├── EVA.slnx
├── src/
│   ├── EVA.Domain/          # Entidades Ricas (EmailRecord, EmailAnalysis), Enums, Exceções e Contratos
│   ├── EVA.Application/     # CQRS (MediatR Commands/Queries), FluentValidation, Pipeline Behaviors
│   ├── EVA.Infrastructure/  # EvaDbContext (EF Core SQL Server), Migrations, Repositórios, Workers e Mocks
│   └── EVA.Api/             # Controllers RESTful, ProblemDetails (RFC 9457), OpenAPI e DI
└── tests/
    └── EVA.UnitTests/       # Testes unitários de Domínio e Aplicação (xUnit)
```

---

## Endpoints REST (`/api/v1/emails`)

| Método | Rota | Descrição |
| :--- | :--- | :--- |
| `GET` | `/api/v1/emails/pending` | Lista e-mails triados que exigem resposta, ordenados por prioridade e prazo final (`Deadline`). |
| `GET` | `/api/v1/emails/{id}` | Obtém os detalhes completos de um e-mail e sua análise de IA pelo ID. |
| `POST` | `/api/v1/emails/process` | Triagem imediata de um novo e-mail via IA com agendamento automático de lembrete. |
| `PATCH` | `/api/v1/emails/{id}/reminder` | Reagenda o próximo lembrete (`NextReminderAt`) respeitando o `Deadline`. |
| `PATCH` | `/api/v1/emails/{id}/complete` | Marca a resposta do e-mail como concluída e cancela lembretes futuros. |

---

## Como Executar

### 1. Configurar a Connection String (User Secrets)

```bash
dotnet user-secrets set "ConnectionStrings:EvaDatabase" "Server=SEU_SERVIDOR,1433;Database=EvaDb;User Id=sa;Password=SUA_SENHA;TrustServerCertificate=True;" --project src/EVA.Api
```

### 2. Aplicar Migrations no SQL Server

```bash
dotnet ef database update --project src/EVA.Infrastructure --startup-project src/EVA.Api
```

### 3. Executar Testes e Iniciar a API

```bash
dotnet test EVA.slnx
dotnet run --project src/EVA.Api
```

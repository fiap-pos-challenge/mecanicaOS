# Arquitetura — Oficina MVP

Documento de referência da arquitetura do MVP do Tech Challenge (Pós Tech SOAT, Fase 1). A fonte da verdade dos requisitos é `docs/enunciado/tech-challenge-fase1.pdf`; fluxos, rotas e modelo de dados estão em `docs/arquitetura/`.

As convenções de código, testes, Git e CI estão em [padrao-desenvolvimento.md](padrao-desenvolvimento.md).

---

## 1. Decisões

| Tema | Decisão |
|---|---|
| Arquitetura | Monólito em camadas, 4 projetos, CQRS leve (sem MediatR) |
| Domínio | Rico, sem classes anêmicas. As regras ficam em agregados, VOs e domain services |
| Validação | No domínio, sem FluentValidation |
| Idioma | Código, rotas, banco e testes em inglês. Commits, docs e mensagens de erro em português |
| Mapeamento | Riok.Mapperly. Não existe pasta `Api/Mappers` |
| Injeção de dependência | `AddDomain`, `AddApplication`, `AddInfrastructure` e `AddPresentation`, todos em `Api/Extensions` |
| Banco | PostgreSQL 16. Schema criado pelo EF Core sem migrations. Dados inseridos direto no banco |
| Swagger | Documentado só com XML comments |
| Testes | xUnit com Assert puro e NSubstitute. Integração com Testcontainers |
| Git | Git flow com merge commit, sem squash |

## 2. Stack

.NET 10 LTS, ASP.NET Core Web API (Controllers), PostgreSQL 16, EF Core 10 + Npgsql + EFCore.NamingConventions, Riok.Mapperly, Swashbuckle, JWT Bearer + BCrypt, ProblemDetails, xUnit + NSubstitute, WebApplicationFactory + Testcontainers + Respawn, coverlet + ReportGenerator, BannedApiAnalyzers, Trivy, Docker multi-stage + docker compose, GitHub Actions.

### Por que PostgreSQL

- A aprovação do orçamento e a baixa de estoque precisam acontecer na mesma transação. O PostgreSQL garante isso (ACID).
- A coluna de sistema `xmin` serve como token de concorrência otimista, sem precisar de coluna extra.
- A sequence gera o número da OS que o cliente vê.
- Índices únicos parciais garantem que um veículo tenha só uma OS aberta.
- É gratuito, tem provider maduro no EF Core e roda no Testcontainers.

## 3. Camadas

```
Api ──► Application ──► Domain
 │                        ▲
 └──► Infrastructure ─────┘ (também referencia Application)
```

| Projeto | Responsabilidade |
|---|---|
| **Oficina.Domain** | Agregados, entidades, VOs, eventos, exceções, domain services e interfaces de repositório. Não referencia nenhum pacote |
| **Oficina.Application** | Use cases (commands), interfaces de consulta (queries), DTOs, mappers Mapperly e handlers de eventos |
| **Oficina.Infrastructure** | DbContext, configurações EF, repositórios, implementação das queries, dispatcher de eventos, JWT, BCrypt e notificação por log |
| **Oficina.Api** | Controllers, `Extensions/` (DI e pipeline), handler global de exceções e policies |

A composição da injeção de dependência fica na Api. Por isso as implementações da Infrastructure são `public`.

### CQRS leve

- **Command:** passa pelo agregado e termina em `IUnitOfWork.CommitAsync`.
- **Query:** vai direto ao EF com `AsNoTracking` e projeta com Mapperly para um DTO. Não passa pelo agregado.

## 4. Estrutura de diretórios

```
mecanicaOS/
├─ src/
│  ├─ Oficina.Api/
│  │  ├─ Controllers/
│  │  │  ├─ Admin/        ServiceOrders, Customers, Vehicles, Services, Parts, Metrics
│  │  │  ├─ Public/       ServiceOrderTracking, Estimates
│  │  │  └─ AuthController.cs
│  │  ├─ Extensions/      DomainExtensions, ApplicationExtensions, InfrastructureExtensions,
│  │  │                   PresentationExtensions, WebApplicationExtensions
│  │  ├─ ExceptionHandling/GlobalExceptionHandler.cs
│  │  ├─ Authorization/Policies.cs
│  │  └─ Program.cs
│  ├─ Oficina.Application/
│  │  ├─ Common/          IUnitOfWork, ICurrentUser, INotifier, IPasswordHasher, ITokenGenerator,
│  │  │                   PagedResult<T>, NotFoundException
│  │  ├─ ServiceOrders/   Commands/<Caso>/ (Command + UseCase), Queries/, ServiceOrderMapper.cs
│  │  ├─ Customers/  Catalog/  Identity/
│  │  └─ Inventory/       ..., EventHandlers/DeductStockOnEstimateApproved.cs
│  ├─ Oficina.Domain/
│  │  ├─ Common/          Entity, AggregateRoot, ValueObject, IDomainEvent, Money, Quantity, exceções
│  │  ├─ ServiceOrders/   ServiceOrder, ServiceItem, PartItem, Estimate, StatusHistory,
│  │  │                   ServiceOrderStatus, Events/, Services/ServiceOrderOpeningPolicy.cs
│  │  ├─ Customers/       Customer, Vehicle, Document, LicensePlate, Email, Phone, VehicleYear,
│  │  │                   Services/RegistrationDeletionPolicy.cs
│  │  ├─ Catalog/         Service
│  │  ├─ Inventory/       Part, PartType, StockMovement, InsufficientStockException
│  │  └─ Identity/        User, Role
│  └─ Oficina.Infrastructure/
│     ├─ Persistence/     OficinaDbContext, UnitOfWork, Configurations/<Context>/
│     ├─ Repositories/<Context>/
│     ├─ Queries/<Context>/
│     ├─ Events/DomainEventDispatcher.cs
│     ├─ Security/        JwtTokenGenerator, BCryptPasswordHasher, ApprovalTokenGenerator
│     └─ Notifications/LogNotifier.cs
├─ tests/
│  ├─ Oficina.UnitTests/          Builders/, Domain/<Context>/, Application/<Context>/
│  └─ Oficina.IntegrationTests/   Infrastructure/, Flows/, Security/
├─ docs/           enunciado/, arquitetura/, ddd/, evidencias/, seguranca/
├─ instructions/   arquitetura.md, padrao-desenvolvimento.md
├─ .github/        workflows/ci.yml, pull_request_template.md
├─ .config/dotnet-tools.json
├─ Dockerfile  docker-compose.yml  .env.example  .editorconfig  BannedSymbols.txt
└─ global.json  Directory.Build.props  Directory.Packages.props  Oficina.slnx  README.md
```

As pastas só são criadas junto com o primeiro arquivo. Não se usa `.gitkeep`.

## 5. Domínio

### 5.1 Glossário (base de `docs/ddd/linguagem-ubiqua.md`)

| Português | Código |
|---|---|
| Ordem de Serviço | `ServiceOrder` |
| Cliente / Documento (CPF/CNPJ) | `Customer` / `Document` |
| Veículo / Placa | `Vehicle` / `LicensePlate` |
| Serviço (catálogo) | `Service` |
| Peça / Insumo | `Part` + `PartType { Part, Supply }` |
| Movimentação de estoque | `StockMovement` |
| Orçamento / Token de aprovação | `Estimate` / `ApprovalToken` |
| Dinheiro / Quantidade | `Money` / `Quantity` |
| Usuário / Perfil | `User` / `Role { Admin, Attendant, Mechanic }` |
| Status da OS | `Received`, `InDiagnosis`, `AwaitingApproval`, `InProgress`, `Completed`, `Delivered` |

`Service` é termo do domínio, então fica proibido o sufixo "Service" em classes técnicas. Por isso os nomes são `JwtTokenGenerator`, `LogNotifier` e assim por diante.

### 5.2 Regras contra classe anêmica

1. Nenhum setter público. Coleções são expostas como `IReadOnlyCollection<T>` sobre uma lista privada.
2. Objetos são criados só por métodos de fábrica (`Customer.Create`, `ServiceOrder.Open`). O EF usa um construtor privado sem parâmetros.
3. Métodos têm nome de intenção, tirado da linguagem ubíqua (`ChangeContact`, `ChangePrice`). Não existe `Update(dto)`.
4. Todo conceito que tem regra vira value object.
5. O estado muda só por comportamento. O agregado registra sozinho o histórico de status e os eventos.
6. O domínio não tem atributos de EF. O mapeamento fica todo na Infrastructure.
7. Só Commands, DTOs e configurações EF podem ficar sem comportamento.
8. Use case não tem `if` de regra de negócio: ele carrega o agregado, chama o domínio e faz o commit.

### 5.3 Comportamento por agregado

| Agregado | Métodos |
|---|---|
| `ServiceOrder` | `Open`, `StartDiagnosis`, `AddServiceItem(Service)`, `AddPartItem(Part, Quantity)`, `RemoveItem`, `SendEstimate`, `ApproveEstimate`, `RejectEstimate`, `Complete`, `Deliver`, `ExecutionTime` |
| `Part` | `Create`, `RegisterEntry`, `DeductForServiceOrder`, `HasAvailable`, `IsBelowMinimum`, `Deactivate` |
| `Service` | `Create`, `ChangeDetails`, `ChangePrice`, `Deactivate` |
| `Customer` | `Create`, `ChangeContact`, `Delete` (exclusão lógica). O documento não muda |
| `Vehicle` | `Create`, `ChangeDetails`, `Delete` (exclusão lógica). A placa não muda |
| `User` | `Create`, `Deactivate`, `CanLogin` |

A `ServiceOrder` recebe `Service` e `Part` como argumento só para ler o preço e a disponibilidade. Ela não guarda referência a outros agregados.

**Regras da OS**
- Não existe alteração livre de status. Cada transição é consequência de uma ação.
- A reprovação do orçamento volta a OS para `InDiagnosis`.
- Serviços e peças só podem ser incluídos em `InDiagnosis`. O preço é copiado do catálogo para o item.
- `Complete` grava a data de término, que é a base do tempo médio de execução.

**Domain services** (registrados via `AddDomain`)
- `ServiceOrderOpeningPolicy`: garante que o veículo tenha no máximo uma OS aberta.
- `RegistrationDeletionPolicy`: impede excluir cliente ou veículo que tenha OS aberta.

**Eventos:** `ServiceOrderOpened`, `EstimateSent`, `EstimateApproved`, `EstimateRejected`, `ServiceOrderCompleted`.

### 5.4 Arredondamento

- `Money` arredonda para 2 casas com cálculo manual, meio para cima, implementado no próprio VO e coberto por testes.
- `Math.Round` e `MidpointRounding` são banidos.
- Todo cálculo monetário passa pelo `Money`.

## 6. Fluxo de requisição, validação e erros

**Command**

1. O controller recebe o command e o `CancellationToken`.
2. O use case busca o agregado no repositório. Se não existir, a resposta é 404.
3. O use case chama o método do agregado, que valida e aplica a regra.
4. `UnitOfWork.CommitAsync(ct)` abre a transação, despacha os eventos de domínio, grava e faz commit. Se algo falhar, faz rollback.
5. O Mapperly converte o resultado em DTO e o controller devolve a resposta.

| Onde | O que valida | HTTP |
|---|---|---|
| Model binding | JSON malformado, tipo errado, campo `required` ausente | 400 |
| VOs e fábricas | CPF/CNPJ, placa, valores e quantidades | 400 (`DomainValidationException`) |
| Repositório e queries | Recurso não encontrado | 404 (`NotFoundException`) |
| Agregado e domain services | Transição inválida, conflito, estoque insuficiente na aprovação | 409 |
| Agregado | Regra de negócio, como peça sem saldo na inclusão | 422 (`BusinessRuleException`) |
| Banco | Violação de `xmin` ou de índice único | 409 |
| Qualquer outra exceção | — | 500, com log |

O código só tem `try/catch` em três lugares: no handler global de exceções, no rollback do UnitOfWork e na tradução de erros do Npgsql.

**Transação da aprovação:** o evento `EstimateApproved` dispara a baixa de estoque antes do commit. Se faltar saldo, a transação inteira sofre rollback e a resposta é 409.

## 7. Contrato da API

### Rotas

| Área | Rotas |
|---|---|
| Autenticação | `POST /api/auth/login` |
| Admin (JWT) | `/api/admin/service-orders`, `/api/admin/customers`, `/api/admin/vehicles`, `/api/admin/services`, `/api/admin/parts`, `/api/admin/parts/{id}/stock-entries`, `/api/admin/metrics/average-execution-time` |
| Ações da OS | `POST /api/admin/service-orders/{id}/start-diagnosis`, `/estimate/send`, `/estimate/approve`, `/estimate/reject`, `/complete`, `/deliver` |
| Pública | `GET /api/public/service-orders/{number}?document=`, `POST /api/public/estimates/{token}/approve`, `POST /api/public/estimates/{token}/reject` |
| Infraestrutura | `GET /swagger`, `GET /health` |

### Formatos

- JSON em camelCase, com enums como string.
- Datas em `DateTimeOffset` UTC, formato ISO 8601. No banco, `timestamptz`.
- Dinheiro em decimal com 2 casas.
- IDs gerados com `Guid.CreateVersion7()` no domínio. O número da OS vem de uma sequence.

### Paginação

A query string é `?page=1&pageSize=20`, com `pageSize` máximo de 100. A resposta tem o formato `{ items, page, pageSize, totalItems }`.

### Verbos e respostas

- `POST` de criação: 201 com Location.
- `PUT`: 200.
- `DELETE`: 204.
- Ações de status: 200 com a OS atualizada.
- Não há `PATCH` nem versionamento de API.

### ProblemDetails

Campos: `title`, `status`, `detail` (em português), `traceId`, `errors` e a extensão `code`, que é estável (por exemplo, `service_order.invalid_transition` ou `inventory.insufficient_stock`). Em produção, o `detail` nunca traz dados internos da exceção.

### Swagger

- Documentado com XML comments nos controllers e DTOs.
- `GenerateDocumentationFile` ligado em Api e Application, com `CS1591` suprimido.

## 8. Permissões

Rotas `/api/admin/**` exigem JWT: sem token a resposta é 401, e com perfil sem permissão é 403. O Admin pode tudo.

| Operação | Atendente | Mecânico | Cliente |
|---|---|---|---|
| Abrir OS, enviar orçamento, entregar veículo | sim | não | não |
| Iniciar diagnóstico, incluir itens, finalizar OS | não | sim | não |
| Aprovar ou reprovar orçamento | sim (presencial) | não | sim (token) |
| Listar e detalhar OS, consultar cadastros e catálogo | sim | sim | não |
| Cadastrar e atualizar clientes e veículos | sim | não | não |
| Excluir cadastros, CRUD de serviços e peças, entrada de estoque, tempo médio | não | não | não |

O cliente não faz login. Ele acompanha a OS pelo número mais o CPF/CNPJ e responde ao orçamento pelo token. O token é de uso único e fica gravado só como hash SHA-256.

## 9. Persistência

- O schema é criado pelo EF Core com `EnsureCreatedAsync` no startup. Não há migrations.
- O modelo EF define o schema inteiro: nomes em snake_case, `xmin` em `ServiceOrder` e `Part`, a sequence do número da OS e o índice único parcial de OS aberta por veículo.
- Não há seed em código nem credenciais no repositório. Dados de teste são inseridos direto no banco, e o `EnsureCreated` não mexe em banco que já existe.
- Quando o modelo muda, é preciso recriar o banco com `docker compose down -v` e reinserir os dados manuais.
- A exclusão lógica usa global query filter. `created_at` e `updated_at` são preenchidos pelo UnitOfWork.
- Não há lazy loading nem repositório genérico. Repositórios existem só para raízes de agregado.

## 10. Segurança e logs

**Segurança**
- A chave do JWT vem de variável de ambiente.
- As policies são definidas por perfil, com `FallbackPolicy` exigindo autenticação. Rotas públicas usam `[AllowAnonymous]` explícito.
- A configuração usa Options pattern com `ValidateOnStart`, no formato `Jwt__Secret`. Toda variável nova entra no `.env.example`.

**Logs**
- `ILogger` com template estruturado e saída JSON no console.
- Nunca são logados: senha, hash, JWT, token de orçamento, CPF/CNPJ, e-mail, telefone, placa e corpo de requisição.
- São logados: IDs, número da OS, transições de status e `traceId`.
- `EnableSensitiveDataLogging` fica sempre desligado.

## 11. Ordem de montagem da estrutura inicial

1. Branches `main` e `develop` protegidas, template de PR e `global.json`.
2. Solution, 6 projetos e referências. `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig` e `BannedSymbols.txt`.
3. `Domain/Common` (bases, `Money`, `Quantity`, exceções) e `Application/Common` (portas).
4. `OficinaDbContext`, `UnitOfWork`, dispatcher de eventos e `EnsureCreated`.
5. `Api/Extensions`, handler global, Swagger com XML e Bearer, JWT, policies e `/health`.
6. Dockerfile, docker-compose (API + Postgres com healthcheck) e `.env.example`.
7. Infraestrutura de testes (factory, Testcontainers, Respawn) com um teste de fumaça por projeto.
8. `ci.yml` completo.

Depois disso, cada contexto entra seguindo os épicos E3 a E7.

## 12. Fora do escopo do MVP

- Orçamento complementar durante `InProgress` e rejeição com encerramento da OS.
- Ajuste de inventário e rota separada de histórico de status (o histórico já vem no detalhe da OS).
- Rate limiting nas rotas públicas, que só entra se sobrar tempo.

O acesso de demonstração será feito com o admin inserido manualmente no banco. O README explica como gerar o hash BCrypt.

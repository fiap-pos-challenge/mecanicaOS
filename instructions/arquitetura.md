# Arquitetura — Oficina MVP

Documento de referência da arquitetura do MVP do Tech Challenge (Pós Tech SOAT, Fase 1). A fonte da verdade dos requisitos é `docs/enunciado/tech-challenge-fase1.pdf`; os diagramas (C4, Event Storming, DDD e Domain Storytelling) estão em `docs/arquitetura/` e a documentação DDD em `docs/ddd/`. As rotas estão no contrato OpenAPI (`tests/Oficina.ContractTests/OpenApi/Snapshots/openapi.json`).

As convenções de código, testes, Git e CI estão em [padrao-desenvolvimento.md](padrao-desenvolvimento.md).

As seções 1 a 12 descrevem o estado atual do código. A seção 13 guarda o histórico das decisões por etapa.

---

## 1. Decisões

| Tema | Decisão |
|---|---|
| Arquitetura | Monólito em camadas (Clean Architecture), 4 projetos em `src/` e 3 de teste. CQRS com **MediatR 12.5.0** (última versão Apache-2.0) |
| Domínio | Rico, sem classes anêmicas. As regras ficam em agregados, VOs, domain services (policies) e nas regras de preço do orçamento (`Domain/Quotes/Pricing`) |
| Validação | Entrada com FluentValidation (pipeline behavior do MediatR). Regras de negócio no domínio |
| Erros | Result pattern (`Result`, `Result<T>` e `Error` com `ErrorType`) para erros esperados, com mensagens nos catálogos `…Errors`. Exceção só para o inesperado |
| Mensageria | Sem broker. Eventos de domínio gravados no Outbox na mesma transação e entregues aos handlers depois do commit pelo `OutboxProcessor` |
| Idioma | Código, rotas, banco e testes em inglês. Commits, docs e mensagens ao usuário (catálogos) em português. Zero comentários no código |
| Mapeamento | Riok.Mapperly. Não existe pasta `Api/Mappers` |
| Injeção de dependência | `AddDomain`, `AddApplication`, `AddInfrastructure`, `AddSecurity` e `AddPresentation`, todos em `Api/Extensions` |
| Banco | SQL Server 2022. Schema por EF Core Migrations, nomes em camelCase, soft delete e auditoria (`createdAt`, `updatedAt`, `deletedAt`) em todas as entidades |
| Swagger | Documentado por metadados de endpoint (`[EndpointSummary]`, `[EndpointDescription]`, `[Tags]`, `[ProducesResponseType]`), disponível só em Development. Snapshot do OpenAPI versionado |
| Testes | xUnit v3 (Microsoft.Testing.Platform) com Assert puro e NSubstitute. Unitários, de contrato e funcionais (Testcontainers MsSql). Cobertura mínima de 85% sobre `src/` |
| Git | Git flow com merge commit, sem squash |

## 2. Stack

.NET 10 LTS, ASP.NET Core Web API (Controllers), SQL Server 2022, EF Core 10 + Microsoft.EntityFrameworkCore.SqlServer, MediatR 12.5.0, FluentValidation, Riok.Mapperly, Swashbuckle, JWT Bearer + BCrypt, ProblemDetails, SignalR, xUnit v3 + NSubstitute, WebApplicationFactory + Testcontainers + Respawn, coverlet + ReportGenerator, BannedApiAnalyzers, SonarQube Community (Docker, profile `quality`), Docker multi-stage + docker compose, GitHub Actions. Front-end em Angular (`web/`).

### Por que SQL Server

- A aprovação do orçamento e a baixa de estoque precisam acontecer na mesma transação. O SQL Server garante isso (ACID).
- A coluna `rowversion` serve como token de concorrência otimista em `ServiceOrder`, `Part` e `Estimate`.
- A sequence gera o número da OS que o cliente vê.
- Índices únicos filtrados (`deletedAt IS NULL`) garantem unicidade só entre registros ativos.
- A edição Developer é gratuita, tem provider oficial no EF Core, imagem oficial em Docker e módulo no Testcontainers.

O PostgreSQL foi a primeira escolha (Etapas 1 e 2) e foi trocado na Etapa 3; a justificativa pública está no README.

## 3. Camadas

```
Api ──► Application ──► Domain
 │                        ▲
 └──► Infrastructure ─────┘ (também referencia Application)
```

| Projeto | Responsabilidade |
|---|---|
| **Oficina.Domain** | Agregados, entidades, VOs, eventos de domínio, catálogos de erro (`…Errors`), domain services (policies), regras de preço do orçamento e interfaces de repositório. Não referencia nenhum projeto nem pacote |
| **Oficina.Application** | Commands e queries do MediatR (um handler e um validator por caso de uso), pipeline behaviors (validação e log), interfaces de consulta (`I<Contexto>Queries`), DTOs, mappers Mapperly, handlers de eventos de domínio e portas para integrações (`Abstractions/`) |
| **Oficina.Infrastructure** | `OficinaDbContext`, configurações EF e migrations, Unidade de Trabalho, repositórios, implementação das queries, Outbox (`OutboxWriter` e `OutboxProcessor`), JWT, BCrypt, Data Protection (segredo do 2FA), e-mail por SMTP, consulta de CEP (ViaCEP), Pix (BR Code e verificação HMAC do webhook) e tarefas em segundo plano (`AutomationSweeper` e `ReportScheduler`) |
| **Oficina.Api** | Controllers (injetam só `ISender`), contratos de entrada (`Contracts/`), `Extensions/` (DI e pipeline), handler global de exceções, ProblemDetails, policies de autorização, rate limiting e hub SignalR (`Realtime/`) |

A composição da injeção de dependência fica na Api. Por isso as implementações da Infrastructure são `public`.

### CQRS leve

- **Command:** passa pelo agregado e termina em `IUnitOfWork.CommitAsync`, que grava os eventos de domínio no Outbox na mesma transação.
- **Query:** vai direto ao EF com `AsNoTracking`, pagina no SQL e mapeia o resultado para DTO com Mapperly. Não passa pelo agregado.

## 4. Estrutura de diretórios

Solution com **7 projetos** (4 em `src/` e 3 em `tests/`), organizada por contexto e por tipo de artefato.

```
mecanicaOS/
├─ src/
│  ├─ Oficina.Api/                 (01 - apresentação)
│  │  ├─ Authorization/  Configuration/  Contracts/<Contexto>/  ExceptionHandling/  Extensions/  Realtime/
│  │  ├─ Controllers/Admin/  Controllers/Public/  Controllers/Auth/
│  │  └─ Program.cs
│  ├─ Oficina.Application/         (02 - casos de uso / CQRS)
│  │  ├─ Abstractions/   Persistence/, Security/, Messaging/, Notifications/, Realtime/,
│  │  │                  Addresses/, Payments/, Links/ (portas implementadas na Infrastructure)
│  │  ├─ Common/         Behaviors/, Pagination/, Privacy/, Validation/
│  │  └─ <Contexto>/     Commands/<Caso>/ (Command, Handler, Validator),
│  │                     Queries/<Caso>/ (Query, Handler, Validator), Queries/I<Contexto>Queries,
│  │                     Dtos/, Mappers/, EventHandlers/
│  ├─ Oficina.Domain/              (03 - domínio)
│  │  ├─ Common/         Entities/, ValueObjects/, Results/, Events/, Interfaces/, Errors/
│  │  └─ <Contexto>/     ServiceOrders, Quotes, Customers, Catalog, Inventory, Identity, Team,
│  │                     Portal, Documents, Notifications, Automations, Reports, Dashboard,
│  │                     Settings, Privacy, Audit
│  │     └─ Entities/  ValueObjects/  Enums/  Events/  Errors/  Services/  Repositories/ (só interfaces)
│  └─ Oficina.Infrastructure/      (04 - persistência, segurança, integrações)
│     ├─ Persistence/    OficinaDbContext, UnitOfWork, Interceptors/, Configurations/<Contexto>/,
│     │                  Conventions/, Converters/, Migrations/, Seeding/, Errors/
│     ├─ Repositories/<Contexto>/   Queries/<Contexto>/   Messaging/Outbox/
│     └─ Security/  Notifications/  Addresses/  Payments/  Links/  Automations/  Reports/
├─ tests/
│  ├─ Oficina.ContractTests/       (05 - contrato da API: OpenAPI, JSON, ProblemDetails, autorização, arquitetura)
│  ├─ Oficina.FunctionalTests/     (06 - fluxos ponta a ponta e persistência real, Testcontainers MsSql)
│  └─ Oficina.UnitTests/           (07 - domínio, application e regras de arquitetura)
│     (os testes espelham a estrutura de src/, com Builders/ e Fixtures/ compartilhados)
├─ web/            front-end Angular
├─ docs/           enunciado/, arquitetura/ (fontes .drawio), ddd/, entrega/, qualidade/, seguranca/
├─ scripts/        ci/, docs/, seed/, sonar/
├─ instructions/   arquitetura.md, padrao-desenvolvimento.md
├─ .github/        workflows/ (ci.yml, cd.yml), pull_request_template.md
├─ .config/dotnet-tools.json
├─ Dockerfile  docker-compose.yml  .env.example  .editorconfig  .gitattributes  .gitignore  .dockerignore
└─ BannedSymbols.txt  global.json  nuget.config  Directory.Build.props  Directory.Packages.props
   Oficina.slnx  README.md
```

Um tipo por arquivo, nome do arquivo igual ao do tipo, namespace igual ao caminho da pasta. Um teste de arquitetura confere o tipo único e o nome do arquivo. As pastas só são criadas junto com o primeiro arquivo. Não se usa `.gitkeep`.

## 5. Domínio

### 5.1 Glossário (resumo de `docs/ddd/linguagem-ubiqua.md`)

| Português | Código |
|---|---|
| Ordem de Serviço / Número da OS | `ServiceOrder` / `ServiceOrderNumber` |
| Cliente / Documento (CPF/CNPJ) | `Customer` / `Document` |
| Veículo / Placa | `Vehicle` / `LicensePlate` |
| Serviço (catálogo) / Pacote | `Service` / `Package` |
| Peça / Insumo | `Part` + `PartType { Part, Supply }` |
| Movimentação de estoque / Baixa | `StockMovement` / `StockMovementType.Deduction` |
| Orçamento | `Estimate` (montagem, descontos, envio e decisão) e `Quote` (orçamento registrado na OS) |
| Token de aprovação | `ApprovalToken` |
| Dinheiro / Quantidade | `Money` / `Quantity` |
| Usuário / Perfil | `User` / `Role { Admin, Attendant, Mechanic, Manager }` |
| Status da OS | `Received`, `InDiagnosis`, `WaitingApproval`, `InProgress`, `Completed`, `Delivered` |
| Status do orçamento | `Pending`, `Approved`, `Rejected` |

`Service` é termo do domínio, então fica proibido o sufixo "Service" em classes técnicas (garantido por teste de arquitetura). Por isso os nomes são `JwtTokenGenerator`, `LogNotifier`, `AutomationSweeper`, `ReportScheduler` e assim por diante.

### 5.2 Regras contra classe anêmica

1. Nenhum setter público. Coleções são expostas como `IReadOnlyCollection<T>` sobre uma lista privada.
2. Objetos são criados só por métodos de fábrica (`Customer.Create`, `ServiceOrder.Open`). O EF usa um construtor privado sem parâmetros.
3. Métodos têm nome de intenção, tirado da linguagem ubíqua (`ChangeContact`, `ChangePrice`). Não existe `Update(dto)`.
4. Todo conceito que tem regra vira value object.
5. O estado muda só por comportamento. O agregado registra sozinho o histórico de status e os eventos.
6. O domínio não tem atributos de EF. O mapeamento fica todo na Infrastructure.
7. Só Commands, DTOs e configurações EF podem ficar sem comportamento.
8. O handler não tem `if` de regra de negócio: ele carrega o agregado, chama o domínio e faz o commit.

### 5.3 Comportamento por agregado

| Agregado | Métodos principais |
|---|---|
| `ServiceOrder` | `Open`, `Plan`, `AssignMechanic`, `StartDiagnosis`, `UpdateChecklist`, `RegisterPhoto`, `WriteDiagnosis`, `MarkSentToQuote`, `AddServiceItem`, `AddPartItem`, `RemoveItem`, `SendQuote`, `SubmitQuote`, `ApproveQuote`, `RejectQuote`, `RejectQuoteAnswer`, `StartTimer`, `StopTimer`, `Complete`, `Deliver` e a propriedade `ExecutionTime` |
| `Estimate` | `Open`, `SaveDraft`, `Price`, `RequestApproval`, `DecideApproval`, `Deliver`, `MarkOpened`, `ConfirmPortalIdentity`, `Approve` (total ou parcial), `Reject`, `NextVersion` |
| `Part` | `Create`, `ChangeDetails`, `RegisterEntry`, `ReceiveStock`, `RegisterExit`, `DeductForServiceOrder`, `Adjust`, `HasAvailable`, `Deactivate`, `Reactivate` |
| `Service` | `Create`, `ChangeDetails`, `ChangePrice`, `ReplaceUsualParts`, `Deactivate`, `Reactivate` |
| `Package` | `Create`, `Change`, `Deactivate`, `Reactivate` |
| `StockCount` | `Open`, `Record`, `Submit`, `Approve`, `Reject` |
| `PurchaseOrder` | `Create`, `Queue`, `MarkSent` |
| `Customer` | `Create`, `ChangeContact`, `ChangeAddress`, `ChangeMessagingConsent`. O documento não muda |
| `Vehicle` | `Create`, `ChangeDetails`, `TransferTo`. A placa não muda |
| `User` | `Create`, `Invite`, `DefinePassword`, `ChangeRole`, `RegisterFailedLogin`, `RegisterSuccessfulLogin`, `PrepareTwoFactor`, `EnableTwoFactor`, `DisableTwoFactor`, `Deactivate`, `Reactivate` |

Excluir cliente ou veículo é exclusão lógica: o registro recebe `deletedAt` pelo interceptor de auditoria e some das consultas pelo global query filter.

A `ServiceOrder` recebe `Service` e `Part` como argumento só para ler o preço e a disponibilidade. Ela não guarda referência a outros agregados.

**Regras da OS**
- Não existe alteração livre de status. Cada transição é consequência de uma ação.
- A recusa do orçamento exige motivo e volta a OS para `InDiagnosis`.
- Serviços e peças só podem ser incluídos em `InDiagnosis`. O preço é copiado do catálogo para o item.
- Cada OS aceita até 12 fotos (`ServiceOrder.MaxPhotos`), guardadas no agregado `ServiceOrderPhoto` e carregadas sob demanda.
- A aprovação grava o início da execução e `Complete` grava a data de término. A diferença entre as duas é a base do tempo médio de execução.

**Decisão do orçamento:** aprovar e recusar passam sempre pelo `QuoteDecider` (Application), seja pelo portal do cliente, pelo balcão (`/api/admin/quotes/{id}/approve` e `/reject`) ou pela rota de transições da OS. Assim o `Estimate` e o `Quote` da OS nunca ficam com decisões diferentes.

**Domain services** (registrados via `AddDomain`)
- `ServiceOrderOpeningPolicy`: garante que o veículo tenha no máximo uma OS aberta.
- `RegistrationDeletionPolicy`: impede excluir cliente ou veículo que tenha OS aberta.
- `QuoteApprovalPolicy`: aprova o orçamento na OS e dá baixa nas peças e insumos aprovados, na mesma transação.
- `UserAdministrationPolicy`: confere se a desativação, a exclusão e a troca de perfil de um usuário são permitidas.

**Regras de preço** (`Domain/Quotes/Pricing`): `EstimatePricing` calcula subtotal, desconto automático (cliente fiel, frota, primeira visita) ou manual e total, e indica quando o desconto passa do limite do perfil.

**Eventos de domínio:** `ServiceOrderOpened`, `ServiceOrderStatusChanged`, `ServiceOrderCompleted`, `QuoteSent`, `QuoteApproved`, `QuoteRejected`, `StockDeducted`, `StockBelowMinimum`, `PaymentConfirmed` e `NotificationCreated`. Todos vão para o Outbox no commit e são entregues aos handlers depois dele.

### 5.4 Arredondamento

- `Money` arredonda para 2 casas com cálculo manual, meio para cima, implementado no próprio VO e coberto por testes.
- `Math.Round` e `MidpointRounding` são banidos.
- Todo cálculo monetário passa pelo `Money`.

## 6. Fluxo de requisição, validação e erros

**Command**

1. O controller monta o command ou a query e envia pelo `ISender` do MediatR, com o `CancellationToken`.
2. O pipeline valida a entrada (FluentValidation) e registra o log. Entrada inválida volta 400 sem chegar ao handler.
3. O handler busca o agregado no repositório. Se não existir, devolve um `Error` do tipo NotFound (404).
4. O handler chama o método do agregado, que valida e aplica a regra e devolve um `Result`.
5. `UnitOfWork.CommitAsync(ct)` abre a transação, grava os eventos de domínio no Outbox, salva e faz commit. Se o banco recusar (concorrência ou índice único), faz rollback e devolve o erro traduzido.
6. O handler devolve o DTO (Mapperly), e o controller converte o `Result` em resposta HTTP ou em ProblemDetails.
7. Depois do commit, o `OutboxProcessor` entrega os eventos aos handlers (e-mail, tempo real, recibo, automações), com retentativas.

| `ErrorType` | Exemplo | HTTP |
|---|---|---|
| `Validation` | CPF/CNPJ, placa, valores e quantidades inválidos | 400 |
| `Unauthorized` | Sem login ou token inválido | 401 |
| `Forbidden` | Perfil sem permissão | 403 |
| `NotFound` | Recurso não encontrado | 404 |
| `Conflict` | Transição inválida, estoque insuficiente na aprovação, `rowversion` ou índice único | 409 |
| `TooLarge` | Planilha de importação acima de 2 MB | 413 |
| `BusinessRule` | Regra de negócio, como peça sem saldo na inclusão | 422 |
| `Locked` | Link do portal bloqueado depois de 3 erros | 423 |
| `TooManyRequests` | Limite de requisições por IP | 429 |
| Exceção não tratada | — | 500, com log e sem stack trace |

Não existem exceções de domínio. `try/catch` aparece só onde há falha externa ou técnica a tratar: o handler global de exceções, o UnitOfWork (rollback e tradução de `SqlException`), o Outbox (retentativas), as integrações (SMTP, ViaCEP), as tarefas em segundo plano, a leitura do XML da NF-e, a leitura das configurações gravadas em JSON e a proteção do segredo do 2FA.

**Transação da aprovação:** a `QuoteApprovalPolicy` aprova o orçamento na OS e chama `Part.DeductForServiceOrder` para cada peça aprovada, tudo no mesmo commit. Se faltar saldo, nada é gravado e a resposta é 409. O evento `StockDeducted` vai para o Outbox junto, e `StockBelowMinimum` é levantado quando a baixa cruza o estoque mínimo.

## 7. Contrato da API

### Rotas

Lista completa no contrato OpenAPI. Resumo por área:

| Área | Rotas |
|---|---|
| Autenticação | `/api/auth`: `login`, `2fa/verify`, `refresh`, `logout`, `reauthenticate`, `2fa/setup`, `2fa/enable`, `2fa/disable`, `invitations/inspect`, `invitations/accept` e `GET me` |
| Cadastros | `/api/admin/customers`, `/api/admin/vehicles` (buscas por `POST …/search`, com o dado no corpo) e `POST /api/addresses/lookup` (CEP) |
| Catálogo | `/api/admin/services`, `/api/admin/parts` e `/api/admin/packages` |
| Estoque e compras | `/api/admin/inventory`: saldo e movimentações, entradas, saídas, código de barras, leitura e confirmação de NF-e, contagens, fornecedores e pedidos de compra |
| Ordens de serviço | `/api/admin/service-orders`: abrir, listar, buscar, detalhar, `transitions`, `start-service`, `checklist`, `photos`, `diagnosis`, `diagnosis/suggestions`, `send-to-quote`, `time-entries/start` e `/stop`, `finish-service`, `mine` e `work` |
| Orçamentos | `/api/admin/quotes`: criar, `draft`, `discount-approvals`, `send`, `delivery`, `approve`, `reject` e `versions` |
| Gestão | `/api/admin/dashboard`, `finance`, `reports`, `assistant`, `metrics/average-execution-time`, `automations`, `notifications`, `jobs`, `customers/imports`, `documents`, `settings`, `team`, `roles`, `users` e `platform` |
| Pública (sem login) | `/api/public`: `service-orders/lookup`, `quotes/{token}/approve`, `portal/access`, `portal/confirm`, `portal/order`, `portal/quote/approve` e `/reject`, `portal/payments/pix`, `portal/payments/{id}`, `portal/payments/card`, `portal/documents/{code}` e `/pdf`, `documents/{code}/verification` e `payments/pix/webhook` |
| Tempo real | Hub SignalR em `/api/hubs/realtime` |
| Infraestrutura | `GET /swagger` (só em Development) e `GET /health` |

### Formatos

- JSON em camelCase, com enums como string.
- Datas em `DateTimeOffset` UTC, formato ISO 8601. No banco, `datetimeoffset`.
- Dinheiro em decimal com 2 casas.
- IDs gerados com `Guid.CreateVersion7()` no domínio. O número da OS vem de uma sequence.

### Paginação

A query string é `?page=1&pageSize=20`, com `pageSize` máximo de 100. A resposta tem o formato `{ items, page, pageSize, totalItems }`.

### Verbos e respostas

- `POST` de criação: 201 com Location.
- `PUT`: 200.
- `DELETE`: 204. Em clientes, veículos e usuários faz exclusão lógica; em serviços e peças, desativa o registro.
- Ações de status: 200 com a OS atualizada.
- Não há `PATCH` nem versionamento de API.

### ProblemDetails

Campos: `title`, `status`, `detail` (em português), `traceId`, `errors` e a extensão `code`, que é estável e segue o formato `<contexto>.<erro>` em camelCase (por exemplo, `serviceOrder.invalidTransition`, `inventory.insufficientStock` ou `portal.locked`). Em produção, o `detail` nunca traz dados internos da exceção.

### Swagger

- Documentado por metadados de endpoint (`[EndpointSummary]`, `[EndpointDescription]`, `[Tags]`, `[ProducesResponseType]`), sem XML comments.
- Publicado só em Development, com esquema Bearer.
- O contrato fica versionado em `tests/Oficina.ContractTests/OpenApi/Snapshots/openapi.json` e é conferido pelos testes de contrato.

## 8. Permissões

Rotas `/api/admin/**` exigem JWT (`FallbackPolicy` autenticada e uma policy por endpoint): sem token a resposta é 401, e com perfil sem permissão é 403. O Admin pode tudo.

| Policy | Perfis | Usada em |
|---|---|---|
| `Authenticated` | Todos os perfis | Consultas comuns: listar e detalhar OS, catálogo, estoque, notificações e `GET /api/auth/me` |
| `Staff` | Admin, Gerente, Atendente | Recepção: clientes, veículos, CEP, abertura e transições da OS, orçamentos, documentos e painel |
| `Mechanics` | Admin, Gerente, Mecânico | Modo mecânico: iniciar atendimento, checklist, fotos, diagnóstico, enviar para orçamento, cronômetro e finalizar |
| `AdminOrManager` | Admin, Gerente | Catálogo, pacotes, entradas e saídas de estoque, NF-e, aprovação de contagens, fornecedores, pedidos de compra, aprovação de desconto, relatórios, financeiro e automações |
| `AdminOnly` | Admin | Usuários e equipe, configurações sensíveis, exclusão de cadastros e tempo médio de execução |

O cliente não faz login. Ele acompanha a OS pela consulta pública (número mais CPF/CNPJ no corpo) e pelo portal, que abre com o link do orçamento e pede os 3 últimos dígitos do telefone; depois de 3 erros, o link é bloqueado. Os tokens do link, da sessão do portal e da aprovação ficam gravados só como hash SHA-256.

## 9. Persistência

- O schema é criado por **EF Core Migrations** (`Persistence/Migrations/`). A API aplica as migrations na subida quando `Database__ApplyMigrationsOnStartup=true`, como em Development.
- O modelo EF define o schema inteiro: nomes em camelCase (convenção própria), `rowversion` em `ServiceOrder`, `Part` e `Estimate`, a sequence do número da OS e os índices únicos filtrados (`deletedAt IS NULL`), incluindo o de OS aberta por veículo.
- Em Development, a API cria o Admin de teste na subida (`DevelopmentAdminSeeder`), com a senha vinda do `.env`. A carga de demonstração completa (`scripts/seed/`) roda uma vez pelo `docker compose` e preenche o banco pela API.
- Nenhuma credencial fica no repositório: os segredos vêm do `.env` (modelo em `.env.example`) ou de User Secrets.
- A exclusão lógica usa global query filter. `createdAt`, `updatedAt` e `deletedAt` são preenchidos por um `SaveChangesInterceptor` com `TimeProvider`.
- Os eventos de domínio são gravados na tabela do Outbox na mesma transação do agregado.
- Não há lazy loading nem repositório genérico. Repositórios existem só para raízes de agregado.

## 10. Segurança e logs

**Segurança**
- A chave do JWT vem de variável de ambiente (`Jwt__Secret`, no `.env` ou em User Secrets), com no mínimo 32 caracteres.
- As policies são definidas por perfil, com `FallbackPolicy` exigindo autenticação. Rotas públicas usam `[AllowAnonymous]` explícito.
- A configuração usa Options pattern com `ValidateOnStart`. O `appsettings.Development.json` não guarda segredos.
- Rate limiting por IP no login, nos convites e em todo `/api/public/**` (429 em ProblemDetails).
- Sessão com refresh token em cookie `HttpOnly` (caminho `/api/auth`), verificação em duas etapas (TOTP) com o segredo cifrado pelo Data Protection e reautenticação antes de mudanças sensíveis.

**Logs**
- `ILogger` com template estruturado e saída JSON no console.
- Nunca são logados: senha, hash, JWT, token de orçamento, CPF/CNPJ, e-mail, telefone, placa e corpo de requisição.
- São logados: IDs, número da OS, transições de status e `traceId`.
- `EnableSensitiveDataLogging` fica sempre desligado.

## 11. Ambiente e entrega

- `docker compose up -d --build` sobe o SQL Server, a API (Development), o front-end e a carga de demonstração. O `.env` é obrigatório (`cp .env.example .env`).
- O SonarQube roda pelo profile `quality` do compose, e `node scripts/sonar/run-analysis.mjs` grava o relatório em `docs/seguranca/`.
- O CI (`ci.yml`) compila, testa com cobertura mínima de 85% e valida o front-end. O CD (`cd.yml`) publica a imagem no GitHub Container Registry e faz um teste de fumaça.

## 12. Fora do escopo do MVP

- Integração com um provedor de Pix real: a confirmação do pagamento é simulada (webhook próprio com HMAC).
- Pagamento com cartão, envio automático por WhatsApp e emissão de nota fiscal.
- Orçamento complementar durante `InProgress` e encerramento da OS na recusa do orçamento (a recusa volta a OS para `InDiagnosis`).
- Rota separada de histórico de status (o histórico já vem no detalhe da OS).

## 13. Histórico das decisões

Registro do que mudou em cada etapa. O estado atual está nas seções 1 a 12; os textos abaixo ficam como histórico e podem citar nomes que não existem mais.

### 13.1 Ordem de montagem da estrutura inicial (Etapa 1)

1. Branches `main` e `develop` protegidas, template de PR e `global.json`.
2. Solution, 6 projetos e referências. `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig` e `BannedSymbols.txt`.
3. `Domain/Common` (bases, `Money`, `Quantity`, exceções) e `Application/Common` (portas).
4. `OficinaDbContext`, `UnitOfWork`, dispatcher de eventos e `EnsureCreated`.
5. `Api/Extensions`, handler global, Swagger com XML e Bearer, JWT, policies e `/health`.
6. Dockerfile e docker-compose (API + SQL Server com healthcheck).
7. Infraestrutura de testes (factory, Testcontainers, Respawn) com um teste de fumaça por projeto.
8. `ci.yml` completo.

Depois disso, cada contexto entrou seguindo os épicos E3 a E7.

### 13.2 Mudanças da Etapa 3

Consolida também as decisões da Etapa 2.

| Tema | Mudança |
|---|---|
| Banco | PostgreSQL → **SQL Server 2022**: `xmin` → `rowversion` (propriedade sombra), sequence do SQL Server para o número da OS, índices únicos filtrados (`deletedAt IS NULL`), tradução de `SqlException` |
| Persistência | `EnsureCreated` → **EF Core Migrations**. Soft delete obrigatório com global query filter e auditoria por `SaveChangesInterceptor` usando `TimeProvider`. Nomes em **camelCase** |
| Aplicação | CQRS com **MediatR 12.5.0** e FluentValidation em pipeline behavior. **Result pattern** (`Result`, `Result<T>`, `Error`) para erros esperados; exceção só para o inesperado |
| Mensageria | Sem broker: eventos de domínio gravados no **Outbox** na mesma transação, processados por `BackgroundService` e entregues ao `LogNotifier` |
| Rotas | `/api/admin/**` (equipe, JWT, policy por endpoint), `/api/public/**` (cliente sem login) e `/api/auth/**`. **Nenhum dado pessoal na URL:** a consulta pública da OS é `POST /api/public/service-orders/lookup`, e a busca de cliente por documento é `POST /api/admin/customers/search`. `GET /api/auth/me` devolve o usuário logado |
| Erros | ProblemDetails (RFC 9457) com `code`, `traceId` e `errors`. Erro 500 sem stack trace. Mensagens só nos catálogos `…Errors`, `…Messages` e `ProblemTitles` |
| Domínio | Termos `Quote` (orçamento) e `WaitingApproval` (aguardando aprovação). Sem rascunho, sem aceite parcial e sem pagamento. Sem inicializadores; obrigatório não anulável; estado interno criado em todos os construtores |
| Estrutura | 7 projetos: `Oficina.Api`, `Oficina.Application`, `Oficina.Domain`, `Oficina.Infrastructure`, `Oficina.ContractTests`, `Oficina.FunctionalTests` e `Oficina.UnitTests` (ver §4) |
| Segurança | `FallbackPolicy` autenticada, uma policy por endpoint, CORS por configuração e rate limiting no login (5 por minuto por IP) |
| Usuários | Em Development, a API cria na subida o Admin de teste (`admin@oficina.local`), se ele ainda não existir. O Admin cadastra Atendentes e Mecânicos pela API |
| Front-end | Contrato estável para um cliente Angular: JSON camelCase, enums como string, datas ISO 8601 UTC, paginação `{ items, page, pageSize, totalItems }` e CORS para `http://localhost:4200` em desenvolvimento |
| Qualidade | Zero comentários no código (checado no CI) e cobertura mínima de 85% sobre `src/` |

### 13.3 Estado ao fim da Etapa 3

| Tema | Como ficou |
|---|---|
| Módulos com API | Identidade (login, `me`, usuários) e Clientes. OS, catálogo, estoque e rotas públicas têm o domínio pronto e entram nas próximas etapas |
| Rotas | `POST /api/auth/login` (anônimo, 5 tentativas por minuto por IP), `GET /api/auth/me`, `/api/admin/customers` (Admin e Atendente; exclusão só Admin) e `/api/admin/users` (só Admin), com busca por `POST …/search` e o dado no corpo |
| Erros | Result pattern de ponta a ponta. Mapeamento: Validation 400, Unauthorized 401, Forbidden 403, NotFound 404, Conflict 409, BusinessRule 422, inesperado 500; HTTP 413 e 429. Não existem as exceções `DomainValidationException`, `NotFoundException` e `BusinessRuleException` da §6: exceção só para o inesperado (`GlobalExceptionHandler`) |
| Mapeamento | Mapperly converte entidade em DTO. Como os value objects são gravados por conversores do EF Core, a consulta faz a paginação no SQL (`OFFSET/FETCH`), materializa com `AsNoTracking` e só então mapeia; não há projeção `IQueryable` |
| Nomes no banco | camelCase por uma convenção própria do EF Core (`IModelFinalizingConvention`), sem o pacote `EFCore.NamingConventions` |
| Swagger | Swashbuckle só em Development, com esquema Bearer, documentado por atributos (`EndpointSummary`, `EndpointDescription`, `Tags`, `ProducesResponseType`), sem XML comments. Snapshot versionado em `tests/Oficina.ContractTests/OpenApi/Snapshots/openapi.json` |
| JWT | HS256 com `sub`, `role` e `jti` (sem nome nem e-mail). `MapInboundClaims = false`, `RoleClaimType = "role"`, `ClockSkew` de 30 s. Chave só por variável de ambiente (`Jwt__Secret`, mínimo de 32 caracteres) |
| Senha | BCrypt fator 12, política de 8 a 64 caracteres com maiúscula, minúscula, número e símbolo. Login com e-mail inexistente compara com um hash fictício (mesmo tempo de resposta) |
| Docker | `sqlserver` (healthcheck) e `api` (Development: cria o banco, aplica as migrations e cria o Admin de teste na subida). Rede `oficina-network`, volume `oficina-sqlserver-data`, API na porta 8080, credenciais só de desenvolvimento no `docker-compose.yml` |
| Testes | Unitários (domínio, handlers, validators, behaviors), de contrato (OpenAPI, JSON, ProblemDetails, autorização, dado pessoal na URL) e funcionais (SQL Server real com Testcontainers e Respawn, API ponta a ponta, matriz de perfis) |

### 13.4 Estado ao fim da Etapa 5

| Tema | Como ficou |
|---|---|
| Escopo | Back-end completo da Fase 1 (OS, orçamento com versões e aprovação total ou parcial, portal do cliente, catálogo, estoque, relatórios, automações, notificações e configurações) e front-end Angular em `web/` |
| Aplicação | Um command ou query por caso de uso (MediatR), com handler e validator. Não existem classes `*Service` fora de `Domain.Catalog` (`Service` é termo do domínio). Controllers injetam só `ISender` (teste de arquitetura) |
| Transações | A aprovação do orçamento, a substituição dos itens da OS e a baixa de estoque acontecem em um único commit. Efeitos externos (e-mail, tempo real, recibo) saem depois do commit pelo Outbox |
| Eventos | `ServiceOrderOpened`, `ServiceOrderStatusChanged`, `ServiceOrderCompleted`, `QuoteApproved`, `QuoteRejected`, `QuoteSent`, `StockDeducted`, `StockBelowMinimum`, `PaymentConfirmed` e `NotificationCreated`, gravados no Outbox e entregues por `OutboxProcessor` com um escopo de DI por handler |
| Pagamento | Pix simulado (webhook com HMAC, segredo em `.env`). Cartão, WhatsApp e emissão fiscal ficam fora do escopo |
| Segurança | Segredos só no `.env` (modelo versionado em `.env.example`), User Secrets fora do Docker, segredo do 2FA cifrado com Data Protection, rate limiting no login e em todo `/api/public/**` (429 em ProblemDetails) |
| Qualidade | SonarQube Community em Docker (`docker compose --profile quality`), analisado por `node scripts/sonar/run-analysis.mjs`, com quality gate nos projetos `mecanicaos-api` e `mecanicaos-web`. O Trivy deixou de ser usado |
| Documentação DDD | `docs/ddd/` (linguagem ubíqua, rastreabilidade, PDFs e SVGs exportados dos `.drawio` de `docs/arquitetura/`) |

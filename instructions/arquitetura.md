# Arquitetura — Oficina MVP

Documento de referência da arquitetura do MVP do Tech Challenge (Pós Tech SOAT, Fase 1). A fonte da verdade dos requisitos é `docs/enunciado/tech-challenge-fase1.pdf`; fluxos, rotas e modelo de dados estão em `docs/arquitetura/`.

As convenções de código, testes, Git e CI estão em [padrao-desenvolvimento.md](padrao-desenvolvimento.md).

---

## 1. Decisões

| Tema | Decisão |
|---|---|
| Arquitetura | Monólito em camadas (Clean Architecture), 4 projetos em `src/` e 3 de teste. CQRS com **MediatR 12.5.0** (última versão Apache-2.0) |
| Domínio | Rico, sem classes anêmicas. As regras ficam em agregados, VOs e domain services |
| Validação | Entrada com FluentValidation (pipeline behavior do MediatR). Regras de negócio no domínio |
| Idioma | Código, rotas, banco e testes em inglês. Commits, docs e mensagens ao usuário (catálogos) em português. Zero comentários no código |
| Mapeamento | Riok.Mapperly. Não existe pasta `Api/Mappers` |
| Injeção de dependência | `AddDomain`, `AddApplication`, `AddInfrastructure` e `AddPresentation`, todos em `Api/Extensions` |
| Banco | SQL Server 2022. Schema por EF Core Migrations, nomes em camelCase, soft delete e auditoria (`createdAt`, `updatedAt`, `deletedAt`) em todas as entidades |
| Swagger | Documentado por metadados de endpoint (`[EndpointSummary]`, `[EndpointDescription]`, `[Tags]`, `[ProducesResponseType]`), disponível só em Development. Snapshot do OpenAPI versionado |
| Testes | xUnit v3 (Microsoft.Testing.Platform) com Assert puro e NSubstitute. Unitários, de contrato e funcionais (Testcontainers MsSql). Cobertura mínima de 85% sobre `src/` |
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

> Atualizado na Etapa 2: solution com **7 projetos**, organização por contexto e por tipo de artefato. `Oficina.IntegrationTests` deixou de existir: os testes de persistência ficam em `Oficina.FunctionalTests/Persistence/`.

```
mecanicaOS/
├─ src/
│  ├─ Oficina.Api/                 (01 - apresentação)
│  │  ├─ Authorization/  Contracts/<Context>/  ExceptionHandling/  Extensions/
│  │  ├─ Controllers/Admin/  Controllers/Public/  Controllers/Auth/
│  │  └─ Program.cs
│  ├─ Oficina.Application/         (02 - casos de uso / CQRS)
│  │  ├─ Abstractions/   Persistence/ (IUnitOfWork), Security/ (ICurrentUser, IPasswordHasher,
│  │  │                  ITokenGenerator, IApprovalTokenGenerator), Notifications/ (INotifier),
│  │  │                  Messaging/ (IDomainEventHandler<T>)
│  │  ├─ Common/         Behaviors/, Pagination/, Validation/
│  │  └─ <Context>/      Commands/<Case>/ (Command, Handler, Validator),
│  │                     Queries/<Case>/ (Query, Handler, Validator), Queries/I<Context>Queries,
│  │                     Dtos/, Mappers/, Messages/, EventHandlers/
│  ├─ Oficina.Domain/              (03 - domínio)
│  │  ├─ Common/         Entities/, ValueObjects/, Results/, Events/, Interfaces/, Errors/
│  │  └─ <Context>/      ServiceOrders | Customers | Catalog | Inventory | Identity
│  │     └─ Entities/  ValueObjects/  Enums/  Events/  Errors/  Services/  Repositories/ (só interfaces)
│  └─ Oficina.Infrastructure/      (04 - persistência, segurança, integrações)
│     ├─ Persistence/    DbContext, UnitOfWork, Interceptors/, Configurations/<Context>/,
│     │                  Migrations/, Errors/
│     ├─ Repositories/<Context>/   Queries/<Context>/   Messaging/Outbox/
│     └─ Security/   Notifications/
├─ tests/
│  ├─ Oficina.ContractTests/       (05 - contrato da API: OpenAPI, JSON, ProblemDetails, autorização)
│  ├─ Oficina.FunctionalTests/     (06 - fluxos ponta a ponta e persistência real, Testcontainers MsSql)
│  └─ Oficina.UnitTests/           (07 - domínio e application, TDD)
│     (os testes espelham a estrutura de src/, com Builders/ e Fixtures/ compartilhados)
├─ docs/           enunciado/, arquitetura/, ddd/, evidencias/, seguranca/
├─ instructions/   arquitetura.md, padrao-desenvolvimento.md
├─ docker/sql/     init-database.sql
├─ .github/        workflows/ (ci.yml, cd.yml), scripts/, pull_request_template.md
├─ .config/dotnet-tools.json
├─ Dockerfile  docker-compose.yml  .editorconfig  .gitattributes  .gitignore  .dockerignore
└─ BannedSymbols.txt  global.json  nuget.config  Directory.Build.props  Directory.Packages.props
   Oficina.slnx  README.md
```

Um tipo por arquivo, nome do arquivo igual ao do tipo, namespace igual ao caminho da pasta. As pastas só são criadas junto com o primeiro arquivo. Não se usa `.gitkeep`.

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
- A configuração usa Options pattern com `ValidateOnStart`, no formato `Jwt__Secret`. Em Development, os valores ficam no `appsettings.Development.json`.

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
6. Dockerfile e docker-compose (API + SQL Server com healthcheck).
7. Infraestrutura de testes (factory, Testcontainers, Respawn) com um teste de fumaça por projeto.
8. `ci.yml` completo.

Depois disso, cada contexto entra seguindo os épicos E3 a E7.

## 12. Fora do escopo do MVP

- Orçamento complementar durante `InProgress` e rejeição com encerramento da OS.
- Ajuste de inventário e rota separada de histórico de status (o histórico já vem no detalhe da OS).
- Rate limiting nas rotas públicas, que só entra se sobrar tempo.

O acesso de demonstração será feito com o admin inserido manualmente no banco. O README explica como gerar o hash BCrypt.

## 13. Mudanças da Etapa 3

Consolida também as decisões da Etapa 2. Esta seção prevalece sobre o texto anterior deste documento, onde houver diferença.

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

## 14. Estado ao fim da Etapa 3

O que está implementado e testado no código. Onde houver diferença, esta seção prevalece sobre as anteriores.

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

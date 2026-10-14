# Oficina — Sistema Integrado de Atendimento e Execução de Serviços

[![CI](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/ci.yml/badge.svg)](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/ci.yml)
[![CD](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/cd.yml/badge.svg)](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/cd.yml)

Back-end de uma oficina mecânica, feito no Tech Challenge da Pós Tech SOAT (Software Architecture), Fase 1.

Hoje o projeto tem:
- o domínio completo (clientes, veículos, serviços, peças, estoque e ordem de serviço);
- as 12 tabelas criadas por migrations;
- a API de login e de cadastro de clientes, veículos e usuários, com JWT e perfis.

Ele está pronto para o grupo criar as demais APIs.

**Stack:** .NET 10, ASP.NET Core (Controllers), SQL Server 2022, EF Core 10 (migrations), MediatR 12.5.0, FluentValidation, Mapperly, JWT + BCrypt, Swagger, xUnit, NSubstitute e Testcontainers.

---

## Passo a passo

### 1. Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Ferramentas do repositório (`dotnet-ef` e ReportGenerator). Rode uma vez, na pasta do projeto:

  ```bash
  dotnet tool restore
  ```

### 2. Rodar tudo no Docker

```bash
docker compose up -d --build
```

Abra `http://localhost:8080/swagger`.

O compose sobe dois serviços:

| Serviço | O que faz |
|---|---|
| `sqlserver` | SQL Server 2022 na porta 1433, com os dados no volume `oficina-sqlserver-data` |
| `api` | A API em `http://localhost:8080`, no ambiente Development |

Na subida, a API cria o banco `oficina`, aplica as migrations e cria o Admin de teste.

Comandos úteis:

```bash
docker compose logs -f api   # logs da API
docker compose down          # para tudo (mantém os dados)
docker compose down -v       # para tudo e apaga o banco (na próxima subida ele é recriado)
```

### 3. Rodar a API pelo Visual Studio ou `dotnet run`

Com o banco do Docker:

```bash
docker compose up -d sqlserver
dotnet run --project src/Oficina.Api
```

A API sobe em `http://localhost:5080` (Swagger em `http://localhost:5080/swagger`).

No Visual Studio (2026 ou 2022 17.14+, com o SDK do .NET 10), abra `Oficina.slnx`, defina `Oficina.Api` como projeto de inicialização e aperte **F5**. O perfil padrão `http` abre o navegador direto no Swagger. Também existe o perfil `https`, em `https://localhost:7080`, que usa o certificado de desenvolvimento do .NET (`dotnet dev-certs https --trust`). Na subida, a API aplica as migrations e cria o Admin de teste.

Se o Visual Studio mostrar erros que o `dotnet build` não mostra, feche a solution, apague as pastas `.vs`, `bin` e `obj` e abra de novo.

A conexão fica em `src/Oficina.Api/appsettings.Development.json`. Para usar um **SQL Server local com autenticação do Windows** em vez do Docker, troque a `ConnectionStrings:Oficina` por:

```text
Server=.\SQLEXPRESS;Database=oficina;Trusted_Connection=True;TrustServerCertificate=True
```

### 4. Migrations

Em Development, as migrations são aplicadas automaticamente na subida da API (`Database:ApplyMigrationsOnStartup = true`).

Para aplicar manualmente:

```bash
dotnet ef database update --project src/Oficina.Infrastructure --startup-project src/Oficina.Api
```

Para criar uma nova migration depois de mudar uma entidade ou uma configuração do EF:

```bash
dotnet ef migrations add NomeDaMigration --project src/Oficina.Infrastructure --startup-project src/Oficina.Api --output-dir Persistence/Migrations
```

### 5. Login no Swagger

1. Em `POST /api/auth/login`, clique em "Try it out" e envie:

   ```json
   { "email": "admin@oficina.local", "password": "Admin@123" }
   ```

2. Copie o `accessToken` da resposta.
3. Clique em **Authorize** (topo da página), cole o token e confirme.
4. Chame `GET /api/auth/me` e as rotas de clientes e usuários.

### 6. Rodar os testes

O Docker precisa estar aberto: os testes funcionais sobem um SQL Server temporário com Testcontainers. Com o Docker fechado, eles falham com a mensagem "Os testes funcionais precisam do Docker em execução…". Os testes unitários e de contrato não precisam do Docker. Todos aparecem e rodam também no Test Explorer do Visual Studio.

```bash
dotnet test --project tests/Oficina.UnitTests
dotnet test --project tests/Oficina.ContractTests
dotnet test --project tests/Oficina.FunctionalTests
```

O CI roda os três projetos, gera o relatório de cobertura com o ReportGenerator e falha se a cobertura de linhas ficar abaixo de 85%. O resumo da cobertura aparece no resumo do job e como comentário no PR. O relatório em HTML fica como artefato do job.

Depois de mudar uma rota ou um DTO, atualize o snapshot do OpenAPI (`tests/Oficina.ContractTests/OpenApi/Snapshots/openapi.json`). No PowerShell, use `$env:UPDATE_OPENAPI_SNAPSHOT=1` antes do comando.

```bash
UPDATE_OPENAPI_SNAPSHOT=1 dotnet test --project tests/Oficina.ContractTests
```

### 7. Conectar no SSMS

- Servidor: `localhost,1433`
- Autenticação: SQL Server, usuário `sa`, senha `Oficina@Dev123`
- Marque "Confiar no certificado do servidor"
- Banco: `oficina`

---

## Credenciais (somente desenvolvimento)

Estes valores estão no `docker-compose.yml` e no `appsettings.Development.json` só para facilitar o desenvolvimento local. **Não use em produção.**

| O quê | Valor |
|---|---|
| SQL Server (`sa`) | `Oficina@Dev123` |
| Admin de teste (criado na subida em Development, se ainda não existir) | `admin@oficina.local` / `Admin@123` |
| Chave do JWT | `Jwt:Secret` no `appsettings.Development.json` |

Fora de Development, a API não cria o Admin de teste e não publica o Swagger. A chave do JWT deve vir da variável de ambiente `Jwt__Secret`, com no mínimo 32 caracteres.

---

## Endpoints

| Método e rota | Perfis | Sucesso |
|---|---|---|
| `POST /api/auth/login` | Anônimo (5 tentativas por minuto por IP; a 6ª recebe 429) | 200 com `accessToken` e `expiresAt` |
| `GET /api/auth/me` | Qualquer usuário logado | 200 com `id`, `name`, `email` e `role` |
| `GET /api/admin/customers?page=1&pageSize=20` | Admin, Attendant | 200 com `{ items, page, pageSize, totalItems }` |
| `POST /api/admin/customers` | Admin, Attendant | 201 |
| `GET /api/admin/customers/{id}` | Admin, Attendant | 200 |
| `PUT /api/admin/customers/{id}` | Admin, Attendant | 200 |
| `POST /api/admin/customers/search` | Admin, Attendant | 200 (CPF/CNPJ no corpo) |
| `DELETE /api/admin/customers/{id}` | Admin | 204 |
| `GET /api/admin/vehicles?page=1&pageSize=20&plate=` | Admin, Attendant, Mechanic | 200 com `{ items, page, pageSize, totalItems }` (`plate` filtra pela placa exata) |
| `POST /api/admin/vehicles` | Admin, Attendant | 201 |
| `GET /api/admin/vehicles/{id}` | Admin, Attendant, Mechanic | 200 com dono (id e nome) e histórico de ordens de serviço |
| `PUT /api/admin/vehicles/{id}` | Admin, Attendant | 200 |
| `DELETE /api/admin/vehicles/{id}` | Admin | 204 |
| `GET /api/admin/users` | Admin | 200 |
| `POST /api/admin/users` | Admin | 201 |
| `GET /api/admin/users/{id}` | Admin | 200 |
| `PUT /api/admin/users/{id}` | Admin | 200 |
| `DELETE /api/admin/users/{id}` | Admin | 204 |
| `POST /api/admin/users/{id}/deactivate` | Admin | 204 |
| `POST /api/admin/users/{id}/reactivate` | Admin | 204 |
| `POST /api/admin/users/search` | Admin | 200 (e-mail no corpo) |
| `GET /health` | Anônimo | 200 `Healthy` |

Os erros seguem o ProblemDetails (`application/problem+json`), com `title`, `status`, `detail` (em português), `code`, `traceId` e, na validação, `errors` com cada campo. Status usados: 400 (dados inválidos), 401 (sem login), 403 (perfil sem permissão), 404, 409 (conflito), 422 (regra de negócio), 429 e 500.

---

## Estrutura

```text
src/
  Oficina.Domain/          entidades, value objects, regras, eventos e interfaces dos repositórios
  Oficina.Application/     casos de uso (commands e queries do MediatR), validators, DTOs e mappers
  Oficina.Infrastructure/  EF Core (DbContext, configurações, migrations), repositórios, consultas, JWT, BCrypt e Outbox
  Oficina.Api/             controllers, autenticação e autorização, ProblemDetails e Swagger
tests/
  Oficina.UnitTests/        domínio e casos de uso
  Oficina.ContractTests/    contrato da API (OpenAPI, JSON, erros, autorização)
  Oficina.FunctionalTests/  API e banco de ponta a ponta (SQL Server com Testcontainers)
instructions/               convenções do grupo (arquitetura e padrão de desenvolvimento)
```

## Padrões

- **Camadas:** a API chama a Application pelo MediatR. A Application usa o domínio e as interfaces, e a Infrastructure implementa as interfaces. O domínio não depende de ninguém.
- **CQRS:** um command ou query por caso de uso, cada um com validator (FluentValidation) e handler.
- **Result pattern:** erros esperados voltam como `Result` com um `Error` dos catálogos (`…Errors`), com código estável e mensagem em português. Exceção só para erro inesperado.
- **Soft delete e auditoria:** "excluir" preenche `deletedAt`. As colunas `createdAt` e `updatedAt` são preenchidas por um interceptor do EF Core. As consultas ignoram os registros excluídos.
- **Nomes:** código, rotas, JSON, tabelas e colunas em inglês e camelCase; mensagens ao usuário e README em português.
- **Dado pessoal nunca na URL:** buscas por CPF/CNPJ ou e-mail usam `POST …/search` com o dado no corpo.
- **Sem comentários no código:** os nomes explicam o código, e as explicações ficam neste README e em `instructions/`.

Detalhes das convenções: [instructions/arquitetura.md](instructions/arquitetura.md) e [instructions/padrao-desenvolvimento.md](instructions/padrao-desenvolvimento.md).

## Próximos passos (para o grupo)

O domínio e as tabelas já existem. Faltam as APIs de:
- serviços;
- peças e estoque;
- ordens de serviço (abertura, diagnóstico, orçamento, aprovação, execução, finalização e entrega);
- consulta pública da OS pelo cliente;
- métricas.

Siga o padrão de Customers, Vehicles e Users: command ou query, validator e handler na Application; controller fino na API; testes nos três projetos.

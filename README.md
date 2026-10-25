# mecanicaOS — Sistema Integrado de Atendimento e Execução de Serviços

[![CI](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/ci.yml/badge.svg)](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/ci.yml)
[![CD](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/cd.yml/badge.svg)](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/cd.yml)

Tech Challenge da Pós Tech SOAT (Software Architecture) da FIAP, Fase 1. Back-end monolítico em .NET 10 com DDD e front-end em Angular 22, integrados de ponta a ponta.

| Link | O que é |
|---|---|
| [docs/ddd/](docs/ddd/) ([no GitHub](https://github.com/fiap-pos-challenge/mecanicaOS/tree/main/docs/ddd)) | Documentação DDD: Event Storming, diagramas, linguagem ubíqua e rastreabilidade com o código |
| [docs/arquitetura/](docs/arquitetura/) | Fontes `.drawio` do C4, do Event Storming, do DDD e do Domain Storytelling |
| [docs/seguranca/](docs/seguranca/) | Relatório do SonarQube e análise de vulnerabilidades das dependências |
| [docs/qualidade/cobertura.md](docs/qualidade/cobertura.md) | Relatório de cobertura dos testes |
| [docs/entrega/checklist-fiap.md](docs/entrega/checklist-fiap.md) | Cada item do enunciado e onde ele está no repositório |
| [docs/roteiro-de-testes.md](docs/roteiro-de-testes.md) | Roteiro para testar tudo pelo navegador |
| [tests/Oficina.ContractTests/OpenApi/Snapshots/openapi.json](tests/Oficina.ContractTests/OpenApi/Snapshots/openapi.json) | Contrato OpenAPI completo (abre em qualquer visualizador OpenAPI) |
| [Vídeo da entrega](#) | `[link do vídeo]` (YouTube, público) |

---

## Sumário

1. [Objetivo](#1-objetivo)
2. [Solução](#2-solução)
3. [Arquitetura](#3-arquitetura)
4. [Tecnologias](#4-tecnologias)
5. [Pré-requisitos](#5-pré-requisitos)
6. [Como executar](#6-como-executar)
7. [Usuários e autenticação](#7-usuários-e-autenticação)
8. [Swagger e documentação da API](#8-swagger-e-documentação-da-api)
9. [Build e testes](#9-build-e-testes)
10. [Qualidade e vulnerabilidades (SonarQube)](#10-qualidade-e-vulnerabilidades-sonarqube)
11. [Banco de dados: decisão e justificativa](#11-banco-de-dados-decisão-e-justificativa)
12. [Requisitos do enunciado e onde estão](#12-requisitos-do-enunciado-e-onde-estão)
13. [Decisões de domínio e limitações](#13-decisões-de-domínio-e-limitações)
14. [Estrutura de diretórios](#14-estrutura-de-diretórios)
15. [Desenvolvimento local (IDE, migrations, User Secrets)](#15-desenvolvimento-local-ide-migrations-user-secrets)
16. [Configuração](#16-configuração)
17. [Segurança e LGPD](#17-segurança-e-lgpd)
18. [Funcionalidades e perfis](#18-funcionalidades-e-perfis)

---

## 1. Objetivo

Uma oficina mecânica de médio porte faz o atendimento, o diagnóstico, a execução e a entrega dos veículos com anotações e planilhas. Isso gera erro na priorização, falha no controle de peças, dificuldade para acompanhar o status, perda de histórico e um fluxo de orçamento lento.

O objetivo é entregar o MVP do **Sistema Integrado de Atendimento e Execução de Serviços**: gestão de ordens de serviço (OS), clientes, veículos, serviços, peças e estoque, com orçamento enviado ao cliente para aprovação, acompanhamento pelo cliente via API, autenticação JWT nas rotas administrativas e boas práticas de qualidade e segurança.

## 2. Solução

- O atendente identifica o cliente pelo **CPF ou CNPJ** (validados), cadastra o **veículo** (placa antiga ou Mercosul, marca, modelo e ano) e abre a **OS**.
- O mecânico faz o diagnóstico e inclui **serviços, peças e insumos**. O **orçamento é calculado automaticamente** a partir do catálogo, com descontos e limites.
- O orçamento é **enviado ao cliente** por link (portal) ou e-mail. O cliente **aprova** (total ou parcialmente) ou **recusa** no portal, ou o atendente registra a decisão no balcão.
- A aprovação muda a OS para **Em execução** e **baixa o estoque** das peças na **mesma transação**. Se faltar saldo, nada é gravado.
- A OS segue os status **Recebida → Em diagnóstico → Aguardando aprovação → Em execução → Finalizada → Entregue**, com regras de transição no domínio e mudança automática conforme as ações.
- O cliente **acompanha a OS pela API pública** (número da OS e CPF/CNPJ no corpo da requisição, ou link do portal).
- A gestão tem **CRUD de clientes, veículos, serviços e peças**, **controle de estoque** (entradas, baixas, inventário e alerta de mínimo), **listagem e detalhe de OS** e o **tempo médio de execução** dos serviços.

## 3. Arquitetura

**Monólito em camadas** (Clean Architecture) com **DDD**, **CQRS** (MediatR) e **Outbox** para os efeitos depois do commit.

```text
Oficina.Api ──► Oficina.Application ──► Oficina.Domain
     │                                        ▲
     └──► Oficina.Infrastructure ─────────────┘ (também implementa as interfaces da Application)
```

| Camada | Responsabilidade |
|---|---|
| `Oficina.Domain` | Agregados (`ServiceOrder`, `Estimate`, `Customer`, `Vehicle`, `Service`, `Part`…), value objects (`Document`, `LicensePlate`, `Money`, `Quantity`…), eventos de domínio, políticas (`QuoteApprovalPolicy`, `EstimatePricing`) e interfaces de repositório. Não depende de nenhum pacote |
| `Oficina.Application` | Um command ou query por caso de uso (MediatR), cada um com handler e validator (FluentValidation), DTOs e handlers de eventos de domínio |
| `Oficina.Infrastructure` | EF Core (SQL Server), migrations, repositórios, consultas, JWT, BCrypt, e-mail, Pix simulado, Outbox e serviços em segundo plano |
| `Oficina.Api` | Controllers (só enviam commands e queries pelo `ISender`), autenticação, policies por perfil, rate limiting, ProblemDetails, Swagger e hub SignalR |

- **Erros esperados** voltam como `Result` com um código estável (por exemplo, `serviceOrders.invalidTransition`) e viram ProblemDetails em português. Exceção só para o inesperado.
- **Transações:** cada command termina em um único `CommitAsync`. A aprovação do orçamento substitui os itens da OS, muda o status e baixa o estoque no mesmo commit. Concorrência otimista com `rowversion` em `ServiceOrder`, `Part` e `Estimate`.
- **Eventos de domínio** (`QuoteApproved`, `StockDeducted`, `StockBelowMinimum`, `PaymentConfirmed`…) são gravados no **Outbox** na mesma transação e processados em segundo plano: notificações, tempo real, recibos e automações.
- **Testes de arquitetura** garantem as dependências entre camadas, controllers só com `ISender`, um tipo por arquivo e zero comentários.
- No navegador, o nginx serve o front e repassa `/api` (inclusive o WebSocket do SignalR) para a API.

Diagramas: [C4 (contexto, containers e componentes)](docs/arquitetura/C4_Oficina.drawio), [Event Storming](docs/ddd/event-storming.pdf), [subdomínios, mapa de contextos, modelo de domínio e Domain Storytelling](docs/ddd/diagramas.pdf). Índice completo em [docs/ddd/README.md](docs/ddd/README.md).

## 4. Tecnologias

| Área | Tecnologias |
|---|---|
| Linguagem e back-end | C# 14, .NET 10 (SDK 10.0.100), ASP.NET Core com Controllers, MediatR 12.5, FluentValidation 12, Mapperly, JWT Bearer + BCrypt, Data Protection, SignalR, Swashbuckle, QRCoder |
| Banco | SQL Server 2022, EF Core 10 com migrations |
| Testes do back-end | xUnit v3 (Microsoft Testing Platform), NSubstitute, Testcontainers (SQL Server real), Respawn, coverlet e ReportGenerator |
| Front-end | Angular 22.2 (standalone, signals, zoneless, `OnPush`), TypeScript 6 estrito, NgRx Signals, Angular CDK, ECharts, PWA, nginx |
| Testes do front-end | Vitest + Angular Testing Library, MSW (só nos testes), axe, Playwright e Lighthouse |
| Qualidade e segurança | SonarQube Community (Docker), `dotnet list package --vulnerable`, `npm audit`, varredura de segredos |
| Infraestrutura | Docker (Dockerfile multi-stage), Docker Compose, GitHub Actions (CI e CD) |

## 5. Pré-requisitos

| Ferramenta | Para quê |
|---|---|
| [Git](https://git-scm.com/) | Baixar o repositório |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) **aberto e rodando** | Executar tudo, os testes funcionais, o E2E e o SonarQube |
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | Só para desenvolver e testar o back-end fora do Docker |
| [Node.js 24.15 ou mais novo](https://nodejs.org/) | Só para desenvolver e testar o front-end, gerar diagramas e rodar a análise do SonarQube |

Para só executar o sistema, bastam Git e Docker Desktop.

## 6. Como executar

```bash
git clone https://github.com/fiap-pos-challenge/mecanicaOS.git
cd mecanicaOS
cp .env.example .env
docker compose up -d --build --wait
```

No Windows (cmd ou PowerShell), troque `cp` por `copy .env.example .env`.

O `.env` guarda as senhas e chaves **só de desenvolvimento** e nunca vai para o Git. Os valores do `.env.example` já funcionam; troque-os se a máquina for compartilhada. Sem o `.env`, o `docker compose` para com a mensagem `defina ... no .env`.

Quando os contêineres ficarem saudáveis, abra:

| Endereço | O que é |
|---|---|
| http://localhost:4200 | Sistema (front-end) |
| http://localhost:8080/swagger | Swagger da API |
| http://localhost:8080/health | Saúde da API |

O compose sobe quatro serviços:

| Serviço | O que faz |
|---|---|
| `sqlserver` | SQL Server 2022 na porta 1433 (dados no volume `oficina-sqlserver-data`) |
| `api` | API em Development na porta 8080. Na subida cria o banco, aplica as migrations e cria o Admin de teste |
| `web` | Front-end de produção servido pelo nginx na porta 4200 |
| `seed` | Roda uma vez e preenche o banco vazio como uma oficina em uso (seção 7) |

### 6.1 Comandos do dia a dia (na raiz do repositório)

```bash
docker compose ps              # situação dos serviços
docker compose logs seed       # usuários criados e link do portal do cliente
docker compose logs -f api     # logs da API
docker compose restart api     # reinicia só a API
docker compose stop            # para tudo sem apagar nada
docker compose down            # remove os contêineres e mantém os dados
```

E-mails em desenvolvimento não saem da máquina: cada mensagem vira um arquivo `.eml` na pasta `.mail/` da raiz.

### 6.2 Apagar e recriar contêineres e imagens

Todas as imagens e contêineres do projeto começam com `oficina`. Rode na raiz do repositório.

| O que apagar | Comando |
|---|---|
| Aplicação: contêineres, rede, **banco** (volumes) e imagens `oficina-api:local` e `oficina-web:local` | `docker compose down -v --rmi local` |
| Modo demonstração (`oficina-web-demo` e a imagem `oficina-web:demo`) | `docker compose --profile demo down --rmi local` |
| SonarQube: contêiner e volumes (análises guardadas) | `docker compose --profile quality down -v` |
| Stack descartável do E2E, se uma execução foi interrompida | `docker compose --env-file .env -f web/e2e/docker-compose.e2e.yml down -v --rmi local` |
| Imagens reaproveitadas pelo E2E e pelo Lighthouse | `docker image rm oficina-web:e2e oficina-web:e2e-demo oficina-api:e2e oficina-web:lighthouse` |
| Imagem do SonarQube (cerca de 2,5 GB; só se quiser baixar de novo) | `docker image rm sonarqube:26.9.0.129388-community` |
| Contêineres que sobraram dos testes funcionais (Testcontainers) | `docker ps -a --filter label=org.testcontainers=true -q` e depois `docker rm -f <ids>` |
| Cache de build do Docker | `docker builder prune -f` |

O `-v` apaga o banco de desenvolvimento. Não há como desfazer, mas a próxima subida recria o banco e a carga de demonstração.

Para recriar do zero, sem reaproveitar nada do build anterior:

```bash
docker compose down -v --rmi local
docker compose build --no-cache
docker compose up -d --wait
docker compose ps
```

Para reconstruir só uma imagem depois de mudar o código: `docker compose up -d --build --wait api` (ou `web`).

As imagens dos testes não precisam ser criadas à mão: o `npm run e2e` e o `npm run lighthouse` constroem as suas, e os testes funcionais sobem e removem o próprio SQL Server (seção 9).

## 7. Usuários e autenticação

As senhas ficam no seu `.env` (valores iniciais no [.env.example](.env.example)):

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador (criado pela API na subida em Development) | `DEV_ADMIN_EMAIL` (`admin@oficina.local`) | `DEV_ADMIN_PASSWORD` |
| Gerente | `gerente@oficina.local` | `SEED_USERS_PASSWORD` |
| Atendentes | `ana.atendente@oficina.local`, `bruno.atendente@oficina.local` | `SEED_USERS_PASSWORD` |
| Mecânicos | `marcos.mecanico@oficina.local`, `paula.mecanica@oficina.local`, `rafael.mecanico@oficina.local` | `SEED_USERS_PASSWORD` |
| Cliente | Sem login: link do portal (`docker compose logs seed`) ou consulta pública pelo número da OS e CPF/CNPJ | — |

**Como autenticar nas APIs administrativas:**

1. Chame `POST /api/auth/login` com `{ "email": "admin@oficina.local", "password": "<DEV_ADMIN_PASSWORD do .env>" }`.
2. A resposta traz o `accessToken` (JWT HS256, validade curta).
3. Envie o token em toda chamada a `/api/admin/**` no cabeçalho `Authorization: Bearer <accessToken>`.
4. Sem token, a API responde 401. Com perfil sem permissão, 403. O login aceita 5 tentativas por minuto por IP (429 depois disso), e 5 senhas erradas seguidas bloqueiam a conta por 15 minutos.

A carga de demonstração (`scripts/seed/seed-oficina.mjs`) usa só a API, como um usuário faria, e monta a oficina **Auto Center Paulista**: equipe de todos os perfis, configurações, 11 serviços, 14 peças (duas abaixo do mínimo), 3 pacotes, fornecedores, 10 clientes, 12 veículos, OS em todas as etapas, documentos, notificações, automações e relatórios. O banco só é preenchido se não tiver clientes.

## 8. Swagger e documentação da API

- **Swagger UI:** http://localhost:8080/swagger (só em Development, que é o modo do `docker compose`). Clique em **Authorize**, cole o `accessToken` do login e chame qualquer rota.
- **Contrato estático:** [openapi.json](tests/Oficina.ContractTests/OpenApi/Snapshots/openapi.json), versionado e conferido por teste de contrato. Toda operação declara resumo, tipos de resposta e status possíveis.
- **Formato:** JSON em camelCase, enums como string, datas ISO 8601 UTC, paginação `{ items, page, pageSize, totalItems }` e erros em ProblemDetails com `code`, `traceId` e `errors`.

## 9. Build e testes

Os comandos do back-end rodam na **raiz do repositório**. Os do front-end rodam dentro da pasta **`web/`**. Funcionam no Git Bash, no PowerShell e no terminal do macOS/Linux, exceto onde está indicado.

### 9.1 Back-end (raiz do repositório)

```bash
dotnet tool restore                                   # ferramentas locais (dotnet-ef, reportgenerator, sonarscanner)
dotnet restore Oficina.slnx
dotnet build Oficina.slnx -c Release -warnaserror     # mesmo build do CI: nenhum aviso é aceito
dotnet test --solution Oficina.slnx -c Release --no-build
```

Para rodar um projeto de teste por vez:

```bash
dotnet test --project tests/Oficina.UnitTests -c Release --no-build
dotnet test --project tests/Oficina.ContractTests -c Release --no-build
dotnet test --project tests/Oficina.FunctionalTests -c Release --no-build
```

**Os testes funcionais precisam do Docker Desktop aberto.** O Testcontainers sobe um SQL Server descartável (imagem `mcr.microsoft.com/mssql/server:2022-latest`), aplica as migrations e remove o contêiner no fim. Sem Docker, os testes falham na inicialização com a mensagem "Os testes funcionais precisam do Docker em execução". A primeira execução demora mais, porque baixa a imagem do SQL Server (cerca de 2,3 GB). Os testes unitários e de contrato não usam Docker.

| Camada | Projeto | O que cobre |
|---|---|---|
| Unidade | `tests/Oficina.UnitTests` | Agregados, value objects, políticas e regras do domínio; handlers e validators; regras de arquitetura |
| Contrato | `tests/Oficina.ContractTests` | Snapshot OpenAPI, autorização por rota, rotas anônimas permitidas, nenhum dado pessoal na URL, dependências entre camadas |
| Funcional | `tests/Oficina.FunctionalTests` | API e banco de ponta a ponta (fluxo da OS, orçamento, portal, Pix, estoque, permissões, migrations, outbox e concorrência) |
| Front-end | `web/src/**/*.spec.ts` | Componentes, páginas, stores, gateways, validadores e acessibilidade |
| E2E | `web/e2e/*.spec.ts` | Navegador real (desktop e celular) contra a imagem de produção e a API real |

### 9.2 Cobertura do back-end

Mesmo gate do CI: mínimo de **85% de linhas** sobre `src/` (o enunciado pede 80% nos domínios críticos). Apague os resultados anteriores para não somar execuções antigas:

```bash
rm -rf TestResults coveragereport
```

No PowerShell: `Remove-Item -Recurse -Force TestResults, coveragereport -ErrorAction SilentlyContinue`.

```bash
dotnet test --project tests/Oficina.UnitTests -c Release --no-build --results-directory TestResults/Unit --coverlet --coverlet-output-format cobertura
dotnet test --project tests/Oficina.ContractTests -c Release --no-build --results-directory TestResults/Contract --coverlet --coverlet-output-format cobertura
dotnet test --project tests/Oficina.FunctionalTests -c Release --no-build --results-directory TestResults/Functional --coverlet --coverlet-output-format cobertura
dotnet reportgenerator "-reports:TestResults/**/*.cobertura.*.xml" "-targetdir:coveragereport" "-reporttypes:Html;TextSummary;JsonSummary" "-assemblyfilters:+Oficina.Api;+Oficina.Application;+Oficina.Domain;+Oficina.Infrastructure" "-filefilters:-**/Program.cs;-**/Migrations/**"
```

O relatório abre em `coveragereport/index.html`. Depois de rodar também o `npm test` do front-end (9.3), `node scripts/docs/coverage-report.mjs` (na raiz) grava o resumo em [docs/qualidade/cobertura.md](docs/qualidade/cobertura.md) (total, por projeto, domínios críticos e front-end) e falha se algum mínimo não for atingido.

### 9.3 Front-end (pasta `web/`)

```bash
cd web
npm ci                 # instala as dependências exatamente como no package-lock.json
npm run lint           # ESLint, zero comentários, cores dos tokens e Prettier
npm test               # Vitest com cobertura (mínimo de 85% de linhas)
npm run build          # build de produção em web/dist/
npm run check:bundle   # garante que a API simulada não entrou no build de produção
npm run check:secrets  # procura segredos nos arquivos versionados
npm audit              # vulnerabilidades das dependências npm
```

Testes no navegador (precisam do Docker Desktop aberto e do `.env` na raiz):

```bash
cd web
npx playwright install chromium   # só na primeira vez
npm run e2e                       # Playwright (desktop e celular) + axe
npm run lighthouse                # acessibilidade do login e da tela de nova versão (mínimo 95)
```

O `npm run e2e` constrói as imagens `oficina-api:e2e`, `oficina-web:e2e` e `oficina-web:e2e-demo` e sobe a stack descartável `oficina-e2e` (`web/e2e/docker-compose.e2e.yml`, portas 4300 e 4301, SQL Server sem volume). No fim, remove os contêineres; as imagens ficam para acelerar a próxima execução. O banco de desenvolvimento não é tocado. O `npm run lighthouse` usa a imagem `oficina-web:lighthouse`. Como apagar essas imagens: seção 6.2.

### 9.4 Contrato da API

Depois de mudar uma rota ou um DTO, atualize o snapshot do contrato (na raiz) e os tipos do front:

```bash
UPDATE_OPENAPI_SNAPSHOT=1 dotnet test --project tests/Oficina.ContractTests
cd web && npm run api:generate
```

No PowerShell, rode `$env:UPDATE_OPENAPI_SNAPSHOT=1` antes do `dotnet test` e `Remove-Item Env:UPDATE_OPENAPI_SNAPSHOT` depois.

### 9.5 Validação completa antes de entregar

Na ordem, com o Docker Desktop aberto e o `.env` criado:

1. Back-end: `dotnet build Oficina.slnx -c Release -warnaserror` e `dotnet test --solution Oficina.slnx -c Release --no-build`.
2. Dependências do back-end: `dotnet list Oficina.slnx package --vulnerable --include-transitive`.
3. Front-end, em `web/`: `npm ci`, `npm run lint`, `npm test`, `npm run build`, `npm run check:bundle`, `npm run check:secrets`, `npm audit`, `npm run e2e` e `npm run lighthouse`.
4. SonarQube (seção 10): `node scripts/sonar/run-analysis.mjs --stop`, com o quality gate **Passed** nos dois projetos.
5. Subida do zero (seção 6.2): `docker compose down -v --rmi local` e `docker compose up -d --build --wait`; depois o [roteiro de testes](docs/roteiro-de-testes.md) no navegador.

O CI ([.github/workflows/ci.yml](.github/workflows/ci.yml)) roda os passos 1 e 3 (com o gate de cobertura de 85% nos dois lados) em todo push e pull request.

## 10. Qualidade e vulnerabilidades (SonarQube)

O SonarQube Community roda em Docker, no profile `quality` do `docker-compose.yml`, fora do ambiente da aplicação. Um único comando sobe o servidor, gera o token, roda os testes com cobertura, analisa o back-end (`dotnet-sonarscanner`, ferramenta local do repositório) e o front-end (`@sonar/scan` via `npx`) e grava o relatório. Os scanners rodam na sua máquina e baixam o Java do próprio SonarQube, então a única imagem extra é a do SonarQube. Pré-requisitos: Docker Desktop, .NET 10 SDK e Node.js.

```bash
node scripts/sonar/run-analysis.mjs          # analisa e mantém o SonarQube em http://localhost:9000
node scripts/sonar/run-analysis.mjs --stop   # analisa e para o SonarQube no fim
```

- Projetos: `mecanicaos-api` (back-end .NET) e `mecanicaos-web` (front-end Angular).
- Usuário `admin`, senha `SONAR_ADMIN_PASSWORD` do `.env`. O token de análise é gravado no `.env` sem aparecer na saída.
- A primeira execução baixa a imagem do SonarQube (cerca de 2,5 GB) e o Java dos scanners. A análise completa leva de 10 a 20 minutos, porque inclui os testes funcionais (Docker aberto).
- Como validar: abra http://localhost:9000, entre em cada projeto e confira o quality gate **Passed**, 0 bugs, 0 vulnerabilidades, notas A em confiabilidade, segurança e manutenibilidade, e os hotspots revisados. O script também falha se o quality gate não passar.
- Para parar sem apagar as análises: `docker compose --profile quality stop`. Para subir de novo só o painel: `docker compose --profile quality up -d --wait sonarqube`.
- Para remover tudo do SonarQube (contêiner e volumes): `docker compose --profile quality down -v`. A imagem `sonarqube` sai com `docker image rm sonarqube:26.9.0.129388-community`.

Resultados: [docs/seguranca/sonarqube-relatorio.md](docs/seguranca/sonarqube-relatorio.md) (quality gate, bugs, vulnerabilidades, hotspots, cobertura e duplicação) e [docs/seguranca/dependencias.md](docs/seguranca/dependencias.md) (dependências NuGet e npm sem vulnerabilidades conhecidas).

## 11. Banco de dados: decisão e justificativa

**SQL Server 2022**, relacional, acessado pelo EF Core 10 com migrations.

| Necessidade do domínio | Como o SQL Server atende |
|---|---|
| Aprovar o orçamento, mudar o status da OS e baixar o estoque juntos | Transação ACID: tudo ou nada em um único commit |
| Dois atendentes alterando a mesma OS ou o mesmo estoque | Concorrência otimista com `rowversion` (`ServiceOrder`, `Part` e `Estimate`), com resposta 409 |
| Número sequencial da OS que o cliente vê | `SEQUENCE` nativa |
| CPF/CNPJ, placa e código de peça únicos só entre registros ativos | Índices únicos filtrados (`WHERE deletedAt IS NULL`) com soft delete |
| Relatórios, painel e tempo médio de execução | Consultas SQL com agregação e paginação (`OFFSET/FETCH`) |
| Testes com banco real e ambiente simples | Imagem oficial em Docker, módulo do Testcontainers e provider oficial do EF Core |

Os dados do domínio são fortemente relacionais (cliente → veículo → OS → itens → orçamento → estoque), com integridade referencial e transações entre agregados. Por isso um banco relacional é a escolha natural, e um banco de documentos exigiria reimplementar essas garantias na aplicação.

O PostgreSQL também atenderia. O grupo escolheu o SQL Server pelo `rowversion` sem configuração extra, pela edição Developer gratuita com a mesma engine da produção, pela integração com as ferramentas .NET (EF Core, SSMS, Visual Studio) e pela familiaridade do grupo.

## 12. Requisitos do enunciado e onde estão

| Requisito | Onde está |
|---|---|
| Identificação do cliente por CPF/CNPJ | `POST /api/admin/customers/search` (documento no corpo), value object `Document` |
| Cadastro de veículo (placa, marca, modelo, ano) | `POST /api/admin/vehicles`, value object `LicensePlate` |
| Abrir OS com serviços, peças e insumos | `POST /api/admin/service-orders`, `PUT /api/admin/service-orders/{id}/diagnosis`, `POST /api/admin/service-orders/{id}/send-to-quote` |
| Orçamento gerado automaticamente | `EstimatePricing` (domínio), `POST /api/admin/quotes` e `GET /api/admin/quotes/{id}` |
| Envio do orçamento ao cliente | `POST /api/admin/quotes/{id}/send` (link do portal ou e-mail) |
| Aprovação ou recusa do orçamento | Cliente: `POST /api/public/portal/quote/approve` e `/reject`; balcão: `POST /api/admin/quotes/{id}/approve` e `/reject` |
| Status da OS e regras de transição | `ServiceOrder` (domínio), `POST /api/admin/service-orders/{id}/transitions`, `start-service`, `finish-service`; status `Received`, `InDiagnosis`, `WaitingApproval`, `InProgress`, `Completed`, `Delivered` |
| Consulta do cliente via API | `POST /api/public/service-orders/lookup` (número e CPF/CNPJ no corpo) e `GET /api/public/portal/order` |
| CRUD de clientes | `/api/admin/customers` (GET, POST, PUT, DELETE) |
| CRUD de veículos | `/api/admin/vehicles` (GET, POST, PUT, DELETE) |
| CRUD de serviços | `/api/admin/services` (GET, POST, PUT, DELETE = desativar) |
| CRUD de peças e insumos com estoque | `/api/admin/parts` (GET, POST, PUT, DELETE = desativar) e `/api/admin/inventory/*` (entradas, baixas, inventário, saldo e movimentações) |
| Listagem e detalhe de OS | `GET /api/admin/service-orders`, `POST /api/admin/service-orders/search`, `GET /api/admin/service-orders/{id}` |
| Tempo médio de execução | `GET /api/admin/metrics/average-execution-time` |
| JWT nas APIs administrativas | `FallbackPolicy` autenticada e uma policy por endpoint em `/api/admin/**` |
| Validação de CPF/CNPJ e placa | Value objects `Document` e `LicensePlate` (dígitos verificadores, placa antiga e Mercosul), com testes |
| Testes unitários e de integração | Seção 9 |
| Swagger | Seção 8 |
| Dockerfile e docker-compose | [Dockerfile](Dockerfile) e [docker-compose.yml](docker-compose.yml) |
| Análise de vulnerabilidades | Seção 10 |

A lista completa, com a evidência de cada item, está em [docs/entrega/checklist-fiap.md](docs/entrega/checklist-fiap.md).

## 13. Decisões de domínio e limitações

- **Transação com mais de um agregado:** a aprovação do orçamento altera `Estimate`, `ServiceOrder` e `Part` no mesmo commit. É uma exceção consciente à regra "um agregado por transação": o enunciado exige que a aprovação e a baixa de estoque sejam consistentes, e a política de domínio `QuoteApprovalPolicy` concentra essa regra. Efeitos externos (e-mail, recibo, tempo real) saem depois, pelo Outbox.
- **Baixa de estoque na aprovação:** as peças saem do estoque quando o orçamento é aprovado, não na abertura da OS. Se faltar saldo, a aprovação é recusada (409) e nada é gravado.
- **Tempo médio de execução:** média, em minutos, entre o início da execução (**Em execução**) e a finalização (**Finalizada**), nas OS finalizadas ou entregues cuja finalização caiu no período pedido (`from` e `to`). A resposta traz também quantas OS entraram na conta.
- **Aprovação parcial:** o cliente pode aceitar só parte dos itens. A OS passa a ter só os itens aceitos, e o total é recalculado.
- **Excluir serviço ou peça é desativar:** `DELETE` em `/services/{id}` e `/parts/{id}` desativa o registro (soft delete lógico). Ele continua nos orçamentos e OS antigos e pode ser reativado.
- **O que fica no domínio:** conceitos técnicos, como os jobs em segundo plano (`BackgroundJob`), ficam na Application. Configurações da oficina (`SettingsDocument`), ações do painel e trilha de auditoria ficam no Domain, porque guardam regras do negócio: condições de orçamento e pagamento e o registro de quem aprovou cada desconto.
- **Pix simulado:** o QR code e o copia-e-cola são gerados, mas a confirmação vem de um webhook próprio com assinatura HMAC. A integração com um PSP real fica fora do escopo.
- **Fora do escopo:** pagamento com cartão (precisa de provedor), envio real por WhatsApp (precisa de provedor) e emissão de nota fiscal.

## 14. Estrutura de diretórios

```text
src/
  Oficina.Domain/          agregados, entidades, value objects, eventos, políticas e interfaces de repositório
  Oficina.Application/     commands, queries, handlers, validators, DTOs e handlers de eventos
  Oficina.Infrastructure/  EF Core, migrations, repositórios, consultas, JWT, e-mail, Pix, Outbox e serviços em segundo plano
  Oficina.Api/             controllers, autenticação, policies, rate limiting, ProblemDetails, Swagger e hub SignalR
tests/
  Oficina.UnitTests/        domínio, aplicação e regras de arquitetura
  Oficina.ContractTests/    contrato da API e dependências entre camadas
  Oficina.FunctionalTests/  API e banco de ponta a ponta com SQL Server em contêiner
web/                        front-end Angular (features por pasta, gateways HTTP, design system próprio)
scripts/
  seed/                     carga de demonstração
  sonar/                    análise do SonarQube em Docker
  docs/                     geração e exportação dos diagramas DDD
  ci/                       .env descartável do CI
docs/
  ddd/                      Event Storming, diagramas, linguagem ubíqua e rastreabilidade
  arquitetura/              fontes .drawio (C4, Event Storming, DDD e Domain Storytelling)
  seguranca/                SonarQube e dependências
  qualidade/                cobertura
  entrega/                  checklist do enunciado
  enunciado/                enunciado do Tech Challenge
instructions/               convenções do grupo
```

## 15. Desenvolvimento local (IDE, migrations, User Secrets)

Fora do Docker, a API lê os segredos de **User Secrets** (nunca do `appsettings`). Configure uma vez, com os mesmos valores do seu `.env`:

```bash
dotnet user-secrets set "ConnectionStrings:Oficina" "Server=localhost,1433;Database=oficina;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True" --project src/Oficina.Api
dotnet user-secrets set "Jwt:Secret" "<JWT_SECRET>" --project src/Oficina.Api
dotnet user-secrets set "DevelopmentAdmin:Email" "admin@oficina.local" --project src/Oficina.Api
dotnet user-secrets set "DevelopmentAdmin:Password" "<DEV_ADMIN_PASSWORD>" --project src/Oficina.Api
dotnet user-secrets set "Payments:PixWebhookSecret" "<PIX_WEBHOOK_SECRET>" --project src/Oficina.Api
```

Depois:

```bash
docker compose up -d --wait sqlserver
dotnet run --project src/Oficina.Api
```

A API sobe em `http://localhost:5080` (Swagger em `/swagger`). No Visual Studio, abra `Oficina.slnx`, defina `Oficina.Api` como projeto de inicialização e aperte **F5**. Para SQL Server local com autenticação do Windows, use `Server=.\SQLEXPRESS;Database=oficina;Trusted_Connection=True;TrustServerCertificate=True` na `ConnectionStrings:Oficina`.

Front-end em desenvolvimento (hot reload). O `npm start` repassa `/api` para `http://localhost:8080`, então a API precisa responder nessa porta. Use a API do Docker ou rode o `dotnet run` na porta 8080:

```bash
docker compose up -d --wait sqlserver api                       # opção 1: API no Docker
dotnet run --project src/Oficina.Api --urls http://localhost:8080  # opção 2: API pela IDE ou terminal
```

Depois, em outro terminal:

```bash
cd web
npm ci
npm start             # http://localhost:4200
```

Pare o `web` do Docker antes (`docker compose stop web`), porque ele também usa a porta 4200.

Migrations (em Development rodam sozinhas na subida da API):

```bash
dotnet dotnet-ef database update --project src/Oficina.Infrastructure --startup-project src/Oficina.Api
dotnet dotnet-ef migrations add NomeDaMigration --project src/Oficina.Infrastructure --startup-project src/Oficina.Api --output-dir Persistence/Migrations
```

SSMS ou Azure Data Studio: servidor `localhost,1433`, autenticação SQL Server, usuário `sa`, senha `MSSQL_SA_PASSWORD` do `.env`, "Confiar no certificado do servidor", banco `oficina`.

## 16. Configuração

| Chave | Para que serve | Origem em desenvolvimento |
|---|---|---|
| `ConnectionStrings__Oficina` | Conexão com o SQL Server | `docker-compose.yml`, com a senha `MSSQL_SA_PASSWORD` do `.env` |
| `Jwt__Secret` | Chave do token de acesso (mínimo de 32 caracteres) | `JWT_SECRET` do `.env` |
| `DevelopmentAdmin__Email`, `DevelopmentAdmin__Password` | Admin criado na subida em Development (sem senha, nenhum usuário é criado) | `DEV_ADMIN_EMAIL` e `DEV_ADMIN_PASSWORD` do `.env` |
| `Payments__PixWebhookSecret` | Segredo da assinatura HMAC do webhook Pix simulado | `PIX_WEBHOOK_SECRET` do `.env` |
| `DataProtection__KeysPath` | Pasta das chaves que cifram o segredo do 2FA | `/keys` (volume `api-keys`) |
| `Email__PickupDirectory` | Grava e-mails como `.eml` em vez de enviar | `/mail` (pasta `.mail/`) |
| `Email__SmtpHost`, `Email__SmtpPort` | SMTP real fora do desenvolvimento (sempre com TLS) | — |
| `PublicLinks__BaseUrl` | Endereço usado nos links do portal e da verificação | `http://localhost:4200` |
| `Cors__AllowedOrigins__0` | Origem do front fora do mesmo domínio | — |
| `Security__LoginRateLimit__PermitLimit` | Tentativas de login por minuto por IP | 5 |
| `Security__PublicRateLimit__PermitLimit` | Requisições por minuto por IP em `/api/public/**` | 60 |

Fora de Development, a API não cria o Admin de teste e não publica o Swagger, e todos os segredos vêm de variáveis de ambiente. A chave Pix da oficina é configurada pela tela **Configurações > Pagamentos**.

## 17. Segurança e LGPD

- Nenhum segredo no repositório: tudo no `.env` local (modelo em `.env.example`) ou em User Secrets. O CI gera um `.env` descartável com valores aleatórios, e `npm run check:secrets` procura segredos nos arquivos versionados.
- CPF, CNPJ, e-mail, telefone e placa nunca vão na URL (um teste de contrato garante) e saem mascarados; abrir o dado exige motivo e fica auditado.
- Nada sigiloso vai para o log (senha, token, documento, e-mail, telefone, placa, corpo de requisição).
- Senhas com BCrypt; token de acesso só em memória no navegador; renovação por cookie `HttpOnly`, `SameSite=Strict`; segredo do 2FA cifrado com Data Protection.
- Rate limiting no login (5 por minuto por IP) e em todo `/api/public/**` (60 por minuto por IP), com 429 em ProblemDetails. O portal bloqueia o link após 3 erros de confirmação.
- Mudanças sensíveis pedem a senha de novo; 2FA opcional ou obrigatório; sessões podem ser encerradas.
- nginx com CSP estrita, Trusted Types e cabeçalhos de segurança.
- Webhook Pix aceito só com assinatura HMAC-SHA256.
- Exportação dos dados do titular (LGPD) pela ficha do cliente.
- O `Directory.Packages.props` fixa `System.Security.Cryptography.Pkcs` em 10.0.13, porque a versão trazida pelo `Microsoft.Data.SqlClient` tem vulnerabilidade conhecida (detalhes em [docs/seguranca/dependencias.md](docs/seguranca/dependencias.md)).

## 18. Funcionalidades e perfis

Além do escopo obrigatório, o sistema tem as telas abaixo, todas usando a API real.

| Módulo | O que faz | Tela |
|---|---|---|
| **Acesso** | Login, sessão que sobrevive à recarga, verificação em duas etapas (TOTP), bloqueio após 5 erros, inatividade com saída automática, convite de funcionário por e-mail | `/login` |
| **Painel** | Faturamento, OS abertas, taxa de aprovação, valores a receber, serviços mais vendidos, pátio por situação e avisos com ação direta | `/dashboard` |
| **Clientes e veículos** | CPF/CNPJ validado e mascarado, endereço pelo CEP, consentimento de mensagens, revelar dado com motivo, veículos com placa e quilometragem, transferência de dono, histórico, importação CSV e exportação LGPD | `/customers` |
| **Catálogo** | Serviços, peças e insumos (custo, venda, margem, estoque mínimo, EAN-13) e pacotes, com histórico de preço | `/catalog` |
| **Estoque** | Alertas de mínimo, entradas (manual, código de barras e XML da NF-e), baixas, movimentações, inventário com aprovação, fornecedores e pedidos de compra | `/inventory` |
| **Ordens de serviço** | Nova OS, quadro com arrastar e soltar, lista, agenda, detalhe com histórico, fotos, tempos e documentos, controle de versão e tempo real | `/service-orders` |
| **Modo mecânico** | Celular/PWA: minhas OS, checklist, fotos, diagnóstico, enviar para orçamento, cronômetro e finalizar; funciona sem internet | `/mechanic` |
| **Orçamentos** | Montador com pacotes, serviços e peças, descontos com aprovação, plano de pagamento, envio, aprovação no balcão, recusa com motivo e versões | `/quotes` |
| **Portal do cliente** | Link sem login, confirmação pelos 3 últimos dígitos do telefone, andamento, fotos, aprovação parcial ou recusa, Pix e documentos | `/o/{link}` |
| **Consulta pública** | Situação da OS pelo número e CPF/CNPJ | `/portal` |
| **Documentos** | OS, orçamento e recibo com código de verificação, PDF e página pública de autenticidade | `/documents` e `/v/{código}` |
| **Financeiro, relatórios, automações, notificações, equipe, configurações e plataforma** | Gestão da oficina | `/finance`, `/reports`, `/automations`, `/team`, `/settings`, `/platform` |

| Perfil | Principais permissões |
|---|---|
| **Administrador** | Tudo, inclusive equipe, configurações, plataforma, exclusões e reprocessar falhas |
| **Gerente** | Operação completa, custos, financeiro, relatórios, automações, aprovação de desconto |
| **Atendente** | Clientes, veículos, OS, orçamentos, documentos e painel (desconto até o seu limite) |
| **Mecânico** | Modo mecânico, suas OS, catálogo sem custos e contagem de estoque |
| **Cliente** | Sem login: portal pelo link e consulta pública |

A API recusa (403) qualquer rota fora do perfil. O passo a passo de teste de cada módulo está em [docs/roteiro-de-testes.md](docs/roteiro-de-testes.md).

---

# Referência técnica

## Front-end (`web/`)

Angular 22.2.1 (versão estável mais recente em 04/10/2026), com componentes standalone, signals, detecção de mudanças sem zone.js (zoneless), `OnPush` em todos os componentes e o novo controle de fluxo (`@if`, `@for`).

### Rodar e testar

Dentro de `web/`:

```bash
npm ci               # instala exatamente o que está no package-lock.json
npm start            # http://localhost:4200 com proxy de /api para a API real em http://localhost:8080
npm run lint         # ESLint, checagem de comentários, checagem de cores fora dos tokens e Prettier
npm test             # testes unitários, de componente e de integração (Vitest), cobertura mínima de 85%
npm run build        # build de produção em dist/
npm run e2e          # Playwright (desktop e celular 390x844) + axe, contra a imagem nginx de produção e a API real
npm run lighthouse   # Lighthouse de acessibilidade (mínimo 95) do login e da tela de nova versão, contra a imagem nginx
npm run api:generate # regenera os tipos do contrato a partir do snapshot OpenAPI do back-end
```

O `npm start` precisa da API respondendo em `http://localhost:8080` (pelo Docker ou pelo `dotnet run --urls http://localhost:8080`, seção 15). O `npm run e2e` e o `npm run lighthouse` precisam do Docker aberto: eles constroem a imagem `web` e testam nela, com os mesmos cabeçalhos de segurança da produção. O E2E também sobe a API e um SQL Server descartáveis (veja "E2E com a API real").

### Como rodar o front-end

| Para quê | Comando | Endereço |
|---|---|---|
| Tudo no Docker (SQL Server, API e front de produção) | `docker compose up -d --build` | `http://localhost:4200` |
| Front em desenvolvimento (com a API rodando em `:8080`) | `cd web` e `npm start` | `http://localhost:4200` |
| Modo demonstração no Docker (dados fictícios) | `docker compose --profile demo up -d --build` | `http://localhost:4300` |
| Modo demonstração em desenvolvimento | `cd web` e `npm run start:demo` | `http://localhost:4200` |
| Testes unitários com cobertura | `cd web` e `npm test` | |
| Lint, comentários, cores e formatação | `cd web` e `npm run lint` | |
| E2E (sobe a stack descartável `oficina-e2e`) | `cd web` e `npm run e2e` | `:4300` (produção) e `:4301` (demo) |
| E2E reaproveitando uma stack já de pé | na raiz, `docker compose --env-file .env -f web/e2e/docker-compose.e2e.yml up --build --wait`; depois, em `web/`, `E2E_KEEP_STACK=1 npx playwright test` | |
| Acessibilidade do login (CI) | `cd web` e `npm run lighthouse` | |
| Desempenho e acessibilidade das telas autenticadas (stack do E2E de pé) | `cd web` e `npm run lighthouse:screens` | |
| Pacote de produção sem a API simulada | `cd web`, `npm run build` e `npm run check:bundle` | |
| Varredura de segredos | `cd web` e `npm run check:secrets` | |

O modo demo e o E2E usam as mesmas portas 4300/4301: não suba os dois ao mesmo tempo.

### Modo demonstração (validação manual)

No site normal todas as telas usam a API real. Para navegar com dados fictícios, existe o build `demo`, que liga no próprio navegador a mesma API simulada do E2E. Uma faixa fixa "Modo demonstração — dados fictícios" aparece em todas as telas.

```bash
docker compose --profile demo up -d --build
```

Abre em `http://localhost:4300` (o `web` normal continua em `http://localhost:4200`). Entre com um usuário fictício da API simulada (`admin@oficina.local`; senha em `web/src/testing/`). Para desligar: `docker compose --profile demo down`. Sem Docker, com a API rodando em `http://localhost:8080`: `cd web` e `npm run start:demo`.

| Tela | Dados no modo demonstração |
|---|---|
| Login, sessão, Clientes (lista, ficha, cadastro), Veículos do cliente, Equipe (lista, convite, ativar/desativar) | **Reais** (API) |
| Resumo e histórico do cliente, endereço por CEP, importação e exportação | Fictícios |
| Perfil profissional e permissões da equipe | Fictícios |
| Catálogo (serviços, pacotes e peças, para manter os pacotes coerentes com os serviços), Estoque e inventário | Fictícios |
| Orçamentos, Ordens de serviço, Modo mecânico, Documentos | Fictícios |
| Portal do cliente (`/o/portal-demo-3324`, confirme com `321`) e verificação de documento | Fictícios |
| Painel, Relatórios, "Pergunte aos seus dados" | Fictícios |
| Automações, filas e notificações | Fictícios |
| Configurações (reautenticação: a senha do usuário ou o código `123456`) | Fictícios |
| Tempo real (SignalR) | Desligado |

Os dados fictícios ficam só na memória do navegador e voltam ao início ao recarregar a página. O histórico fictício pertence aos clientes fictícios, não aos clientes reais cadastrados pela API.

**Garantia de que nada disso vai para produção:** a API simulada mora em `web/src/testing/` e só entra no build pela substituição de arquivo da configuração `demo` (`src/app/demo/demo-mode.ts` vira `src/testing/demo/demo-mode.ts`). O `npm run check:bundle` (no CI, depois do build de produção) falha se o pacote de produção tiver qualquer marcador da API simulada, e o CI também confere que o build `demo` contém a API simulada (`--expect-fake`), para a checagem não passar por engano.

### Regras do front-end

- Zero comentários em `.ts`, `.html` e `.scss` (o `npm run lint` falha se houver).
- Código em inglês; textos para o usuário em português, só no catálogo `src/app/core/i18n/messages.pt-BR.ts`.
- TypeScript estrito (`strict`, `noUncheckedIndexedAccess`, `exactOptionalPropertyTypes`, templates estritos), sem `any` e sem `!` (non-null). Hoje não há nenhum `!` no código; qualquer exceção precisa ser justificada nesta seção.
- O lint proíbe `innerHTML`, `[innerHTML]`, `bypassSecurityTrust*`, `localStorage`, `sessionStorage`, `console.*` e `eval`. Token de acesso fica só em memória.
- A única exceção de `localStorage` é `core/preferences/ui-preferences.ts`, com chaves fixas e só para preferências visuais (menu recolhido e tema). Nunca token nem dado pessoal.
- Nenhum dado pessoal em rota, query, título da aba ou log do navegador. Um teste falha se alguma rota tiver parâmetro `document`, `plate`, `licensePlate`, `email` ou `phone`.
- Cores só pelos tokens de `src/styles/tokens.scss` (o lint falha com cor solta em `.scss` ou `.html`).
- Cobertura mínima de 85% em linhas, igual ao back-end.

### Design system e shell

- **Componentes** em `src/app/shared/ui/` (prefixo `ui-`), construídos sobre o Angular CDK: botão, botão de ícone, campo, chip, segmentado, status, placa, toggle, KPI, skeleton, estado vazio, abas, card, avatar, tabela (ordenação e paginação), diálogo e toast. Cada um tem teste de comportamento e de acessibilidade (axe).
- **Ícones:** pacote `lucide` (só os dados dos ícones) desenhado pelo componente `ui-icon`. Só os ícones importados entram no bundle.
- **Shell:** menu lateral recolhível (preferência lembrada), gaveta no celular (fecha pelo botão, Esc ou toque fora), barra inferior no celular, busca com Ctrl K, notificações e troca de tema (claro, escuro, sistema). Funciona a partir de 360 px sem rolagem horizontal.
- **Rotas:** todas as telas do plano carregam sob demanda e estão implementadas (inclusive Financeiro, Documentos, Plataforma e a consulta pública `/portal`). As telas internas exigem login e a permissão da tela (veja "Autenticação e perfis").
- **Tela de desenvolvimento** (só no build de desenvolvimento): `/dev/ui` (catálogo de componentes).

### Camada de dados (gateways)

O site mostra **só dados do banco**. Não há dado simulado no navegador, nem no `npm start` nem no Docker.

Cada funcionalidade conversa com o back-end por uma **porta** (classe abstrata usada como token de injeção):

```text
features/customers/data/
  customers.gateway.ts     CustomersGateway (porta) e HttpCustomersGateway
  customer.models.ts       tipos do front (campos obrigatórios, sem null inesperado)
  customer.mappers.ts      DTO do contrato -> tipo do front (falha com ContractError se faltar campo)
  customers.providers.ts   provideCustomersData()
```

- `Http<Feature>Gateway` chama a API real pelo `ApiClient`, tipado pelos tipos gerados do OpenAPI ou pelo contrato da funcionalidade (`<feature>.contracts.ts`) quando o back-end ainda não publicou a rota.
- Cada funcionalidade tem um modo em `src/environments/environment.ts`: `http` (a API existe) ou `pending` (a API ainda não existe).

```ts
features: {
  auth: { api: 'http' },
  customers: { api: 'http' },
  users: { api: 'http' },
  realtime: { api: 'http' },
  session: { api: 'http' },
  twoFactor: { api: 'http' },
  vehicles: { api: 'http' },
  customerInsights: { api: 'http' },
  addresses: { api: 'http' },
  jobs: { api: 'http' },
  teamProfiles: { api: 'http' },
  roles: { api: 'http' },
  catalog: { api: 'http' },
  inventory: { api: 'http' },
  quotes: { api: 'http' },
  serviceOrders: { api: 'http' },
  portal: { api: 'http' },
  documents: { api: 'http' },
  dashboard: { api: 'http' },
  reports: { api: 'http' },
  automations: { api: 'http' },
  notifications: { api: 'http' },
  assistant: { api: 'http' },
  settings: { api: 'http' },
  finance: { api: 'http' },
  platform: { api: 'http' },
},
```

Hoje **todas** as funcionalidades usam a API real no build de produção. O modo `pending` continua existindo para a próxima funcionalidade que nascer antes do back-end (e no modo demonstração, que não tem tempo real, sessão por cookie nem 2FA).

- **Modo `pending`:** a porta responde na hora com o erro `feature.pending` (501). As telas continuam inteiras; o que depende da API mostra o bloco "Em construção" e cada ação mostra "Esta funcionalidade ainda está em construção no back-end." Nada é gravado e nenhum dado é inventado.

**Quando o back-end publicar uma rota:**

1. Atualize o snapshot OpenAPI do back-end (teste de contrato) e rode `npm run api:generate`.
2. Ajuste o `Http<Feature>Gateway` para os tipos gerados, se algo mudou do contrato do front.
3. Troque a funcionalidade para `api: 'http'` no `environment.ts`.
4. Rode `npm test` e `npm run e2e`.

### Testes do front

- A API simulada existe **só para os testes**, em `src/testing/fake-api/` (nunca entra no build): rotas, regras e erros de cada contrato sobre um banco em memória, no formato do back-end (camelCase, enums como texto, datas ISO em UTC, paginação `{ items, page, pageSize, totalItems }` e ProblemDetails com `code`, `traceId` e `errors`).
- `provideTestApi('mock')` responde as chamadas pelo interceptor de teste; `useGatewayHarness()` testa cada gateway contra o `msw/node`; `provideTestApi('pending')` reproduz o site com as funcionalidades sem API, para testar os avisos de construção.
- Cenários de erro por funcionalidade nos testes: `FakeBackend.setScenario(...)` (403, 404, 409, 422, 429, 500 e tempo esgotado).

### HTTP e erros

Interceptors, nesta ordem:

1. **Correlação:** cada requisição para `/api` recebe um `traceparent` W3C novo.
2. **Token:** envia `Authorization: Bearer` só para `/api`, com o token guardado em memória (`AuthTokenStore`).
3. **Erros:** converte qualquer falha em `ApiError` (status, `code`, título, detalhe, `traceId` e erros por campo):
   - 401 tenta renovar a sessão e repetir a requisição uma vez; se não der, encerra a sessão e leva para `/login`;
   - 403 leva para `/forbidden`;
   - 5xx, falha de rede e tempo esgotado mostram um aviso com o código para o suporte (`traceId`);
   - 400, 404, 409, 422 e 429 ficam para a tela tratar.
4. **Retry:** só `GET`, só para falha de rede, 408, 429, 502, 503 e 504. Até 2 novas tentativas, com espera de 300 ms e 600 ms, ou o tempo do cabeçalho `Retry-After`.
5. **Timeout:** 15 s por padrão.

Uma requisição pode mudar esse comportamento pelo `HttpContext`: `SKIP_GLOBAL_ERROR_HANDLING`, `SKIP_AUTH`, `RETRY_ENABLED` e `REQUEST_TIMEOUT_MS`. O login usa os três primeiros.

Para formulários, `applyApiErrors(form, error)` coloca a mensagem de cada campo do ProblemDetails no controle certo (inclusive `items[0].quantity`) e devolve os erros sem campo correspondente.

**Busca por dado pessoal sempre por POST**, com o dado no corpo (`findByDocument`, `findByEmail`). Os testes conferem que o documento e o e-mail não aparecem na URL.

### Autenticação e perfis

**Login** (`/login`, desktop e celular) contra `POST /api/auth/login` e `GET /api/auth/me`:

- Senha errada, e-mail inexistente e usuário excluído recebem a mesma mensagem genérica (401). Ninguém descobre se uma conta existe sem saber a senha.
- Conta desativada só aparece com a senha certa (403 `identity.accountDisabled`).
- Cinco senhas erradas seguidas bloqueiam a conta por 15 minutos (429 `identity.accountLocked`). A tela mostra a contagem regressiva e desabilita o botão até o fim.
- O limite por IP (5 tentativas por minuto) responde 429 `common.tooManyRequests` com `Retry-After`. A tela trata igual, com o tempo vindo do servidor.
- Sem rede, a tela avisa. Em erro 500, mostra o código para o suporte (`traceId`).

**Sessão:**

- O token fica só em memória (`AuthTokenStore`). O estado do usuário fica no `AuthStore` (`@ngrx/signals`): usuário, perfil, status e endereço para voltar depois do login.
- Recarregar a página leva ao login, porque ainda não existe cookie de renovação (prompt B1). O endereço pedido é lembrado e aberto depois do login.
- Um 401 no meio do uso tenta renovar a sessão uma vez (`SessionService.renew()`) e repete a requisição. Se não renovar, volta ao login sem perder o rascunho.
- **Inatividade:** aviso aos 25 minutos ("Continuar conectado" ou "Sair agora") e saída aos 30. Os tempos ficam em `environment.session` (`idleWarningMs`, `idleLogoutMs`).
- Sair (pelo menu da conta ou por inatividade) limpa todo o estado em memória registrado no `SessionStateRegistry`: token, usuário, rascunhos e endereço lembrado.

**Renovação da sessão (porta pronta para o B1).** A porta é o `SessionGateway` (`core/auth/session.gateway.ts`), com `refresh()` e `logout()`:

- `features.session.api = 'http'`: o refresh usa o cookie `oficina_refresh` (`HttpOnly`, `SameSite=Strict`, caminho `/api/auth`); recarregar a página mantém a sessão.
- Contrato que o B1 deve implementar: `POST /api/auth/refresh` (lê o cookie e devolve `{ accessToken, expiresAt }`, ou 401 sem cookie válido) e `POST /api/auth/logout` (revoga o cookie, 204).
- Para trocar para a API real depois do B1: publique os dois endpoints, rode `npm run api:generate`, mude `features.session.api` para `'http'` no `environment.ts` e rode `npm test`. O `HttpSessionGateway` já chama os dois endpoints.

**Verificação em duas etapas** (`/login/verify`, pronta para o B1):

- Contrato: o login pode responder `{ requiresTwoFactor: true, challengeId }`. A tela pede o código de 6 dígitos (colar, preencher automático, setas e apagar) e chama `POST /api/auth/2fa/verify` com `{ challengeId, code }`, que devolve `{ accessToken, expiresAt }` ou 401 `identity.invalidTwoFactorCode`.
- `features.twoFactor.api = 'http'`: quando o usuário tem 2FA ligado, o login devolve `requiresTwoFactor` e `challengeId`, e a tela pede o código TOTP.

**Perfis e permissões** (`core/auth/permissions.ts`). Um mapa central diz o que cada perfil pode fazer, por ação e não só por tela, com o menor privilégio possível:

| Perfil | Pode ver e fazer |
|---|---|
| Admin | Tudo da oficina, inclusive Configurações, Plataforma, usuários e excluir cliente |
| Manager | Operação, cadastros, estoque, aprovação de descontos acima do limite, relatórios, financeiro, automações e custos do catálogo. Equipe só leitura. Sem Configurações, Plataforma e exclusão de cliente |
| Attendant | Painel, OS, orçamentos, clientes, catálogo e estoque (só leitura) e documentos |
| Mechanic | Modo mecânico (tela inicial), OS, catálogo e estoque, sem custos e sem dados de cliente |
| Customer (portal) | Só os próprios orçamentos, a OS do próprio carro com o status e o catálogo de serviços. Nada de estoque nem dado interno da oficina |

- Guardas: `authGuard` (exige login e lembra o endereço), `guestGuard` (quem já entrou não vê o login), `permissionGuard(permissão)` (rota fora do perfil abre `/forbidden`) e `twoFactorGuard`.
- Diretiva estrutural para mostrar ou esconder por permissão: `<button *appCan="'customers.delete'; else somenteLeitura">`.
- O menu lateral e a barra inferior usam o mesmo mapa. O mecânico entra direto no Modo mecânico.
- As guardas são só experiência de uso. Toda autorização continua no back-end, e um 403 da API abre a tela de acesso negado.

**Telas de estado:**

- 403 (`/forbidden`) e 404 dentro do shell, com link para o início do perfil.
- 500 (`/error`): aparece quando uma navegação falha e mostra o código para o suporte (`traceId`). O código fica em memória, nunca na URL.
- Nova versão (`/update` e um aviso fixo): o front confere o `index.html` a cada 5 minutos (`versionCheckIntervalMs`) e avisa quando o pacote principal mudou. Se um pedaço da aplicação não carregar depois de uma publicação, abre a tela de nova versão.

**Rascunhos:** `bindDraft(form, chave)` guarda o formulário em memória enquanto a pessoa digita. Se a sessão expirar, o rascunho fica. Depois do novo login do mesmo usuário na mesma aba, a tela volta preenchida. Se entrar outro usuário, os rascunhos são descartados. Nada vai para `localStorage`.

### Clientes e veículos

Tela `/customers` (Admin e Attendant; o mecânico não vê o cadastro). No desktop, a lista e a ficha ficam lado a lado, como no protótipo; no celular, a ficha abre como tela própria.

**Lista** (API real `POST /api/admin/customers/query`):

- Busca por nome ou CPF/CNPJ completo, com atraso de digitação. O termo vai no corpo, nunca na URL.
- Filtros: tipo (PF/PJ), situação (ativa, OS aberta), frota e "sem retorno há 90 dias". Ordenação por nome ou data de cadastro, com paginação.
- Documento, e-mail e telefone chegam mascarados do servidor.
- **Resumo do cliente:** situação, frota e retorno vêm de `POST /api/admin/customers/summaries`; o front lê a lista real em lotes de 100 (até 500 clientes), cruza com o resumo e pagina localmente, avisando quando passa de 500. Quando o B2/B3 publicarem esses campos no `/query`, o filtro passa para o servidor e o `CustomersListStore` volta a paginar só pela API.

**Ficha** (`/customers/:id`):

- Dados mascarados com botão "revelar" por campo: `POST /api/admin/customers/{id}/reveal` devolve o valor e grava a auditoria (`sensitiveDataAccess`: usuário, cliente, campo, motivo e data).
- Veículos: cadastrar, editar, transferir dono e excluir.
- Histórico de OS, gasto total, ticket médio e última visita.
- Consentimento de mensagens com a data registrada pelo servidor.
- Ações: novo orçamento (leva o cliente pelo estado da navegação, sem dado na URL), editar, enviar WhatsApp ou e-mail, exportar dados do titular (LGPD) e excluir.
  - Envio sem consentimento fica desabilitado com o motivo ao lado.
  - Exclusão fica desabilitada enquanto houver veículos (a política do domínio também devolve 409).

**Formulário** (`/customers/new` e `/customers/:id/edit`):

- CPF/CNPJ, telefone e CEP com máscara e validação iguais às do back-end.
- O CEP preenche o endereço pela porta `AddressLookupGateway`. O navegador nunca chama serviço externo: a consulta real será feita pelo back-end.
- Consentimento por WhatsApp e e-mail. O de e-mail fica desabilitado sem e-mail.
- Erros 400 vão para os campos; 409 de documento duplicado marca o campo documento.
- O rascunho fica em memória e volta depois de um novo login do mesmo usuário.
- Na edição, e-mail e telefone são revelados com motivo `Edit` (também auditado).

**Contratos publicados pelo back-end:**

| Contrato | Rotas | Erros tratados |
|---|---|---|
| Veículos (`customers/data/vehicles.contracts.ts`) | `GET /api/admin/customers/{customerId}/vehicles`, `POST /api/admin/vehicles`, `PUT` e `DELETE /api/admin/vehicles/{id}`, `POST /api/admin/vehicles/{id}/transfer`, `POST /api/admin/vehicles/search` (placa no corpo) | 400 placa inválida, 409 placa duplicada (`vehicle.duplicatedLicensePlate`), 400 km menor (`vehicle.mileageDecrease`), 400 mesmo dono (`vehicle.sameOwner`), 409 OS aberta |
| Resumo do cliente (`insights.contracts.ts`) | `POST /api/admin/customers/summaries` (até 500 ids), `GET /api/admin/customers/{id}/service-history` | 400, 403 |
| CEP (`address-lookup.gateway.ts`) | `POST /api/addresses/lookup` | 400 CEP inválido, 404 CEP não encontrado |
| Filas (`customer-jobs.contracts.ts`) | `POST /api/admin/customers/{id}/data-export` (202), `POST /api/admin/imports/customers` (202, `{ fileName, csv }`), `GET /api/admin/jobs/{id}` | 400 arquivo inválido, 413 acima de 2 MB, 404 tarefa |

- A importação aceita CSV (`nome;documento;telefone;email`, com modelo para baixar) e mostra o relatório por linha quando a fila existir. `.xlsx` ficou de fora para não trazer biblioteca extra.
- A exportação LGPD roda em fila; quando termina, a ficha oferece o JSON para baixar.

**Permissões novas:** `customers.reveal` e `customers.import` (Admin, Manager, Attendant), `customers.export` (Admin, Manager) e `vehicles.manage` (Admin, Manager, Attendant).

### Equipe e permissões

Tela `/team` (Admin altera; Manager só lê). Usuários vêm da API real (`/api/admin/users`); perfil profissional, produtividade e permissões vêm da API real (`/api/admin/team`, `/api/admin/roles`).

- **Cartões e lista:** avatar, perfil, situação, especialidades e, para mecânicos, OS, ocupação, receita e uma sparkline de OS por semana (30 ou 90 dias). Filtros por perfil e situação vão na query da API (`role`, `status`), porque não são dados pessoais.
- **Convite:** o Admin informa nome, e-mail e perfil. A API cria o usuário sem senha e manda um link de uso único, válido por 72 horas. Ninguém, nem o Admin, vê a senha. Reenviar invalida o link anterior.
- **Página `/invite`** (pública): o token vem no fragmento (`#token=`), que não vai ao servidor nem ao `Referer`; o front lê, guarda em memória e limpa a URL. Mostra nome, perfil e e-mail mascarado, pede a senha com as regras do back-end e trata convite inexistente, expirado ou já usado.
- **Desativar e reativar** com confirmação. O botão fica desabilitado com o motivo quando é o próprio usuário ou o único administrador ativo; a API também recusa (422 `identity.cannotManageOwnAccount`, 409 `identity.lastActiveAdmin`).
- **Membro (`/team/:id`):** acesso (nome e perfil, API real), perfil profissional (especialidades, comissão, jornada por dia, limite de desconto; mecânico não aplica desconto) e produtividade (OS, horas, ocupação, receita, retrabalho e tendência).
- **Permissões (`/team/permissions`):** matriz perfil × permissão agrupada por módulo, só leitura. No celular, uma lista por perfil.

**Contratos publicados pelo back-end:**

| Contrato | Rotas | Erros tratados |
|---|---|---|
| Perfil profissional (`team/data/team-profile.contracts.ts`) | `POST /api/admin/team/profiles/query` (até 500 ids), `GET` e `PUT /api/admin/team/{userId}/profile`, `POST /api/admin/team/productivity` (`periodDays` 30 ou 90) | 400 por campo, 422 `team.mechanicDiscount`, 403 |
| Permissões (`team/data/roles.contracts.ts`) | `GET /api/admin/roles`, `PUT /api/admin/roles/{role}/permissions` | 501 `roles.customizationUnavailable` até o back-end suportar perfis personalizados |


### Catálogo

Tela `/catalog` com abas que são rotas: `/catalog/services`, `/catalog/packages` e `/catalog/parts`. Todos os perfis da oficina veem; só Admin e Manager (`catalog.manage`) cadastram, editam, ativam e desativam. Custo e margem só aparecem com `catalog.viewCosts` (Admin e Manager), e o contrato prevê que a API nem devolva o custo para os outros perfis.

- **Lista:** busca por nome ou código com atraso, filtro de situação, paginação e ativar/desativar na linha. Serviços mostram tempo, preço base e comissão; pacotes mostram itens, preço e economia; peças mostram código, tipo, venda, margem e estoque contra o mínimo.
- **Serviço** (`/catalog/services/new` e `/:id`): nome, descrição, preço base, tempo padrão, comissão, garantia e peças usadas normalmente.
- **Pacote:** itens (serviços e peças com quantidade de até 3 casas), soma dos itens, preço do pacote, economia em R$ e % e a faixa de km para a automação sugerir o pacote. Pacote mais caro que a soma dos itens mostra aviso.
- **Peça ou insumo:** código (regra do `PartCode`: letras, números, ponto e hífen, até 30), tipo, unidade, custo, preço de venda, margem ao vivo, estoque mínimo, fornecedor e código de barras EAN-13. O estoque atual é só leitura.
- **Histórico de preço** em cada página de edição: campo, valor antigo e novo, quem mudou e quando.
- **Caminhos tristes:** código duplicado (409 `inventory.duplicatePartCode` no campo), venda abaixo do custo (aviso e confirmação, sem bloquear) e desativar item usado em pacote ativo (409 `catalog.usedByActivePackage`, com os pacotes que bloqueiam).
- **Cálculos** em centavos (`features/catalog/data/catalog.math.ts`): soma por linha com arredondamento half-up, economia, margem sobre o preço de venda e validação do EAN-13.

**Tudo na API real** (`catalog: http`), com o mesmo formato nas três abas: `/api/admin/services`, `/api/admin/parts` e `/api/admin/packages`.

| Rota | Perfis | Observação |
|---|---|---|
| `GET /api/admin/{services\|parts\|packages}?term=&status=&includeInactive=&page=&pageSize=` | Todos da oficina | Paginado; `term` no nome (e no código exato da peça); sem `status`, só ativos, a não ser com `includeInactive=true`; `cost` da peça só para Admin e Manager |
| `GET /api/admin/{tipo}/{id}` | Todos da oficina | Pacote devolve os itens com nome e preço atual, `itemsTotal` e `savings` |
| `POST` e `PUT /api/admin/{tipo}` e `/{id}` | Admin, Manager | 400 por campo; 409 `inventory.duplicatePartCode` no campo `code`; serviço e pacote inativos recebem 409 até reativar |
| `POST /api/admin/{tipo}/{id}/deactivate` e `/reactivate` | Admin, Manager | 409 `catalog.usedByActivePackage` com os nomes dos pacotes ativos no `detail` |
| `GET /api/admin/{tipo}/{id}/price-history` | Todos da oficina | Do mais novo para o mais antigo, com o nome de quem mudou; mudanças de custo só para Admin e Manager |

O histórico de preço fica na tabela `priceChanges` (serviço: preço base; peça: custo e preço de venda; pacote: preço). As peças usadas normalmente no serviço ficam em `serviceUsualParts` e os itens do pacote em `packageItems`.

### Estoque e inventário

Tela `/inventory` com seções em rotas: Visão geral, Inventário (`/inventory/counts`), Pedidos de compra e Fornecedores. Contrato pronto para o B3 em `features/inventory/data/inventory.contracts.ts`, sobre a entidade `Part` real (`stockQuantity`, `minimumStock`, movimentos). `inventory` usa a API real (`/api/admin/inventory`).

- **Visão geral:** KPIs (abaixo do mínimo e pedido sugerido, saídas no mês, giro médio, sem saída há 90 dias), tabela com nível visual, situação (OK, Repor, Sem estoque, Parado), filtros por situação e local e busca.
- **Movimentações por peça** (`/inventory/parts/:partId`): entrada, saída e ajuste, com motivo, usuário, OS vinculada e saldo depois do movimento.
- **Entrada de mercadoria** em três modos: manual; por código de barras, com a câmera do celular (ZXing carregado sob demanda, só nesse diálogo) e digitação como alternativa; e por nota do fornecedor (upload de XML ou PDF até 2 MB, rascunho montado pelo back-end para conferência linha a linha).
- **Baixa** com bloqueio de saldo negativo.
- **Inventário por local:** abrir contagem, digitar o contado (no celular, com teclado numérico), ver a diferença na hora, enviar para aprovação; o gestor aprova (o ajuste entra no estoque) ou rejeita com motivo.
- **Pedidos de compra:** sugestões pelo mínimo e pela previsão de 30 dias, agrupadas por fornecedor; rascunho e envio para a fila do fornecedor.
- **Fornecedores:** cadastro simples (nome, CNPJ, contato e prazo de entrega).
- **Regras** (front e contrato): quantidade fracionada só em insumo (`inventory.fractionalPart`), saldo negativo bloqueado (422 `inventory.insufficientStock`), código de barras desconhecido (404 `inventory.unknownBarcode`), um inventário aberto por local (409 `inventory.countAlreadyOpen`) e ajuste só depois da aprovação.
- **Permissões novas:** `inventory.count` (todos da oficina, inclusive o mecânico) e `inventory.approve` (Admin e Manager). Entrada, baixa, compras e fornecedores ficam com `inventory.manage` (Admin e Manager).
- O nginx libera a câmera só para a própria origem (`Permissions-Policy: camera=(self)`).

| Rota | Uso |
|---|---|
| `GET /api/admin/inventory/stock?term=&situation=&location=&page=&pageSize=` | Saldo, KPIs e locais |
| `GET /api/admin/inventory/stock/{partId}/movements` | Movimentações da peça |
| `POST /api/admin/inventory/entries` e `/exits` | Entrada e baixa |
| `POST /api/admin/inventory/barcodes/lookup` | Peça pelo código de barras (no corpo) |
| `POST /api/admin/inventory/invoices/read` e `/confirm` | Rascunho da nota e confirmação |
| `GET` e `POST /api/admin/inventory/counts`, `GET /counts/{id}`, `PUT /counts/{id}/lines`, `POST /counts/{id}/submit\|approve\|reject` | Inventário |
| `GET /api/admin/inventory/purchase-orders/suggestions`, `GET` e `POST /purchase-orders`, `POST /purchase-orders/{id}/send` (fila) | Compras |
| `GET` e `POST /api/admin/inventory/suppliers`, `PUT /suppliers/{id}` | Fornecedores |

### Orçamentos

Telas `/quotes` (lista) e `/quotes/:id` (montador), para Admin, Manager e Attendant. Contrato pronto para o B4 em `features/quotes/data/quote.contracts.ts`, sobre a entidade `Quote` real (versão, status `Pending`, `Approved` e `Rejected`, totais de serviços e peças e `RejectionReason`). O `ApprovalToken` nunca chega ao front: o envio devolve só o link público. `quotes` usa a API real.

- **Lista:** número, cliente, placa, versão, situação (rascunho, aguardando cliente, aprovado, recusado), total e aviso de desconto aguardando gestor, com filtro por situação. "Novo orçamento" escolhe uma OS ainda sem orçamento.
- **Montador:** abas Pacotes, Serviços, Peças e Sugestões da IA (vindas do diagnóstico da OS), busca, adicionar, quantidade (fracionada só em insumo) e remover. Peça sem estoque mostra o que falta e o prazo do fornecedor. Preço que mudou no catálogo depois do rascunho mostra a diferença e o botão "Usar preços atuais".
- **Descontos:** automático (a regra cadastrada que der o maior desconto entre as que valem para o cliente: fiel, primeiro serviço, frota), manual em % ou R$ com motivo obrigatório, ou sem desconto. Acima do limite do perfil, "Pedir aprovação" cria o pedido e o gestor recebe a notificação. O envio fica bloqueado até o gestor aprovar exatamente aquele desconto. O total nunca fica negativo, e total zero bloqueia o envio.
- **Cálculo:** tudo em centavos inteiros na função pura `quote.math.ts` (linha, subtotais, desconto, limite por perfil, bloqueios de envio, parcelas, estoque, preço e comparação de versões), com arredondamento meio para cima no centavo e comparação de limite sem arredondar. O fake do contrato usa a mesma função, então o B4 tem a regra de referência.
- **Pagamento:** formas oferecidas (Pix, crédito, débito, dinheiro, boleto) entre as que a oficina ativou e parcelamento no crédito com simulação: sem juros até o limite configurado (o resto do centavo vai na primeira parcela) e tabela Price com os juros mensais configurados acima disso.
- **Envio:** WhatsApp, e-mail ou só o link; canal sem consentimento fica desabilitado com o motivo; mensagem pré-escrita pela IA e editável; expiração do link (1, 3, 7 ou 15 dias). Fase 1 do WhatsApp: o back-end devolve o link `wa.me` com o texto e o front abre em nova aba (o telefone completo nunca fica no front). Envio pela fila, com o estado "na fila", "enviado", "aberto pelo cliente" ou "falhou" (com "tentar de novo") e link expirado (com "gerar novo link"). Depois de enviado, o orçamento só muda com a resposta do cliente ou um novo link.
- **Respostas e versões:** registrar a aprovação no balcão ou a recusa com motivo obrigatório. A recusa fecha a versão; "Criar nova versão" copia os itens para a versão seguinte, e "Comparar versões" mostra o que entrou, saiu ou mudou e a diferença no total.
- **Rascunho automático:** cada mudança salva sozinha depois de 1,2 s (e no botão Salvar), com o estado na tela. Se a pessoa sair com alteração pendente, o rascunho é salvo na saída.
- **Documento:** "Ver PDF" abre a pré-visualização do orçamento (componente `app-quote-document`, base do documento da F10) com impressão pelo navegador. O PDF final com código de verificação será gerado pelo back-end.
- **Permissão nova:** `quotes.approveDiscount` (Admin e Manager).

| Rota | Uso |
|---|---|
| `GET /api/admin/quotes?status=&page=&pageSize=` | Lista (última versão de cada orçamento) |
| `GET /api/admin/quotes/service-orders` | OS sem orçamento |
| `POST /api/admin/quotes` | Cria a partir da OS |
| `GET /api/admin/quotes/{id}` e `GET /{id}/options` | Orçamento e o que o montador oferece (catálogo, estoque, sugestões da IA, regras, limite do perfil, pagamento, prazos) |
| `PUT /api/admin/quotes/{id}/draft` | Salva o rascunho (409 `quotes.locked` depois de enviado) |
| `POST /api/admin/quotes/{id}/discount-approvals` e `POST /{id}/discount-approvals/{approvalId}/approve\|reject` | Pedido de aprovação e decisão do gestor |
| `POST /api/admin/quotes/{id}/message-draft` | Mensagem escrita pela IA |
| `POST /api/admin/quotes/{id}/send`, `GET /{id}/delivery`, `POST /{id}/delivery/retry` | Envio pela fila (202), estado e reenvio |
| `POST /api/admin/quotes/{id}/approve` e `/reject` | Resposta do cliente registrada no balcão |
| `POST` e `GET /api/admin/quotes/{id}/versions` | Nova versão e histórico |

### Ordens de serviço e modo mecânico

Telas `/service-orders` (quadro, lista e agenda, com o botão **Nova OS** que busca o cliente, escolhe o veículo e abre a OS), `/service-orders/:id` (detalhe) e `/mechanic` (modo mecânico, pensado para o celular). Contrato pronto para o B4/B5 em `features/service-orders/data/service-order.contracts.ts` e `features/mechanic/data/mechanic.gateway.ts`, com os status reais `Received`, `InDiagnosis`, `WaitingApproval`, `InProgress`, `Completed` e `Delivered`. `serviceOrders` usa a API real.

- **Quadro:** seis colunas, arrastar e soltar (mouse e toque) e, como alternativa acessível, o menu "Mover para" em cada cartão, usável só com teclado. As transições espelham o domínio; durante o arraste, as colunas proibidas ficam marcadas, e soltar nelas explica o motivo (OS entregue, falta orçamento, transição inválida). Etiquetas: atrasada, sem peça e lembrete enviado.
- **Lista e agenda:** filtros por status, mecânico, só atrasadas e busca sem acento; agenda pela entrega prometida, com as atrasadas primeiro.
- **Detalhe:** linha do tempo (`StatusHistory`), orçamento vigente, peças reservadas, fotos, mecânico responsável, apontamento de tempo (iniciar e parar) e documentos.
- **Tempo real e conflito:** as mudanças chegam pelo `RealtimeGateway` (SignalR em `/api/hubs/realtime`, token em `access_token`; o nginx repassa o WebSocket e não grava essa rota no log). Um 409 de versão recarrega a OS e mostra o que outro usuário mudou.
- **Modo mecânico:** minhas OS, checklist (OK, Atenção, Trocar), fotos pela câmera, descrição do problema, "Sugerir com IA", enviar para orçamento, iniciar e finalizar o serviço.

| Rota | Uso |
|---|---|
| `GET /api/admin/service-orders` e `GET /{id}` | Quadro, lista, agenda e detalhe |
| `POST /api/admin/service-orders/{id}/transitions` | Mudança de status (409 com a versão atual em caso de conflito) |
| `POST /{id}/time-entries/start` e `/stop` | Apontamento de tempo |
| `GET /api/admin/service-orders/mine` e `GET /{id}/work` | Minhas OS e a ficha de trabalho do mecânico |
| `/{id}/checklist`, `/{id}/diagnosis` e `/{id}/photos` | Checklist, diagnóstico e fotos |
| `POST /{id}/diagnosis/suggestions` | Sugestão da IA a partir da descrição |
| `POST /{id}/send-to-quote`, `/start-service` e `/finish-service` | Enviar para orçamento, iniciar e finalizar |

### PWA (aplicativo instalável)

- `public/manifest.webmanifest` (abre em `/mechanic`, tela cheia, ícones 192, 512 e SVG) e um service worker próprio (`public/sw.js`), registrado só no build de produção por uma política de Trusted Types (`oficina#sw`) que aceita apenas `/sw.js`.
- O service worker guarda só o shell da aplicação (HTML, JS, CSS, fontes e ícones). Nunca guarda `/api/`, o portal (`/o/`) nem a verificação (`/v/`). A navegação tenta a rede primeiro e, sem rede, abre o shell guardado.
- **Fila offline do mecânico:** sem rede, cada ação (checklist, foto, diagnóstico, enviar para orçamento, iniciar, finalizar) entra numa fila local e já aparece na tela. A fila fica no IndexedDB (`oficina-offline`), ligada ao id do usuário: sobrevive a recarregar a página, e a fila de outro usuário é descartada. Checklist e diagnóstico repetidos da mesma OS viram uma ação só. Ao voltar a rede, a fila é enviada em ordem. Uma faixa mostra "sem conexão", quantas ações estão pendentes e quais são; a ação recusada pelo servidor aparece como erro.
- **Logout:** apaga o cache do service worker e o banco `oficina-offline`. A fila não guarda dado do cliente, só o número da OS e a ação.
- A CSP não mudou para o service worker: `script-src 'self'` já cobre `/sw.js`. O nginx serve `/sw.js` e o manifesto com `Cache-Control: no-cache`.

### Portal do cliente e documentos

Rotas públicas `/o/:token` (portal da OS) e `/v/:code` (verificação de documento), fora do shell autenticado, com layout próprio de celular. Contrato pronto para o B4/B6 em `features/portal/data/portal.gateway.ts` e `features/documents/data/documents.gateway.ts`. `portal` e `documents` usam a API real; o Pix gera o código copia-e-cola e o QR (QRCoder), e o recibo é emitido quando o webhook `POST /api/public/payments/pix/webhook` chega assinado (HMAC-SHA256 do `txId` com `Payments__PixWebhookSecret` no cabeçalho `X-Pix-Signature`). A rota `/portal` é a consulta pública da OS por número e CPF/CNPJ (no corpo, nunca na URL).

- **Posse:** o cliente confirma os 3 últimos dígitos do telefone; 3 erros bloqueiam o link. Link inválido, expirado ou encerrado mostra a mesma tela neutra, sem dizer se existiu.
- **OS:** linha do tempo, fotos, itens do orçamento com marcação do que aprovar e total recalculado, aprovar ou recusar (motivo obrigatório) e pagar: Pix (copia-e-cola e QR, com consulta do pagamento) ou cartão, que redireciona para a página do provedor (só endereço `https`) e mostra o resultado na volta.
- **Documento (OS, orçamento, recibo):** visualizar na tela, imprimir (CSS de impressão), baixar o PDF gerado pelo back-end, enviar por WhatsApp ou e-mail pela API, copiar o link de verificação e ver o código de verificação.
- **Verificação:** `/v/:code` diz se o documento é autêntico e mostra o resumo dele.
- **Token:** vai no corpo do `POST /api/public/portal/access`; depois, só a sessão do portal, no cabeçalho `X-Portal-Session`. Nunca aparece no título da aba, em log do navegador nem no log de acesso do nginx (`/o/` fica fora do `access_log`). `Referrer-Policy: no-referrer` vale para todo o site.

| Rota | Uso |
|---|---|
| `POST /api/public/portal/access` e `/confirm` | Abre o link e confirma a posse (423 quando bloqueado) |
| `GET /api/public/portal/order` | OS, linha do tempo, fotos e orçamento |
| `POST /api/public/portal/quote/approve` e `/reject` | Resposta do cliente |
| `POST /api/public/portal/payments/pix` e `/card`, `GET /payments/{id}` | Pagamento e consulta |
| `GET /api/public/portal/documents/{code}` e `/pdf` | Documento pelo portal |
| `GET /api/admin/documents/{code}`, `/pdf` e `POST /{code}/send` | Documento na área interna |
| `GET /api/public/documents/{code}/verification` | Verificação pública |

### Painel

Tela `/dashboard` (Admin, Manager e Attendant). Contrato pronto para o B7 em `features/dashboard/data/dashboard.contracts.ts`. `dashboard` usa a API real.

- **Cartões:** indicadores (faturamento, OS abertas, orçamentos aprovados, a receber, com a variação contra o período anterior), faturamento diário (serviços e peças empilhados), "precisa de atenção", serviços mais vendidos e pátio agora (OS por situação). Período: hoje, 7 dias ou 30 dias.
- **Precisa de atenção:** cada aviso traz a ação direta (gerar pedido de compra, enviar novo prazo, enviar lembrete) e o atalho para a OS, o orçamento, o estoque ou os clientes. O que veio da IA leva o selo "Sugestão da IA" e só acontece quando a pessoa clica.
- **Painel personalizável:** "Personalizar" mostra a lista de cartões para esconder e reordenar (subir e descer, tudo pelo teclado). A preferência fica salva na conta pelo `PUT /api/admin/dashboard/layout`.

| Rota | Uso |
|---|---|
| `GET /api/admin/dashboard?period=today\|week\|month` | Indicadores, gráficos e avisos |
| `GET` e `PUT /api/admin/dashboard/layout` | Cartões escolhidos e a ordem (por usuário) |
| `POST /api/admin/dashboard/attention/{id}/actions` | Ação direta de um aviso (`CreatePurchaseOrder`, `SendNewDeadline`, `SendReminder`) |

### Relatórios

Tela `/reports` (Admin e Manager). Contrato pronto para o B7 em `features/reports/data/report.contracts.ts`. `reports` usa a API real.

- **Construtor:** medir (faturamento, quantidade de OS, saída de peças, ticket médio, margem, comissão) × agrupar (mês, semana, mecânico, serviço, categoria, forma de pagamento, cliente) × período (este mês, mês passado, último trimestre, este ano ou datas próprias, até 366 dias) × gráfico (colunas, linha, rosca, tabela). O relatório se atualiza a cada mudança.
- **Definição tipada, nunca SQL:** o front manda só `{ measure, groupBy, period, chart }`, com valores de listas fechadas. `validateDefinition` recusa combinações sem sentido antes de chamar a API (comissão por cliente, linha sem agrupar por tempo, rosca por mês, rosca de ticket médio ou margem, período incompleto, invertido ou longo demais). O agrupamento que não combina com a medida aparece desabilitado com o motivo. O B7 deve repetir a mesma validação (422 `reports.incompatible`).
- **Meus relatórios:** salvar (nome de 3 a 80 caracteres, sem repetir), abrir no construtor, renomear, excluir (com confirmação) e agendar o envio (dia da semana, horário, até 10 destinatários e formato PDF ou Excel), ou cancelar o agendamento.
- **Exportar Excel ou PDF:** sempre pela fila (202). O front consulta a situação até o arquivo ficar pronto e mostra o link de download (URL assinada vinda do back-end). Exportação grande (por cliente) não é acompanhada na tela: o aviso diz que o arquivo chega por notificação quando ficar pronto.
- **Caminhos tristes:** período sem dados mostra um estado vazio útil; filtros incompatíveis bloqueiam salvar e exportar e explicam o motivo; falha da API mostra "Tentar de novo".

| Rota | Uso |
|---|---|
| `POST /api/admin/reports/run` | Executa a definição (dados por grupo, unidade, total e período efetivo) |
| `GET` e `POST /api/admin/reports/saved` | Meus relatórios |
| `PUT` e `DELETE /api/admin/reports/saved/{id}` | Renomear e excluir |
| `PUT /api/admin/reports/saved/{id}/schedule` | Agendar ou cancelar o envio (`schedule: null`) |
| `POST /api/admin/reports/exports` e `GET /exports/{id}` | Exportação pela fila e situação |

### Gráficos (`shared/ui/chart`)

- `ui-chart` usa ECharts (colunas, linha e rosca) com renderização em canvas, carregado sob demanda: o ECharts fica num pacote separado, baixado só quando um gráfico aparece.
- **Acessível:** o gráfico tem nome e um resumo dos valores para leitor de tela, e sempre há "Ver os dados em tabela". O tipo "Tabela" mostra só a tabela. Se o gráfico não puder ser desenhado, a tabela aparece no lugar.
- **Cores dos tokens:** as cores vêm das variáveis do tema (`--accent`, `--amber`, `--violet`, `--ok`, `--bad`, `--muted`, `--fg`, `--line`) e o gráfico é redesenhado quando o tema muda, legível no claro e no escuro.
- **CSP sem exceção:** a dica do gráfico usa o modo `richText` (desenhada no canvas, sem HTML). O ECharts limpa o próprio container com `innerHTML = ''`, que os Trusted Types bloqueiam; em vez de abrir uma política, o container do gráfico só aceita ser limpo (qualquer outro conteúdo é recusado).

### Automações e filas

Tela `/automations` (Admin e Manager). Contrato pronto para o B5/B8 em `features/automations/data/automation.gateway.ts`. `automations` usa a API real.

- **Regras "quando → faça":** lista com ativar e pausar (interruptor), editar e excluir. O editor visual monta a regra com gatilho (orçamento enviado, OS mudou de situação, peça abaixo do mínimo, OS entregue, revisão vencendo, pagamento vencido, pagamento recebido), até 5 condições (campo, comparação e valor, só os que valem para o gatilho), espera opcional (1 minuto a 60 dias) e até 5 ações (WhatsApp ou e-mail ao cliente, aviso à equipe, rascunho de pedido de compra, baixa de pagamento).
- **Modelos prontos:** lembrete de orçamento, aviso de status, reposição, pós-venda, revisão preventiva, cobrança e baixa de pagamento. O modelo só preenche o editor; nada é salvo sem a pessoa revisar.
- **Definição tipada e validada:** `{ name, trigger, conditions, wait, actions }` com listas fechadas. `validateRule` recusa nome curto, regra sem ação, condição ou ação que não combina com o gatilho, valor inválido, espera fora do limite e mensagem vazia, longa (500) ou com campo desconhecido entre chaves (só `{cliente}`, `{os}`, `{veiculo}`, `{valor}` e `{link}`). O B5 deve repetir a validação (422 `automations.invalid`).
- **Monitor de filas:** processadas, com erro e na fila nas últimas 24 h para notificações, documentos, IA, pagamentos, importações, relatórios e automações; fila de erros com o resumo (sem dado pessoal), o erro, as tentativas e "Reprocessar". Só o Admin reprocessa (permissão nova `automations.reprocess`); o Manager vê o aviso.

| Rota | Uso |
|---|---|
| `GET` e `POST /api/admin/automations/rules` | Regras |
| `PUT` e `DELETE /api/admin/automations/rules/{id}` | Editar e excluir |
| `PUT /api/admin/automations/rules/{id}/status` | Ativar ou pausar (`enabled`) |
| `GET /api/admin/automations/queues` e `/queues/failures` | Saúde das filas e fila de erros |
| `POST /api/admin/automations/queues/failures/{id}/retry` | Reprocessar (só Admin, 202) |

### Central de notificações

O sino da barra superior mostra quantas notificações não foram lidas e abre a central: lista com tipo e data, abrir (leva à tela ligada, só caminhos internos do próprio site), marcar uma ou todas como lidas e "Preferências" (por tipo e canal: no sistema, e-mail e WhatsApp). As novas chegam em tempo real pelo `RealtimeGateway` (evento `notification`); como reserva, a lista é consultada a cada 60 s e ao abrir o sino. A notificação nova é anunciada para leitores de tela. Contrato em `features/notifications/data/notification.models.ts`. `notifications` usa a API real.

| Rota | Uso |
|---|---|
| `GET /api/admin/notifications` | Notificações do usuário |
| `POST /api/admin/notifications/{id}/read` e `/read-all` | Marcar como lida |
| `GET` e `PUT /api/admin/notifications/preferences` | Preferências por tipo e canal |

### Pergunte aos seus dados

No construtor de relatórios, o cartão "Pergunte aos seus dados" manda a pergunta em português (8 a 300 caracteres) para o back-end, que devolve uma definição de relatório tipada (nunca SQL) e uma explicação. O front confere a definição com `readDefinition` e `validateDefinition` antes de mostrar. A sugestão aparece com o selo da IA e só vai para o construtor com "Abrir no construtor"; o construtor mostra que o relatório veio da pergunta até a pessoa mudar algum filtro. O front nunca chama o provedor de IA. Contrato em `features/reports/data/assistant.gateway.ts` (`POST /api/admin/assistant/report-definitions`, 422 `assistant.notUnderstood`). `assistant` usa a API real.

### Configurações e auditoria

Tela `/settings`, só para o Admin (o mapa de permissões deixa os demais perfis de fora; se um perfil tiver `settings.view` sem `settings.manage`, a tela abre somente leitura e sem a aba Segurança). Contrato pronto para o B1 e os demais em `features/settings/data/settings.contracts.ts`. `settings` usa a API real.

- **Oficina:** nome, CNPJ (validado), endereço, logo (PNG ou JPEG até 512 KB; SVG recusado), horário comercial por dia e o plano atual (somente leitura).
- **Descontos:** regras (tipo % ou R$, valor, condição, ativa) e limite de desconto manual por perfil. É o mesmo contrato que o montador de orçamentos já consome (`DiscountRuleDto` e o limite por perfil): a API simulada guarda tudo no mesmo lugar, e o teste confere que uma regra criada aqui aparece nas opções do orçamento.
- **Pagamentos:** formas aceitas, taxa e prazo de recebimento por forma, máximo de parcelas, parcelas sem juros e juros ao mês. É o mesmo `PaymentSettingsDto` da simulação de parcelas do orçamento (com `fees` a mais).
- **Mensagens:** modelos por tipo e canal com as variáveis `{cliente}`, `{veiculo}`, `{placa}`, `{total}`, `{link}` e `{prazo}`, pré-visualização com dados de exemplo (renderizada como texto) e horário de envio. Variável desconhecida é erro de validação no front e na API.
- **Segurança:** exigir 2FA de todos os perfis (o Admin sempre usa), tempo de inatividade (5 a 60 min, aplicado na hora ao timer de inatividade da sessão), tentativas e tempo de bloqueio, sessões ativas com "Encerrar" e o registro de auditoria com filtros por área, período e autor (somente leitura).
- **Documentos fiscais:** estado das integrações de NFS-e, NF-e e NFC-e (informativo; fase futura).
- **Alteração sensível:** todo salvamento pede confirmação e reautenticação (senha ou código 2FA) em `POST /api/auth/reauthenticate`, que devolve um token de uso único enviado no cabeçalho `X-Reauthentication`. Senha errada responde 422 (não 401, para não encerrar a sessão). Sem o token, a API responde 403 `settings.reauthenticationRequired`. Cada alteração vai para a auditoria.

| Rota | Uso |
|---|---|
| `POST /api/auth/reauthenticate` | Senha ou código 2FA; devolve `reauthToken` de uso único |
| `GET` e `PUT /api/admin/settings/workshop`, `POST /workshop/logo` | Dados da oficina e logo |
| `GET` e `PUT /api/admin/settings/discounts` | Regras e limites por perfil (mesmo contrato do orçamento) |
| `GET` e `PUT /api/admin/settings/payments` | Formas, taxas, prazos, parcelas e juros (mesmo contrato do orçamento) |
| `GET` e `PUT /api/admin/settings/messages` | Modelos e horário de envio |
| `GET` e `PUT /api/admin/settings/security` | Política de segurança |
| `GET /api/admin/settings/sessions`, `DELETE /sessions/{id}` | Sessões ativas |
| `POST /api/admin/settings/audit/search` | Auditoria com filtros no corpo (paginada) |
| `GET /api/admin/settings/fiscal` | Integrações fiscais |

### Selo da IA (`ui-ai-badge`)

Tudo o que vem da IA leva o mesmo selo violeta: avisos do painel, sugestões da IA no montador e nos itens do orçamento, mensagem ao cliente escrita pela IA, sugestões do diagnóstico no modo mecânico, leitura da nota do fornecedor, notificações sugeridas pela IA e "Pergunte aos seus dados". A IA só sugere: a ação acontece quando a pessoa confirma (gerar pedido, enviar, abrir no construtor, enviar para orçamento).

### Trocar uma funcionalidade de `pending` para `http`

Quando o back-end publicar a API de uma funcionalidade:

1. Confira o contrato no arquivo indicado na tabela abaixo (tipos de corpo e resposta, rotas e erros). Se a API real mudou algo, ajuste o `*.contracts.ts` e o mapeador da funcionalidade, e rode `npm run api:generate` para atualizar os tipos gerados do OpenAPI.
2. Em `web/src/environments/environment.ts`, troque `{ api: 'pending' }` por `{ api: 'http' }` na chave da funcionalidade.
3. Em `web/src/environments/environment.e2e.ts`, mantenha `http` e tire a rota da lista `INTERCEPTED_ROUTES` de `web/e2e/fake-backend.ts`, para o E2E passar a usar a API real.
4. Em `web/src/testing/demo/demo.interceptor.ts`, inclua a funcionalidade em `REAL_API_FEATURES` (o modo demo passa a usar a API real nela).
5. Atualize o teste `app-environment.spec.ts` (ele confere o que é real em cada build) e rode `npm test`, `npm run e2e` e `npm run build`.

### Contratos do front publicados pelo back-end

Todos os contratos abaixo já estão implementados na API (controllers em `src/Oficina.Api/Controllers`). Cada linha é o contrato que o front usa, com a API simulada de referência em `web/src/testing/fake-api/<funcionalidade>/`. Todo erro segue o ProblemDetails do projeto (`code`, `traceId`, `errors`); os códigos listados são os que o front trata de forma específica, além de 400 `common.validationFailed`, 401, 403 `common.forbidden` e 404.

| Funcionalidade (chave) | Rotas | Contrato (corpo e resposta) | Erros tratados | Prompt |
|---|---|---|---|---|
| Sessão e 2FA (`session`, `twoFactor`) | `POST /api/auth/refresh` (cookie `HttpOnly`), `POST /api/auth/2fa/verify`, `POST /api/auth/logout`, `POST /api/auth/reauthenticate` | `core/auth/session.gateway.ts`, `features/auth/data/two-factor.gateway.ts`, `features/settings/data/settings.contracts.ts` | 401 `identity.invalidTwoFactorCode`, 422 `auth.reauthenticationFailed` | B1 |
| Endereço por CEP (`addresses`) | `POST /api/addresses/lookup` | `features/customers/data/address-lookup.gateway.ts` | 404 `address.zipCodeNotFound` | B2 |
| Estoque (`inventory`) | `/api/admin/inventory/stock`, `/stock/{partId}/movements`, `/entries`, `/exits`, `/invoices/read` e `/confirm`, `/counts` (+ `/{id}`, `/{id}/lines`, `/{id}/{action}`), `/purchase-orders` (+ `/suggestions`, `/{id}/send`), `/suppliers` (+ `/{id}`), `/barcodes/lookup` | `features/inventory/data/inventory.contracts.ts` | 409 `inventory.*` (contagem aberta, fechada, pedido enviado, duplicados), 404 `inventory.unknownBarcode` | B3 |
| Resumo e histórico do cliente (`customerInsights`) | `POST /api/admin/customers/summaries`, `GET /api/admin/customers/{id}/service-history` | `features/customers/data/insights.contracts.ts` | | B4 |
| Orçamentos (`quotes`) | ver a seção "Orçamentos" | `features/quotes/data/quote.contracts.ts` | 409 `quotes.locked`, `quotes.notPending`, `quotes.alreadyExists`, `quotes.versionNotAllowed` | B4 |
| Ordens de serviço e modo mecânico (`serviceOrders`) | ver a seção "Ordens de serviço e modo mecânico" | `features/service-orders/data/service-order.contracts.ts`, `features/mechanic/data/mechanic.gateway.ts` | 409 de versão (devolve a OS atual), 422 de transição | B4 (sugestão da IA: B8) |
| Portal do cliente (`portal`) | ver a seção "Portal do cliente e documentos" | `features/portal/data/portal.gateway.ts` | 404 `portal.linkUnavailable` (inválido e expirado iguais), 423 bloqueado, 409 `portal.alreadyAnswered` | B4 (pagamento: B6) |
| Documentos (`documents`) | `GET /api/admin/documents/{code}`, `/pdf`, `POST /{code}/send`, `GET /api/public/documents/{code}/verification` | `features/documents/data/documents.gateway.ts` | 404 `documents.notFound` | B5 |
| Importação e exportação LGPD (`jobs`) | `POST /api/admin/imports/customers`, `POST /api/admin/customers/{id}/data-export`, `GET /api/admin/jobs/{id}` | `features/customers/data/customer-jobs.contracts.ts` | 413 `import.fileTooLarge` | B5 |
| Notificações (`notifications`) | ver a seção "Central de notificações" (+ evento `notification` no hub) | `features/notifications/data/notification.models.ts` | | B5 |
| Automações e filas (`automations`) | ver a seção "Automações e filas" | `features/automations/data/automation.gateway.ts` | 422 `automations.invalid` | B5 |
| Perfil profissional e permissões (`teamProfiles`, `roles`) | `POST /api/admin/team/profiles/query`, `GET`/`PUT /api/admin/team/{userId}/profile`, `GET /api/admin/team/productivity`, `GET /api/admin/roles`, `PUT /api/admin/roles/{role}/permissions` | `features/team/data/team-profile.contracts.ts`, `roles.contracts.ts` | | B1 |
| Configurações e auditoria (`settings`) | ver a seção "Configurações e auditoria" | `features/settings/data/settings.contracts.ts` (descontos e pagamentos com os tipos de `quote.contracts.ts`) | 403 `settings.reauthenticationRequired`, 422 `auth.reauthenticationFailed` | B1 (descontos e pagamentos: B4/B6; mensagens: B5) |
| Painel (`dashboard`) | ver a seção "Painel" | `features/dashboard/data/dashboard.contracts.ts` | | B7 |
| Relatórios (`reports`) | ver a seção "Relatórios" | `features/reports/data/report.contracts.ts` | 422 `reports.incompatible`, 409 `reports.duplicateName` | B7 |
| Pergunte aos seus dados (`assistant`) | `POST /api/admin/assistant/report-definitions` | `features/reports/data/assistant.gateway.ts` | 422 `assistant.notUnderstood` | B8 |
| Tempo real (`realtime`) | hub SignalR `/api/hubs/realtime`, evento `event` (`serviceOrderStatusChanged`, `paymentConfirmed`, `exportReady`, `notification`) | `core/realtime/realtime.gateway.ts` | | B7 |

Também estão ligados à API real: veículos, catálogo (serviços, peças e pacotes), financeiro (`finance`: `GET /api/admin/finance`), lista de documentos (`GET /api/admin/documents`), plataforma (`platform`: `GET /api/admin/platform`) e a consulta pública da OS em `/portal` (`POST /api/public/service-orders/lookup`).

### Mudanças de infraestrutura e decisões registradas

- `web/playwright.config.ts` reaproveita uma stack já de pé fora do CI (`reuseExistingServer: !isCi`), e `E2E_KEEP_STACK=1` faz o `globalTeardown` não derrubar a stack. Os dois existem para iterar no E2E sem reconstruir as imagens a cada execução; no CI nada muda.
- A fila offline do mecânico sobrevive à sessão expirada (é apagada no logout e na inatividade): quem perdeu a sessão entra de novo e as ações sincronizam. A fila fica ligada ao id do usuário e é descartada se outro usuário entrar no mesmo aparelho.
- O nginx comprime com gzip os arquivos estáticos (JS, CSS, JSON, SVG e manifesto). As respostas da API não são comprimidas pelo nginx.
- O redirecionamento do pagamento com cartão no portal só aceita `https` de origens em `paymentProviderOrigins` (`environment.ts`). Em produção a lista está vazia até a fase 2 do pagamento (B6): o portal mostra um aviso em vez de redirecionar.
- O `angular.json` tem orçamento por rota (`bundle`) para as telas principais e para o ECharts.

### E2E com a API real

O `npm run e2e` sobe uma stack descartável (`web/e2e/docker-compose.e2e.yml`, projeto `oficina-e2e`, porta 4300): imagem nginx de produção, API real e SQL Server sem volume. O banco de desenvolvimento não é tocado.

- Na preparação, o teste entra como `admin@oficina.local` e cria pela API um atendente, um mecânico e um usuário desativado (senha de teste definida em `web/e2e/accounts.ts`).
- Cobre: login, senha errada, conta desativada, bloqueio após 5 erros, menu por perfil, rota proibida por URL, volta ao endereço pedido, logout, recarga da página e inatividade (relógio simulado).
- Clientes, contra a API real: criar, revelar, editar, buscar, excluir, documento inválido e duplicado (409), envio desabilitado sem consentimento. Veículos contra a API real: cadastrar e editar, com a quilometragem menor recusada no campo. Resumo e histórico vêm da API real. Tudo em desktop e celular.
- Equipe, contra a API real: convidar, ler o link no arquivo `.eml` que a API grava em `web/e2e/.mail/` (pasta montada no container e apagada no fim), criar a senha, entrar com o convidado, link já usado, e-mail repetido, desativar e reativar e o Admin sem poder se desativar. Perfil profissional salvo na API real e permissões fixas exibidas como somente leitura.
- Catálogo contra a API real: cadastrar serviço e alterar o preço com histórico, cadastrar peça com margem e histórico de custo e venda, código repetido recusado, montar pacote com economia e desativar peça usada em pacote bloqueado com o nome do pacote. Estoque contra a API real: visão geral, entrada por código de barras e por nota com a validação do servidor, contagem, sugestões de compra e cadastro de fornecedor, e mecânico sem custos nem movimentação.
- Orçamentos sem API: o build do E2E usa `--configuration production,e2e` (mesma imagem nginx; o `environment.e2e.ts` só troca `quotes` para `http`) e o teste responde `/api/admin/quotes/**` com `page.route`, usando a mesma API simulada dos testes unitários (o contrato do B4). Cobre, em desktop e celular: novo orçamento a partir da OS, montador com aviso de estoque, regra automática, desconto acima do limite com pedido de aprovação, pagamento com parcelas, rascunho salvo, pré-visualização, envio que falha e é reenviado até "aberto pelo cliente", aprovação no balcão, recusa com motivo, nova versão, comparação de versões, preço alterado no catálogo e link expirado.
- Ordens de serviço, modo mecânico, portal e documentos sem API: o `environment.e2e.ts` também liga `serviceOrders`, `portal` e `documents`, e o `page.route` responde com a mesma API simulada. Cobre, em desktop e celular: arrastar no quadro (mouse no desktop, toque no celular), transição bloqueada, mover pelo teclado, lista, agenda, detalhe, conflito de versão, modo mecânico offline com a fila no IndexedDB e a sincronização na volta da rede, PWA (manifesto, service worker, abrir sem rede e cache e fila apagados no logout), portal (tela neutra para link inválido e expirado, bloqueio após 3 erros, aprovação parcial com Pix, cartão recusado na volta do provedor, recusa com motivo) e o recibo baixado e verificado.
- Painel e relatórios sem API: o `environment.e2e.ts` liga `dashboard` e `reports`, e o `page.route` responde com a mesma API simulada (contrato do B7). Cobre, em desktop e celular: indicadores e gráficos desenhados de verdade (canvas, sem erro de CSP ou Trusted Types), ação direta de um aviso, troca de período, painel personalizado e mantido ao voltar à tela, construtor com agrupamento desabilitado, combinação incompatível, período sem dados, salvar, agendar, exportar até o link de download, exportação grande na fila e excluir.
- Automações, notificações e IA sem API: o `environment.e2e.ts` liga `automations`, `notifications` e `assistant`. Cobre, em desktop e celular: pausar regra, criar a partir de um modelo com erro de validação e espera, reprocessar item da fila de erros, sino com contagem, notificação nova ao reabrir, marcar todas como lidas, preferências por canal e a pergunta em português que vira relatório no construtor (sem erro de CSP).
- As telas de orçamentos, OS, portal, painel, relatórios, automações, notificações, assistente e configurações continuam respondidas pelo `page.route` no E2E, porque os cenários dependem de dados semeados (links do portal, OS em todas as etapas, avisos). O mesmo fluxo contra a API real está coberto pelos testes funcionais do back-end (`QuotePortalApiTests`, `ServiceOrderWorkflowApiTests`, `DashboardApiTests`, `ReportsApiTests`, `OperationsApiTests`, `SettingsApiTests`, `FinanceApiTests`, `JobsApiTests`).
- O 429 do limite por IP, a falta de rede e o 500 são simulados com `page.route`, no formato exato do back-end. Na stack de teste o limite por IP sobe para 1000 (`Security__LoginRateLimit__PermitLimit`), para os testes não se bloquearem entre si.

### Tipos do contrato (OpenAPI)

- `npm run api:generate` lê `tests/Oficina.ContractTests/OpenApi/Snapshots/openapi.json` e gera `src/app/core/api/generated/` com `@hey-api/openapi-ts` (só tipos, sem código de execução).
- Um teste (`scripts/api-generate.test.mjs`) falha se o snapshot mudou e a pasta não foi regenerada.
- A pasta gerada é uma exceção às regras de comentário, Prettier, ESLint e cobertura, igual às Migrations do back-end. Não edite à mão.

### Utilitários (`src/app/shared/util/`)

- **Dinheiro:** valores em centavos inteiros (`Cents`), formatação e leitura em BRL, sem erro de ponto flutuante.
- **Datas:** a API manda UTC; a tela mostra em `America/Sao_Paulo`.
- **CPF/CNPJ, placa (antiga e Mercosul), telefone e e-mail:** validação igual à do back-end, máscara de digitação e validadores de Reactive Forms.
- **Mascaramento:** `maskDocument`, `maskEmail`, `maskPhone`, `maskPlate` e `maskName`, para exibir dado pessoal sem expor tudo.

### Segurança da entrega (nginx)

O nginx da imagem `web` envia em toda resposta:

- **CSP estrita:** só a própria origem, sem `unsafe-inline` e sem `unsafe-eval`. Os estilos dos componentes usam um *nonce* novo a cada requisição.
- **Trusted Types:** `require-trusted-types-for 'script'`.
- **Outros cabeçalhos:** `frame-ancestors 'none'`, HSTS, `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer` e `Permissions-Policy`.
- **Compressão:** gzip nos arquivos estáticos.

Rotas desconhecidas devolvem o `index.html` (fallback de SPA), e o Angular decide a tela. As fontes (Barlow Semi Condensed, IBM Plex Sans e IBM Plex Mono) vão dentro do pacote, sem chamada a servidores externos.

### Estrutura

```text
web/
  src/app/core/api/           ApiClient tipado e tipos gerados do OpenAPI (generated/)
  src/app/core/auth/          token em memória, sessão, renovação e mapa de permissões
  src/app/core/config/        ambiente e provideGateway (http ou pending)
  src/app/core/data/          paginação, busca e erro de contrato
  src/app/core/forms/         erros da API nos campos do formulário
  src/app/core/http/          ApiError, interceptors e HttpContext
  src/app/core/i18n/          catálogo de mensagens pt-BR
  src/app/core/preferences/   preferências visuais (único uso de localStorage)
  src/app/core/realtime/      porta de tempo real, adaptador SignalR (carregado só ao conectar) e versão pending
  src/app/core/pwa/           registro do service worker, instalação e fila offline (IndexedDB)
  src/app/core/session/       inatividade
  src/app/core/drafts/        rascunhos de formulário em memória
  src/app/core/errors/        tela de erro e falhas de navegação
  src/app/core/version/       aviso de nova versão
  src/app/core/theme/         tema claro, escuro e sistema
  src/app/features/*/data/    portas, adaptadores, modelos e mapeadores por funcionalidade
  src/app/features/auth/      AuthStore, guardas, diretiva *appCan, login e duas etapas
  src/app/features/customers/ clientes e veículos: lista, ficha, formulário, diálogos e contratos
  src/app/features/team/      equipe: cartões, membro, permissões, convite e contratos
  src/app/features/catalog/   catálogo: serviços, pacotes, peças, histórico de preço e contrato
  src/app/features/inventory/ estoque: visão geral, movimentações, entrada, inventário, compras e fornecedores
  src/app/features/quotes/    orçamentos: lista, montador, descontos, pagamento, envio, versões, documento e contrato
  src/app/features/service-orders/ ordens de serviço: quadro, lista, agenda, detalhe e contrato
  src/app/features/mechanic/  modo mecânico e fila offline
  src/app/features/portal/    portal público do cliente
  src/app/features/documents/ documento compartilhado e página de verificação
  src/app/features/dashboard/ painel personalizável e contrato
  src/app/features/reports/   construtor de relatórios, meus relatórios, agendamento, exportação, "Pergunte aos seus dados" e contrato
  src/app/features/automations/ regras, editor visual, modelos, monitor de filas e contrato
  src/app/features/notifications/ central de notificações, preferências e contrato
  src/app/features/settings/  configurações, reautenticação, sessões, auditoria e contrato
  src/app/layout/             shell, menu, barra superior, barra inferior e páginas gerais
  src/app/shared/ui/          design system (inclui o gráfico ECharts e o selo da IA)
  src/app/shared/util/        dinheiro, datas, documentos, placa, telefone e máscaras
  src/app/dev/                tela só de desenvolvimento (/dev/ui)
  src/testing/                apoio dos testes: API simulada (só testes), harness e ambientes
  src/environments/           só URLs públicas e o modo de cada funcionalidade
  src/styles/                 tokens, controles e overlays
  e2e/                        testes Playwright e a stack descartável do E2E
  nginx/                      configuração do nginx e cabeçalhos de segurança
  scripts/                    checagens, geração do contrato, servidor do E2E e Lighthouse
```

## Credenciais (somente desenvolvimento)

Nenhuma senha ou chave fica no repositório. Os valores de desenvolvimento ficam no `.env` local, criado a partir do [.env.example](.env.example) (seção 6), e os usuários estão na seção 7.

| O quê | Onde |
|---|---|
| SQL Server (`sa`) | `MSSQL_SA_PASSWORD` do `.env` |
| Admin de teste (criado na subida em Development, se ainda não existir) | `DEV_ADMIN_EMAIL` e `DEV_ADMIN_PASSWORD` do `.env` |
| Chave do JWT | `JWT_SECRET` do `.env` |
| Usuários da carga de demonstração | `SEED_USERS_PASSWORD` do `.env` |
| Segredo do webhook Pix | `PIX_WEBHOOK_SECRET` do `.env` |
| SonarQube local | usuário `admin`, senha `SONAR_ADMIN_PASSWORD` do `.env` |
| API simulada dos testes do front (só `npm test` e modo demonstração, sem servidor) | usuários fictícios definidos em `web/src/testing/` |

Fora de Development, a API não cria o Admin de teste e não publica o Swagger. Todos os segredos vêm de variáveis de ambiente.

---

## Endpoints

| Método e rota | Perfis | Sucesso |
|---|---|---|
| `POST /api/auth/invitations/inspect` | Anônimo (5 por minuto por IP, à parte do login) | 200 com nome, e-mail mascarado, perfil e validade; 404, 409 (já usado) ou 422 (expirado) |
| `POST /api/auth/invitations/accept` | Anônimo (mesmo limite) | 204; senha forte e token de uso único |
| `POST /api/auth/login` | Anônimo (5 tentativas por minuto por IP; a 6ª recebe 429 com `Retry-After`). 401 genérico para senha errada; 403 `identity.accountDisabled` só com a senha certa; 5 senhas erradas seguidas bloqueiam a conta por 15 min (429 `identity.accountLocked`) | 200 com `accessToken` e `expiresAt` |
| `GET /api/auth/me` | Qualquer usuário logado | 200 com `id`, `name`, `email` e `role` |
| `GET /api/admin/customers?page=1&pageSize=20` | Admin, Manager, Attendant | 200 com `{ items, page, pageSize, totalItems }` |
| `POST /api/admin/customers` | Admin, Manager, Attendant | 201; aceita `messagingConsent { whatsApp, email }` e o servidor grava a data |
| `GET /api/admin/customers/{id}` | Admin, Manager, Attendant | 200 |
| `PUT /api/admin/customers/{id}` | Admin, Manager, Attendant | 200 |
| `POST /api/admin/customers/search` | Admin, Manager, Attendant | 200 (CPF/CNPJ no corpo) |
| `POST /api/admin/customers/query` | Admin, Manager, Attendant | 200 paginado; `term` (nome ou CPF/CNPJ completo), `documentType`, `sort` (Name, CreatedAt) e `direction` no corpo |
| `POST /api/admin/customers/{id}/reveal` | Admin, Manager, Attendant | 200 com o campo aberto (`Document`, `Email` ou `Phone`); grava quem viu, quando e por quê |
| `DELETE /api/admin/customers/{id}` | Admin | 204 |
| `GET /api/admin/vehicles?page=1&pageSize=20` | Admin, Manager, Attendant, Mechanic | 200 com `{ items, page, pageSize, totalItems }` |
| `POST /api/admin/vehicles/search` | Admin, Manager, Attendant, Mechanic | 200 com o veículo (placa no corpo, nunca na URL); 404 `vehicle.notFound` |
| `GET /api/admin/customers/{id}/vehicles` | Admin, Manager, Attendant, Mechanic | 200 com `{ items }` |
| `POST /api/admin/vehicles` | Admin, Manager, Attendant | 201; `licensePlate`, `brand`, `model`, `year` e `mileage` (0 a 9.999.999) |
| `GET /api/admin/vehicles/{id}` | Admin, Manager, Attendant, Mechanic | 200 com dono (id e nome) e histórico de ordens de serviço |
| `PUT /api/admin/vehicles/{id}` | Admin, Manager, Attendant | 200; quilometragem menor recebe 400 `vehicle.mileageDecrease` |
| `POST /api/admin/vehicles/{id}/transfer` | Admin, Manager, Attendant | 200; `newCustomerId` no corpo; mesmo dono recebe 400 `vehicle.sameOwner` |
| `DELETE /api/admin/vehicles/{id}` | Admin | 204 |
| `GET /api/admin/services?page=1&pageSize=20&term=&status=&includeInactive=false` | Admin, Manager, Attendant, Mechanic | 200 com `{ items, page, pageSize, totalItems }`; `term` busca no nome e na descrição; `status` (`Active`, `Inactive`) filtra; sem `status`, só ativos, a não ser com `includeInactive=true` |
| `POST /api/admin/services` | Admin, Manager | 201; `commissionPercent` (0 a 50) e `warrantyDays` (0 a 3650); grava o preço inicial no histórico |
| `GET /api/admin/services/{id}` | Admin, Manager, Attendant, Mechanic | 200 (ativo ou inativo, indicado em `isActive`) |
| `GET /api/admin/services/{id}/price-history` | Admin, Manager, Attendant, Mechanic | 200 com `{ items }` do mais novo para o mais antigo (`field`, `oldValue`, `newValue`, `changedBy`, `changedAt`) |
| `PUT /api/admin/services/{id}` | Admin, Manager | 200 (serviço inativo recebe 409; reative antes de alterar); preço novo vai para o histórico |
| `POST /api/admin/services/{id}/deactivate` ou `DELETE /api/admin/services/{id}` | Admin, Manager | 204 (o `DELETE` desativa; o histórico continua) |
| `POST /api/admin/services/{id}/reactivate` | Admin, Manager | 204 |
| `GET /api/admin/parts?page=1&pageSize=20&term=&status=&includeInactive=false` | Admin, Manager, Attendant, Mechanic | 200 paginado; `cost` só para Admin e Manager |
| `POST /api/admin/parts` | Admin, Manager | 201; `code` único (409 `inventory.duplicatePartCode`), `barcode` EAN-13 opcional; grava custo e venda no histórico |
| `GET /api/admin/parts/{id}` e `/price-history` | Admin, Manager, Attendant, Mechanic | 200; custo e mudanças de custo só para Admin e Manager |
| `PUT /api/admin/parts/{id}` | Admin, Manager | 200; o estoque não muda aqui |
| `POST /api/admin/parts/{id}/deactivate` e `/reactivate`, `DELETE /api/admin/parts/{id}` | Admin, Manager | 204 (o `DELETE` desativa); peça em pacote ativo recebe 409 `catalog.usedByActivePackage` |
| `GET /api/admin/packages?page=1&pageSize=20&term=&status=&includeInactive=false` | Admin, Manager, Attendant, Mechanic | 200 paginado com `itemsTotal` e `savings` |
| `POST /api/admin/packages` e `PUT /api/admin/packages/{id}` | Admin, Manager | 201/200; 1 a 50 serviços ou peças ativos, sem repetir; `suggestion { minMileage, maxMileage }` opcional |
| `GET /api/admin/packages/{id}` e `/price-history` | Admin, Manager, Attendant, Mechanic | 200 |
| `POST /api/admin/packages/{id}/deactivate` e `/reactivate` | Admin, Manager | 204 |
| `GET /api/admin/users` | Admin | 200 |
| `POST /api/admin/users` | Admin | 201 |
| `GET /api/admin/users/{id}` | Admin | 200 |
| `GET /api/admin/users?role=&status=` | Admin, Manager | 200 paginado, com `status` (Active, Inactive, InvitationPending) e `invitationExpiresAt` |
| `POST /api/admin/users/invite` | Admin | 201; usuário sem senha e e-mail com o link (72 h, uso único) |
| `POST /api/admin/users/{id}/invite/resend` | Admin | 200; invalida o link anterior |
| `POST /api/admin/users` | Admin | 201 (com senha; o front não usa) |
| `GET /api/admin/users/{id}` | Admin, Manager | 200 |
| `PUT /api/admin/users/{id}` | Admin | 200 |
| `DELETE /api/admin/users/{id}` | Admin | 204 |
| `POST /api/admin/users/{id}/deactivate` | Admin | 204 |
| `POST /api/admin/users/{id}/reactivate` | Admin | 204 |
| `POST /api/admin/users/search` | Admin | 200 (e-mail no corpo) |
| `POST /api/auth/refresh`, `/logout`, `/2fa/verify`, `/reauthenticate`, `/2fa/setup`, `/2fa/enable`, `/2fa/disable` | Anônimo (refresh/logout pelo cookie `oficina_refresh`) e usuário logado | sessão renovada pelo cookie `HttpOnly`; reautenticação devolve token de uso único para o cabeçalho `X-Reauthentication` |
| `POST /api/addresses/lookup` | Admin, Manager, Attendant | 200 com o endereço do CEP (ViaCEP); 404 `address.zipCodeNotFound` |
| `/api/admin/team/*` e `/api/admin/roles` | Admin, Manager | perfil profissional, produtividade e permissões fixas |
| `/api/admin/inventory/*` | Admin, Manager (Mechanic: contagem) | estoque, entradas (NF-e), saídas, contagens, fornecedores e pedidos de compra |
| `/api/admin/service-orders/*` | Admin, Manager, Attendant (Mechanic: ficha, timer, checklist, fotos, diagnóstico) | abrir OS, quadro, transições com versão, modo mecânico; busca por placa em `POST /search` (placa no corpo); foto sob demanda em `GET /{id}/photos/{photoId}` |
| `GET /api/admin/metrics/average-execution-time?from=&to=` | Admin | 200 com `averageMinutes` e `totalServiceOrders` |
| `/api/admin/quotes/*` | Admin, Manager, Attendant | orçamento, descontos com aprovação, envio, versões |
| `/api/public/portal/*`, `POST /api/public/service-orders/lookup`, `GET /api/public/documents/{code}/verification`, `POST /api/public/payments/pix/webhook` | Anônimo (sessão do portal no cabeçalho `X-Portal-Session`; 60 requisições por minuto por IP) | portal do cliente, fotos sob demanda, consulta pública da OS, Pix com QR e verificação de documento |
| `GET /api/admin/documents`, `/{code}`, `/{code}/pdf`, `POST /{code}/send` | Admin, Manager, Attendant | documentos emitidos (OS, orçamento, recibo) |
| `/api/admin/customers/summaries`, `/{id}/service-history`, `/{id}/data-export`, `/api/admin/imports/customers`, `/api/admin/jobs/{id}` | Admin, Manager, Attendant (exportação LGPD: Admin, Manager) | resumo, histórico, exportação LGPD e importação CSV |
| `/api/admin/dashboard`, `/layout`, `/attention/{id}/actions` | Admin, Manager, Attendant | painel com indicadores reais |
| `/api/admin/reports/*`, `/api/admin/assistant/report-definitions` | Admin, Manager | relatórios, agendamento por e-mail, exportação XLSX/PDF e assistente local |
| `/api/admin/automations/*` | Admin, Manager (reprocessar: Admin) | regras, filas e falhas |
| `/api/admin/notifications/*` | Qualquer usuário logado | notificações e preferências |
| `/api/admin/settings/*` | Admin, Manager (alterar: Admin com `X-Reauthentication`) | oficina, descontos, pagamentos, mensagens, segurança, sessões, auditoria e fiscal |
| `GET /api/admin/finance`, `GET /api/admin/platform` | Admin, Manager / Admin | financeiro e plataforma |
| Hub SignalR `/api/hubs/realtime` | Qualquer usuário logado (token em `access_token`) | evento `event` com mudanças de OS, pagamento, exportação e notificação |
| `GET /health` | Anônimo | 200 `Healthy` |

Documento, e-mail e telefone de cliente sempre saem mascarados nas respostas; o valor aberto só sai pelo `/reveal`, com auditoria.

Os erros seguem o ProblemDetails (`application/problem+json`), com `title`, `status`, `detail` (em português), `code`, `traceId` e, na validação, `errors` com cada campo. Status usados: 400 (dados inválidos), 401 (sem login), 403 (perfil sem permissão), 404, 409 (conflito), 422 (regra de negócio), 429 e 500.

---

## Padrões

- **Camadas:** a API chama a Application pelo MediatR. A Application usa o domínio e as interfaces, e a Infrastructure implementa as interfaces. O domínio não depende de ninguém.
- **CQRS:** um command ou query por caso de uso, cada um com validator (FluentValidation) e handler.
- **Result pattern:** erros esperados voltam como `Result` com um `Error` dos catálogos (`…Errors`), com código estável e mensagem em português. Exceção só para erro inesperado.
- **Soft delete e auditoria:** "excluir" preenche `deletedAt`. As colunas `createdAt` e `updatedAt` são preenchidas por um interceptor do EF Core. As consultas ignoram os registros excluídos.
- **Desativar não é excluir:** desativar (usuários e serviços) só muda `isActive` para `false`. O registro continua consultável e pode ser reativado. Serviço inativo sai da listagem padrão, não pode ser alterado nem incluído em ordem de serviço, e os itens que já estão nas ordens continuam como estão.
- **Nomes:** código, rotas, JSON, tabelas e colunas em inglês e camelCase; mensagens ao usuário e README em português.
- **Dado pessoal nunca na URL:** buscas por CPF/CNPJ ou e-mail usam `POST …/search` com o dado no corpo.
- **Sem comentários no código:** os nomes explicam o código, e as explicações ficam neste README e em `instructions/`.

Detalhes das convenções: [instructions/arquitetura.md](instructions/arquitetura.md) e [instructions/padrao-desenvolvimento.md](instructions/padrao-desenvolvimento.md).


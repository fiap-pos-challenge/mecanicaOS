# Oficina — Sistema Integrado de Atendimento e Execução de Serviços

[![CI](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/ci.yml/badge.svg)](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/ci.yml)
[![CD](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/cd.yml/badge.svg)](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/cd.yml)

Sistema para oficinas mecânicas (back-end .NET e front-end Angular), feito no Tech Challenge da Pós Tech SOAT (Software Architecture).

Hoje o projeto tem:
- o domínio completo (clientes, veículos, serviços, peças, estoque e ordem de serviço);
- as 12 tabelas criadas por migrations;
- a API de login e de cadastro de clientes e usuários, com JWT e perfis.

Ele está pronto para o grupo criar as demais APIs.

O front-end Angular está na pasta `web/`. Por enquanto ele tem só a fundação: workspace, qualidade, testes, Docker e uma página inicial que mostra se a API está no ar.

**Front-end:** Angular 22.2 (standalone, signals, zoneless, `OnPush`), TypeScript 6 em modo estrito, Vitest + Angular Testing Library, Playwright + axe, ESLint + Prettier e nginx.

**Back-end:** .NET 10, ASP.NET Core (Controllers), SQL Server 2022, EF Core 10 (migrations), MediatR 12.5.0, FluentValidation, Mapperly, JWT + BCrypt, Swagger, xUnit, NSubstitute e Testcontainers.

---

## Passo a passo

### 1. Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 24.15 ou mais novo](https://nodejs.org/) (só para desenvolver o front-end; o Angular 22 não roda em versões anteriores)
- Ferramentas do repositório (`dotnet-ef` e ReportGenerator). Rode uma vez, na pasta do projeto:

  ```bash
  dotnet tool restore
  ```

### 2. Rodar tudo no Docker

```bash
docker compose up -d --build
```

Abra `http://localhost:4200` (front-end) e `http://localhost:8080/swagger` (API).

O compose sobe três serviços:

| Serviço | O que faz |
|---|---|
| `sqlserver` | SQL Server 2022 na porta 1433, com os dados no volume `oficina-sqlserver-data` |
| `api` | A API em `http://localhost:8080`, no ambiente Development |
| `web` | O front-end em `http://localhost:4200`, servido pelo nginx sem root. O nginx repassa `/api` para a API, então o navegador fala só com a própria origem |

Na subida, a API cria o banco `oficina`, aplica as migrations e cria o Admin de teste.

E-mails em desenvolvimento: em vez de um servidor SMTP, a API grava cada mensagem (convites) como um arquivo `.eml` na pasta configurada em `Email:PickupDirectory`. No Docker, é a pasta `.mail/` na raiz do repositório (montada em `/mail` no container); rodando pelo Visual Studio ou `dotnet run`, é `src/Oficina.Api/.mail/`. Abra o `.eml` num cliente de e-mail ou num editor de texto. A pasta está no `.gitignore`. Fora de Development, configure `Email:SmtpHost` e `Email:SmtpPort` (sem `PickupDirectory`) para usar um SMTP real; sem nenhum dos dois, o envio fica desligado. O conteúdo, o destinatário e o token do e-mail nunca vão para o log.

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

O CI também tem o job `web`: `npm ci`, lint, testes com cobertura mínima de 85%, build de produção, `npm audit --audit-level=high` e E2E. Ele roda os três projetos do back-end, gera o relatório de cobertura com o ReportGenerator e falha se a cobertura de linhas ficar abaixo de 85%. O resumo da cobertura aparece no resumo do job e como comentário no PR. O relatório em HTML fica como artefato do job.

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

### Pacote fixado por segurança

O `Directory.Packages.props` fixa `System.Security.Cryptography.Pkcs` na versão 10.0.13. A versão que o `Microsoft.Data.SqlClient` traz por dependência transitiva tem uma vulnerabilidade conhecida (aviso `NU1903` do NuGet), e o build trata avisos como erro. A fixação foi autorizada pelo responsável e pode sair quando o SqlClient trouxer uma versão corrigida.

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

O `npm start` precisa da API rodando (pelo Docker ou pelo `dotnet run`). O `npm run e2e` e o `npm run lighthouse` precisam do Docker aberto: eles constroem a imagem `web` e testam nela, com os mesmos cabeçalhos de segurança da produção. O E2E também sobe a API e um SQL Server descartáveis (veja "E2E com a API real").

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
| E2E reaproveitando uma stack já de pé | `docker compose -f web/e2e/docker-compose.e2e.yml up --build --wait` e depois `E2E_KEEP_STACK=1 npx playwright test` | |
| Acessibilidade do login (CI) | `cd web` e `npm run lighthouse` | |
| Desempenho e acessibilidade das telas autenticadas (stack do E2E de pé) | `cd web` e `npm run lighthouse:screens` | |
| Pacote de produção sem a API simulada | `cd web`, `npm run build` e `npm run check:bundle` | |
| Varredura de segredos | `cd web` e `npm run check:secrets` | |

O modo demo e o E2E usam as mesmas portas 4300/4301: não suba os dois ao mesmo tempo.

### Modo demonstração (validação manual)

No site normal, as telas sem API ficam em "Em construção". Para navegar por todas antes do merge, existe o build `demo`, que liga no próprio navegador a mesma API simulada do E2E. Uma faixa fixa "Modo demonstração — dados fictícios" aparece em todas as telas.

```bash
docker compose --profile demo up -d --build
```

Abre em `http://localhost:4300` (o `web` normal continua em `http://localhost:4200`). Entre com `admin@oficina.local` / `Admin@123`. Para desligar: `docker compose --profile demo down`. Sem Docker, com a API rodando em `http://localhost:8080`: `cd web` e `npm run start:demo`.

| Tela | Dados no modo demonstração |
|---|---|
| Login, sessão, Clientes (lista, ficha, cadastro), Equipe (lista, convite, ativar/desativar) | **Reais** (API) |
| Veículos, resumo e histórico do cliente, endereço por CEP, importação e exportação | Fictícios |
| Perfil profissional e permissões da equipe | Fictícios |
| Catálogo, Estoque e inventário | Fictícios |
| Orçamentos, Ordens de serviço, Modo mecânico, Documentos | Fictícios |
| Portal do cliente (`/o/portal-demo-3324`, confirme com `321`) e verificação de documento | Fictícios |
| Painel, Relatórios, "Pergunte aos seus dados" | Fictícios |
| Automações, filas e notificações | Fictícios |
| Configurações (reautenticação: a senha do usuário ou o código `123456`) | Fictícios |
| Tempo real (SignalR) | Desligado |

Os dados fictícios ficam só na memória do navegador e voltam ao início ao recarregar a página. Os veículos e o histórico fictícios pertencem aos clientes fictícios, não aos clientes reais cadastrados pela API.

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
- **Rotas:** todas as telas do plano carregam sob demanda e mostram "Em construção" até a fase delas. Todas exigem login e a permissão da tela (veja "Autenticação e perfis").
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
  realtime: { api: 'pending' },
  session: { api: 'pending' },
  twoFactor: { api: 'pending' },
  vehicles: { api: 'pending' },
  customerInsights: { api: 'pending' },
  addresses: { api: 'pending' },
  jobs: { api: 'pending' },
  teamProfiles: { api: 'pending' },
  roles: { api: 'pending' },
  catalog: { api: 'pending' },
  inventory: { api: 'pending' },
  quotes: { api: 'pending' },
},
```

- **Modo `pending`:** a porta responde na hora com o erro `feature.pending` (501). As telas continuam inteiras; o que depende da API mostra o bloco "Em construção" e cada ação mostra "Esta funcionalidade ainda está em construção no back-end." Nada é gravado e nenhum dado é inventado.
- **Telas mistas:** a parte com API funciona normalmente. Ex.: clientes e ficha usam a API real; veículos, histórico, gasto e situação aparecem "Em construção".

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

- Hoje `features.session.api = 'pending'`: `refresh()` devolve "sem sessão" e o fluxo acima continua igual (recarregar leva ao login).
- Contrato que o B1 deve implementar: `POST /api/auth/refresh` (lê o cookie e devolve `{ accessToken, expiresAt }`, ou 401 sem cookie válido) e `POST /api/auth/logout` (revoga o cookie, 204).
- Para trocar para a API real depois do B1: publique os dois endpoints, rode `npm run api:generate`, mude `features.session.api` para `'http'` no `environment.ts` e rode `npm test`. O `HttpSessionGateway` já chama os dois endpoints.

**Verificação em duas etapas** (`/login/verify`, pronta para o B1):

- Contrato: o login pode responder `{ requiresTwoFactor: true, challengeId }`. A tela pede o código de 6 dígitos (colar, preencher automático, setas e apagar) e chama `POST /api/auth/2fa/verify` com `{ challengeId, code }`, que devolve `{ accessToken, expiresAt }` ou 401 `identity.invalidTwoFactorCode`.
- Hoje `features.twoFactor.api = 'pending'`: o login real nunca pede o código, então a tela só aparece quando o B1 publicar o desafio.

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
- **Regra temporária:** situação, frota e retorno dependem de OS e veículos, ainda sem API: a lista mostra o aviso "Em construção" e esses filtros não encontram ninguém. Quando a API do resumo existir, o front lê a lista real em lotes de 100 (até 500 clientes), cruza com o resumo e pagina localmente, avisando quando passa de 500. Quando o B2/B3 publicarem esses campos no `/query`, o filtro passa para o servidor e o `CustomersListStore` volta a paginar só pela API.

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

**Contratos prontos para o B2/B3** (hoje em `pending`; as telas mostram "Em construção"):

| Contrato | Rotas | Erros tratados |
|---|---|---|
| Veículos (`customers/data/vehicles.contracts.ts`) | `GET /api/admin/customers/{customerId}/vehicles`, `POST /api/admin/vehicles`, `PUT` e `DELETE /api/admin/vehicles/{id}`, `POST /api/admin/vehicles/{id}/transfer`, `POST /api/admin/vehicles/search` (placa no corpo) | 400 placa inválida, 409 placa duplicada, 422 km menor, 422 mesmo dono, 409 OS aberta |
| Resumo do cliente (`insights.contracts.ts`) | `POST /api/admin/customers/summaries` (até 500 ids), `GET /api/admin/customers/{id}/service-history` | 400, 403 |
| CEP (`address-lookup.gateway.ts`) | `POST /api/addresses/lookup` | 400 CEP inválido, 404 CEP não encontrado |
| Filas (`customer-jobs.contracts.ts`) | `POST /api/admin/customers/{id}/data-export` (202), `POST /api/admin/imports/customers` (202, `{ fileName, csv }`), `GET /api/admin/jobs/{id}` | 400 arquivo inválido, 413 acima de 2 MB, 404 tarefa |

- A importação aceita CSV (`nome;documento;telefone;email`, com modelo para baixar) e mostra o relatório por linha quando a fila existir. `.xlsx` ficou de fora para não trazer biblioteca extra.
- A exportação LGPD roda em fila; quando termina, a ficha oferece o JSON para baixar.

**Permissões novas:** `customers.reveal` e `customers.import` (Admin, Manager, Attendant), `customers.export` (Admin, Manager) e `vehicles.manage` (Admin, Manager, Attendant).

### Equipe e permissões

Tela `/team` (Admin altera; Manager só lê). Usuários vêm da API real (`/api/admin/users`); perfil profissional, produtividade e permissões têm contrato pronto e mostram "Em construção" até a API sair.

- **Cartões e lista:** avatar, perfil, situação, especialidades e, para mecânicos, OS, ocupação, receita e uma sparkline de OS por semana (30 ou 90 dias). Filtros por perfil e situação vão na query da API (`role`, `status`), porque não são dados pessoais.
- **Convite:** o Admin informa nome, e-mail e perfil. A API cria o usuário sem senha e manda um link de uso único, válido por 72 horas. Ninguém, nem o Admin, vê a senha. Reenviar invalida o link anterior.
- **Página `/invite`** (pública): o token vem no fragmento (`#token=`), que não vai ao servidor nem ao `Referer`; o front lê, guarda em memória e limpa a URL. Mostra nome, perfil e e-mail mascarado, pede a senha com as regras do back-end e trata convite inexistente, expirado ou já usado.
- **Desativar e reativar** com confirmação. O botão fica desabilitado com o motivo quando é o próprio usuário ou o único administrador ativo; a API também recusa (422 `identity.cannotManageOwnAccount`, 409 `identity.lastActiveAdmin`).
- **Membro (`/team/:id`):** acesso (nome e perfil, API real), perfil profissional (especialidades, comissão, jornada por dia, limite de desconto; mecânico não aplica desconto) e produtividade (OS, horas, ocupação, receita, retrabalho e tendência).
- **Permissões (`/team/permissions`):** matriz perfil × permissão agrupada por módulo, só leitura. No celular, uma lista por perfil.

**Contratos prontos para o B2/B3** (hoje em `pending`):

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

**Contrato pronto para o B2** (hoje em `pending`; listas vazias com "Em construção" e salvar responde com a mensagem de construção) em `features/catalog/data/catalog.contracts.ts`, com base nas entidades `Service` e `Part` do domínio e os campos novos do catálogo (comissão, garantia, peças usuais, custo, fornecedor, código de barras e o pacote):

| Rota | Perfis | Observação |
|---|---|---|
| `GET /api/admin/catalog/{services\|packages\|parts}?term=&status=&page=&pageSize=` | Todos da oficina | Paginado; `cost` só para Admin e Manager |
| `GET /api/admin/catalog/{tipo}/{id}` | Todos da oficina | Pacote devolve `itemsTotal` e `savings` |
| `POST` e `PUT /api/admin/catalog/{tipo}` e `/{id}` | Admin, Manager | 400 por campo, 409 código duplicado |
| `POST /api/admin/catalog/{tipo}/{id}/deactivate` e `/reactivate` | Admin, Manager | 409 `catalog.usedByActivePackage` com `packages` |
| `GET /api/admin/catalog/{tipo}/{id}/price-history` | Todos da oficina | Mudanças de custo só para Admin e Manager |

### Estoque e inventário

Tela `/inventory` com seções em rotas: Visão geral, Inventário (`/inventory/counts`), Pedidos de compra e Fornecedores. Contrato pronto para o B3 em `features/inventory/data/inventory.contracts.ts`, sobre a entidade `Part` real (`stockQuantity`, `minimumStock`, movimentos). Hoje `inventory` está em `pending`: as telas aparecem completas, sem dados, com o aviso "Em construção".

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

Telas `/quotes` (lista) e `/quotes/:id` (montador), para Admin, Manager e Attendant. Contrato pronto para o B4 em `features/quotes/data/quote.contracts.ts`, sobre a entidade `Quote` real (versão, status `Pending`, `Approved` e `Rejected`, totais de serviços e peças e `RejectionReason`). O `ApprovalToken` nunca chega ao front: o envio devolve só o link público. Hoje `quotes` está em `pending`: as telas mostram o aviso "Em construção".

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

Telas `/service-orders` (quadro, lista e agenda), `/service-orders/:id` (detalhe) e `/mechanic` (modo mecânico, pensado para o celular). Contrato pronto para o B4/B5 em `features/service-orders/data/service-order.contracts.ts` e `features/mechanic/data/mechanic.gateway.ts`, com os status reais `Received`, `InDiagnosis`, `WaitingApproval`, `InProgress`, `Completed` e `Delivered`. Hoje `serviceOrders` está em `pending`: as telas mostram o aviso "Em construção".

- **Quadro:** seis colunas, arrastar e soltar (mouse e toque) e, como alternativa acessível, o menu "Mover para" em cada cartão, usável só com teclado. As transições espelham o domínio; durante o arraste, as colunas proibidas ficam marcadas, e soltar nelas explica o motivo (OS entregue, falta orçamento, transição inválida). Etiquetas: atrasada, sem peça e lembrete enviado.
- **Lista e agenda:** filtros por status, mecânico, só atrasadas e busca sem acento; agenda pela entrega prometida, com as atrasadas primeiro.
- **Detalhe:** linha do tempo (`StatusHistory`), orçamento vigente, peças reservadas, fotos, mecânico responsável, apontamento de tempo (iniciar e parar) e documentos.
- **Tempo real e conflito:** as mudanças chegam pelo `RealtimeGateway` (SignalR em `/api/hubs/realtime`; hoje `realtime` está em `pending`). Um 409 de versão recarrega a OS e mostra o que outro usuário mudou.
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

Rotas públicas `/o/:token` (portal da OS) e `/v/:code` (verificação de documento), fora do shell autenticado, com layout próprio de celular. Contrato pronto para o B4/B6 em `features/portal/data/portal.gateway.ts` e `features/documents/data/documents.gateway.ts`. Hoje `portal` e `documents` estão em `pending`.

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

Tela `/dashboard` (Admin, Manager e Attendant). Contrato pronto para o B7 em `features/dashboard/data/dashboard.contracts.ts`. Hoje `dashboard` está em `pending`: a tela mostra o aviso "Em construção".

- **Cartões:** indicadores (faturamento, OS abertas, orçamentos aprovados, a receber, com a variação contra o período anterior), faturamento diário (serviços e peças empilhados), "precisa de atenção", serviços mais vendidos e pátio agora (OS por situação). Período: hoje, 7 dias ou 30 dias.
- **Precisa de atenção:** cada aviso traz a ação direta (gerar pedido de compra, enviar novo prazo, enviar lembrete) e o atalho para a OS, o orçamento, o estoque ou os clientes. O que veio da IA leva o selo "Sugestão da IA" e só acontece quando a pessoa clica.
- **Painel personalizável:** "Personalizar" mostra a lista de cartões para esconder e reordenar (subir e descer, tudo pelo teclado). A preferência fica salva na conta pelo `PUT /api/admin/dashboard/layout`.

| Rota | Uso |
|---|---|
| `GET /api/admin/dashboard?period=today\|week\|month` | Indicadores, gráficos e avisos |
| `GET` e `PUT /api/admin/dashboard/layout` | Cartões escolhidos e a ordem (por usuário) |
| `POST /api/admin/dashboard/attention/{id}/actions` | Ação direta de um aviso (`CreatePurchaseOrder`, `SendNewDeadline`, `SendReminder`) |

### Relatórios

Tela `/reports` (Admin e Manager). Contrato pronto para o B7 em `features/reports/data/report.contracts.ts`. Hoje `reports` está em `pending`.

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

Tela `/automations` (Admin e Manager). Contrato pronto para o B5/B8 em `features/automations/data/automation.gateway.ts`. Hoje `automations` está em `pending`.

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

O sino da barra superior mostra quantas notificações não foram lidas e abre a central: lista com tipo e data, abrir (leva à tela ligada, só caminhos internos do próprio site), marcar uma ou todas como lidas e "Preferências" (por tipo e canal: no sistema, e-mail e WhatsApp). As novas chegam em tempo real pelo `RealtimeGateway` (evento `notification`); como reserva, a lista é consultada a cada 60 s e ao abrir o sino. A notificação nova é anunciada para leitores de tela. Contrato em `features/notifications/data/notification.models.ts`. Hoje `notifications` está em `pending`.

| Rota | Uso |
|---|---|
| `GET /api/admin/notifications` | Notificações do usuário |
| `POST /api/admin/notifications/{id}/read` e `/read-all` | Marcar como lida |
| `GET` e `PUT /api/admin/notifications/preferences` | Preferências por tipo e canal |

### Pergunte aos seus dados

No construtor de relatórios, o cartão "Pergunte aos seus dados" manda a pergunta em português (8 a 300 caracteres) para o back-end, que devolve uma definição de relatório tipada (nunca SQL) e uma explicação. O front confere a definição com `readDefinition` e `validateDefinition` antes de mostrar. A sugestão aparece com o selo da IA e só vai para o construtor com "Abrir no construtor"; o construtor mostra que o relatório veio da pergunta até a pessoa mudar algum filtro. O front nunca chama o provedor de IA. Contrato em `features/reports/data/assistant.gateway.ts` (`POST /api/admin/assistant/report-definitions`, 422 `assistant.notUnderstood`). Hoje `assistant` está em `pending`.

### Configurações e auditoria

Tela `/settings`, só para o Admin (o mapa de permissões deixa os demais perfis de fora; se um perfil tiver `settings.view` sem `settings.manage`, a tela abre somente leitura e sem a aba Segurança). Contrato pronto para o B1 e os demais em `features/settings/data/settings.contracts.ts`. Hoje `settings` está em `pending`.

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

### Contratos pendentes para o back-end

Cada linha é o contrato que o front já usa, com a API simulada de referência em `web/src/testing/fake-api/<funcionalidade>/`. Todo erro segue o ProblemDetails do projeto (`code`, `traceId`, `errors`); os códigos listados são os que o front trata de forma específica, além de 400 `common.validationFailed`, 401, 403 `common.forbidden` e 404.

| Funcionalidade (chave) | Rotas | Contrato (corpo e resposta) | Erros tratados | Prompt |
|---|---|---|---|---|
| Sessão e 2FA (`session`, `twoFactor`) | `POST /api/auth/refresh` (cookie `HttpOnly`), `POST /api/auth/2fa/verify`, `POST /api/auth/logout`, `POST /api/auth/reauthenticate` | `core/auth/session.gateway.ts`, `features/auth/data/two-factor.gateway.ts`, `features/settings/data/settings.contracts.ts` | 401 `identity.invalidTwoFactorCode`, 422 `auth.reauthenticationFailed` | B1 |
| Veículos (`vehicles`) | `GET`/`POST /api/admin/vehicles`, `GET`/`PUT`/`DELETE /vehicles/{id}`, `POST /vehicles/{id}/transfer`, `POST /vehicles/search`, `GET /api/admin/customers/{id}/vehicles` | `features/customers/data/vehicles.contracts.ts` | 409 `vehicle.duplicateLicensePlate`, 409 `vehicle.hasOpenServiceOrder` | B2 |
| Catálogo (`catalog`) | `GET`/`POST /api/admin/catalog/{kind}`, `GET`/`PUT /catalog/{kind}/{id}`, `POST /{id}/deactivate` e `/reactivate`, `GET /{id}/price-history` | `features/catalog/data/catalog.contracts.ts` | 409 `catalog.alreadyActive`/`alreadyInactive` | B2 |
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

Observação: a `develop` já entregou parte do B2 (`/api/admin/vehicles` e `/api/admin/services`). As rotas e os campos são diferentes dos contratos acima (por exemplo, serviços em `/api/admin/services` e não em `/api/admin/catalog/services`); ao ligar `vehicles` e `catalog`, ajuste os contratos do front ou combine com o grupo qual lado muda.

### Mudanças de infraestrutura e decisões registradas

- `web/playwright.config.ts` reaproveita uma stack já de pé fora do CI (`reuseExistingServer: !isCi`), e `E2E_KEEP_STACK=1` faz o `globalTeardown` não derrubar a stack. Os dois existem para iterar no E2E sem reconstruir as imagens a cada execução; no CI nada muda.
- A fila offline do mecânico sobrevive à sessão expirada (é apagada no logout e na inatividade): quem perdeu a sessão entra de novo e as ações sincronizam. A fila fica ligada ao id do usuário e é descartada se outro usuário entrar no mesmo aparelho.
- O nginx comprime com gzip os arquivos estáticos (JS, CSS, JSON, SVG e manifesto). As respostas da API não são comprimidas pelo nginx.
- O redirecionamento do pagamento com cartão no portal só aceita `https` de origens em `paymentProviderOrigins` (`environment.ts`). Em produção a lista está vazia até a fase 2 do pagamento (B6): o portal mostra um aviso em vez de redirecionar.
- O `angular.json` tem orçamento por rota (`bundle`) para as telas principais e para o ECharts.

### E2E com a API real

O `npm run e2e` sobe uma stack descartável (`web/e2e/docker-compose.e2e.yml`, projeto `oficina-e2e`, porta 4300): imagem nginx de produção, API real e SQL Server sem volume. O banco de desenvolvimento não é tocado.

- Na preparação, o teste entra como `admin@oficina.local` e cria pela API um atendente, um mecânico e um usuário desativado (senha `Oficina@2026`, somente teste).
- Cobre: login, senha errada, conta desativada, bloqueio após 5 erros, menu por perfil, rota proibida por URL, volta ao endereço pedido, logout, recarga da página e inatividade (relógio simulado).
- Clientes, contra a API real: criar, revelar, editar, buscar, excluir, documento inválido e duplicado (409), envio desabilitado sem consentimento. Veículos, resumo e histórico mostram "Em construção" e salvar veículo responde com a mensagem de construção. Tudo em desktop e celular.
- Equipe, contra a API real: convidar, ler o link no arquivo `.eml` que a API grava em `web/e2e/.mail/` (pasta montada no container e apagada no fim), criar a senha, entrar com o convidado, link já usado, e-mail repetido, desativar e reativar e o Admin sem poder se desativar. Perfil profissional e permissões mostram "Em construção".
- Catálogo e estoque sem API: abas vazias com "Em construção", formulários e diálogos completos (entrada em três modos, inventário no celular, fornecedores) respondendo com a mensagem de construção, e mecânico sem custos nem movimentação.
- Orçamentos sem API: o build do E2E usa `--configuration production,e2e` (mesma imagem nginx; o `environment.e2e.ts` só troca `quotes` para `http`) e o teste responde `/api/admin/quotes/**` com `page.route`, usando a mesma API simulada dos testes unitários (o contrato do B4). Cobre, em desktop e celular: novo orçamento a partir da OS, montador com aviso de estoque, regra automática, desconto acima do limite com pedido de aprovação, pagamento com parcelas, rascunho salvo, pré-visualização, envio que falha e é reenviado até "aberto pelo cliente", aprovação no balcão, recusa com motivo, nova versão, comparação de versões, preço alterado no catálogo e link expirado.
- Ordens de serviço, modo mecânico, portal e documentos sem API: o `environment.e2e.ts` também liga `serviceOrders`, `portal` e `documents`, e o `page.route` responde com a mesma API simulada. Cobre, em desktop e celular: arrastar no quadro (mouse no desktop, toque no celular), transição bloqueada, mover pelo teclado, lista, agenda, detalhe, conflito de versão, modo mecânico offline com a fila no IndexedDB e a sincronização na volta da rede, PWA (manifesto, service worker, abrir sem rede e cache e fila apagados no logout), portal (tela neutra para link inválido e expirado, bloqueio após 3 erros, aprovação parcial com Pix, cartão recusado na volta do provedor, recusa com motivo) e o recibo baixado e verificado.
- Painel e relatórios sem API: o `environment.e2e.ts` liga `dashboard` e `reports`, e o `page.route` responde com a mesma API simulada (contrato do B7). Cobre, em desktop e celular: indicadores e gráficos desenhados de verdade (canvas, sem erro de CSP ou Trusted Types), ação direta de um aviso, troca de período, painel personalizado e mantido ao voltar à tela, construtor com agrupamento desabilitado, combinação incompatível, período sem dados, salvar, agendar, exportar até o link de download, exportação grande na fila e excluir.
- Automações, notificações e IA sem API: o `environment.e2e.ts` liga `automations`, `notifications` e `assistant`. Cobre, em desktop e celular: pausar regra, criar a partir de um modelo com erro de validação e espera, reprocessar item da fila de erros, sino com contagem, notificação nova ao reabrir, marcar todas como lidas, preferências por canal e a pergunta em português que vira relatório no construtor (sem erro de CSP).
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

Estes valores estão no `docker-compose.yml` e no `appsettings.Development.json` só para facilitar o desenvolvimento local. **Não use em produção.**

| O quê | Valor |
|---|---|
| SQL Server (`sa`) | `Oficina@Dev123` |
| Admin de teste (criado na subida em Development, se ainda não existir) | `admin@oficina.local` / `Admin@123` |
| Chave do JWT | `Jwt:Secret` no `appsettings.Development.json` |
| API simulada dos testes do front (só `npm test`) | `admin@oficina.local`, `ana@oficina.local`, `marcos@oficina.local` e `gestor@oficina.local`, todos com `Admin@123` |

Fora de Development, a API não cria o Admin de teste e não publica o Swagger. A chave do JWT deve vir da variável de ambiente `Jwt__Secret`, com no mínimo 32 caracteres.

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
| `GET /health` | Anônimo | 200 `Healthy` |

Documento, e-mail e telefone de cliente sempre saem mascarados nas respostas; o valor aberto só sai pelo `/reveal`, com auditoria.

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
- veículos;
- serviços;
- peças e estoque;
- ordens de serviço (abertura, diagnóstico, orçamento, aprovação, execução, finalização e entrega);
- consulta pública da OS pelo cliente;
- métricas.

Siga o padrão de Customers e Users: command ou query, validator e handler na Application; controller fino na API; testes nos três projetos.

# Oficina — Sistema Integrado de Atendimento e Execução de Serviços

[![CI](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/ci.yml/badge.svg)](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/ci.yml)
[![CD](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/cd.yml/badge.svg)](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/cd.yml)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=fiap-pos-challenge_mecanicaOS&metric=alert_status)](https://sonarcloud.io/project/overview?id=fiap-pos-challenge_mecanicaOS)

Back-end de uma oficina mecânica, feito no Tech Challenge da Pós Tech SOAT (Software Architecture), Fase 1.

A API cobre:
- cadastro de clientes, veículos, serviços, peças e insumos, e controle de estoque;
- o ciclo completo da ordem de serviço: abertura, diagnóstico, orçamento, aprovação, execução, finalização e entrega;
- o acompanhamento da OS pelo cliente e a aprovação ou reprovação do orçamento por link;
- a métrica de tempo médio de execução;
- login com JWT e perfis (Admin, Attendant e Mechanic).

**Stack:** .NET 10, ASP.NET Core, SQL Server 2022, EF Core 10, MediatR, FluentValidation, Mapperly, JWT + BCrypt, Swagger, xUnit, NSubstitute e Testcontainers.

**Arquitetura:** monolito em camadas (Api → Application → Domain ← Infrastructure), com DDD, CQRS e eventos de domínio gravados em Outbox. Detalhes em [Padrões](#padrões) e na [documentação DDD](#documentação-ddd).

## Integrantes

| Nome | RM |
|---|---|
| Gustavo Rene Dias Boamorte | RM377251 |
| Gustavo Bertonsin Silva | RM377609 |
| Alessandro José Dionisio Filho | RM378128 |
| José Eduardo Lehmam Smith | RM378239 |

**Vídeo da entrega:** `<link do YouTube>` · **PDF de entrega:** [docs/entrega/entrega-fase1.pdf](docs/entrega/entrega-fase1.pdf)

---

## Como executar

Só é preciso ter o **[Docker Desktop](https://www.docker.com/products/docker-desktop/)** instalado e aberto. Não precisa instalar .NET nem SQL Server.

**1. Baixe o projeto**

```bash
git clone https://github.com/fiap-pos-challenge/mecanicaOS.git
cd mecanicaOS
```

**2. Crie o arquivo `.env`**

A senha do SQL Server e a chave do JWT não ficam no repositório. Copie o modelo e, se quiser, troque os valores:

```bash
cp .env.example .env        # no PowerShell: Copy-Item .env.example .env
```

O `.env` não é versionado (está no `.gitignore`), e o `docker compose` lê o arquivo sozinho. Sem ele, a subida para com a mensagem `Defina MSSQL_SA_PASSWORD no .env`.

**3. Suba a aplicação**

```bash
docker compose up -d --build
```

A primeira execução leva alguns minutos, porque o Docker baixa as imagens do SQL Server e do .NET e compila a API. Na subida, a API cria o banco, aplica as migrations e cria o Admin de teste e os dados de exemplo.

**4. Abra o Swagger**

Acesse **http://localhost:8080/swagger** (porta **8080**, a da API rodando no Docker).

Se a página ainda não abrir, espere cerca de 30 segundos (o SQL Server demora um pouco para ficar pronto) e recarregue.

**5. Faça login**

1. Em `POST /api/auth/login`, clique em **Try it out** e envie:

   ```json
   { "email": "admin@oficina.local", "password": "Admin@123" }
   ```

2. Copie o `accessToken` da resposta.
3. Clique em **Authorize**, no topo da página, cole o token e confirme.

**6. Execute o fluxo completo**

No Swagger, a ordem do fluxo de uma OS é:
1. cadastrar o cliente (`POST /api/admin/customers`) e o veículo (`POST /api/admin/vehicles`);
2. abrir a OS (`POST /api/admin/service-orders`);
3. iniciar o diagnóstico;
4. incluir serviços e peças;
5. enviar o orçamento (`quote/send`), que devolve o token do link;
6. aprovar pelo link (`POST /api/public/quotes/{token}/approve`);
7. finalizar (`complete`);
8. entregar (`deliver`).

O cliente acompanha a OS em `POST /api/public/service-orders/lookup`, e o tempo médio fica em `GET /api/admin/metrics/average-execution-time`.

**Postman ou Insomnia (recomendado)**

Importe [docs/postman/oficina-fluxo-completo.postman_collection.json](docs/postman/oficina-fluxo-completo.postman_collection.json). A pasta **Workflow** executa, em 27 passos e na ordem do enunciado, o fluxo de um chamado: login, cadastro do cliente e do veículo, abertura da OS, diagnóstico, serviços e peças, orçamento, aprovação pelo link, finalização, entrega, consulta do cliente e tempo médio. Rode só essa pasta (no Postman, **Run folder**). As demais pastas são a suíte de testes, com casos de erro. Cada passo guarda token e ids e confere o resultado.

A variável `baseUrl` vem como `http://localhost:8080` (Docker); troque para `http://localhost:5080` se rodar com `dotnet run`. O login permite 5 tentativas por minuto e a pasta Workflow faz 3, então espere um minuto entre duas execuções seguidas.

**Para parar**

```bash
docker compose down      # para tudo e mantém os dados
docker compose down -v   # para tudo e apaga o banco (na próxima subida ele é recriado vazio)
```

### Se algo der errado

| Sintoma | O que fazer |
|---|---|
| `Cannot connect to the Docker daemon` ou erro parecido | Abra o Docker Desktop e espere ele terminar de iniciar. |
| `port is already allocated` (porta 8080 ou 1433) | Outro programa usa a porta. Pare esse programa, ou troque a porta da esquerda no `docker-compose.yml` (por exemplo, `"8081:8080"` e abra `http://localhost:8081/swagger`). |
| `required variable MSSQL_SA_PASSWORD is missing a value` | Falta o `.env`. Rode `cp .env.example .env` e suba de novo. |
| `container oficina-sqlserver is unhealthy` | Normalmente é um banco de uma execução antiga com outra senha, ou uma senha fraca no `.env` (o SQL Server exige 8 caracteres, com maiúscula, minúscula, número e símbolo). Rode `docker compose down -v` e suba de novo. |
| Mac com chip Apple (M1, M2…) e o SQL Server não sobe | No Docker Desktop, ative **Settings → General → Use Rosetta for x86_64/amd64 emulation** e suba de novo. |
| Ver o que a API está fazendo | `docker compose logs -f api` |

---

## Dados de exemplo

Na primeira subida em Development, a API também cria dados para você testar sem cadastrar nada. Se o cliente `39053344705` já existir, nada é recriado.

| O quê | Dados |
|---|---|
| Usuários | Attendant `ana@oficina.local` e Mechanic `caio@oficina.local`, ambos com a senha `Oficina@2026` |
| Clientes e veículos | João Pereira (CPF `39053344705`, placa `QWE4R56`), Carla Mendes (CPF `11144477735`, placa `RIO2A34`) e Transportes Silva Ltda (CNPJ `11222333000181`, placa `FRT7B89`) |
| Serviços | Alinhamento e balanceamento, troca de pastilhas, revisão completa e diagnóstico eletrônico |
| Peças e insumos | `OLE-5W30`, `FLT-AR01`, `PAS-DIA01`, `VEL-IGN01` (estoque abaixo do mínimo, aparece em `belowMinimum=true`) e `ADT-RAD01` |

Para recriar tudo do zero, rode `docker compose down -v` e suba de novo.

## Autenticação e usuários de demonstração

As rotas administrativas exigem um JWT. Faça login em `POST /api/auth/login`, copie o `accessToken` e envie no cabeçalho `Authorization: Bearer <token>` (no Swagger, botão **Authorize**). O token vale 60 minutos.

Os usuários abaixo só existem em Development e servem apenas para demonstração. **Não use em produção.**

| Perfil | E-mail | Senha |
|---|---|---|
| Admin | `admin@oficina.local` | `Admin@123` |
| Attendant | `ana@oficina.local` | `Oficina@2026` |
| Mechanic | `caio@oficina.local` | `Oficina@2026` |

**Segredos:** a senha do SQL Server (`MSSQL_SA_PASSWORD`) e a chave do JWT (`JWT_SECRET`) vêm do `.env`, que não é versionado. O modelo é o [.env.example](.env.example). Para conectar no SSMS ou no Azure Data Studio, use:
- servidor `localhost,1433`;
- usuário `sa`;
- a senha do seu `.env`;
- banco `oficina`;
- a opção "Confiar no certificado do servidor" marcada.

Fora de Development, a API não cria os usuários de demonstração e não publica o Swagger. A chave do JWT vem da variável de ambiente `Jwt__Secret` (mínimo de 32 caracteres), e a conexão vem de `ConnectionStrings__Oficina`.

---

## Banco de dados: SQL Server 2022

**Decisão:** banco relacional, SQL Server 2022 (imagem oficial `mcr.microsoft.com/mssql/server:2022-latest`, edição Developer).

**Justificativa:**

- **Transações ACID nos fluxos críticos.** A aprovação do orçamento muda o status da OS e baixa o estoque das peças no mesmo commit (`QuoteApprovalPolicy` + `UnitOfWork`). Os eventos de domínio vão para a tabela de Outbox na mesma transação, então nunca existe evento sem o dado, nem dado sem o evento.
- **Concorrência otimista nativa.** `ServiceOrder` e `Part` têm coluna `rowversion`. Se duas aprovações simultâneas, ou duas baixas da mesma peça, chegarem juntas, a segunda recebe 409 e o estoque não fica negativo.
- **Regras garantidas pelo banco.** Índices únicos filtrados impedem:
  - duas OS abertas para o mesmo veículo (`status <> 'Delivered'`);
  - CPF/CNPJ, placa, código de peça ou e-mail duplicados.

  O filtro `deletedAt IS NULL` evita conflito com registros excluídos (soft delete).
- **Número da OS sequencial e legível**, gerado por uma `SEQUENCE` sem corrida entre requisições.
- **Modelo naturalmente relacional.** Os dados formam a cadeia cliente → veículos → ordens de serviço → itens, orçamento e histórico de status. Há integridade referencial, e as listagens e a métrica de tempo médio usam junções e paginação (`OFFSET/FETCH`).
- **Ecossistema .NET.**
  - O provedor do EF Core é maduro: migrations, `rowversion`, sequences e índices filtrados ficam mapeados direto no modelo.
  - Há imagem oficial para Docker.
  - O Testcontainers permite rodar os testes funcionais com o banco real.

**Alternativas consideradas:**
- **PostgreSQL:** foi usado nas primeiras etapas e é equivalente em recursos. Foi trocado pelo SQL Server para alinhar com a experiência do grupo e com o ferramental .NET (SSMS e Azure Data Studio).
- **NoSQL:** descartado, porque o fluxo da OS depende de transação entre agregados (OS e estoque) e de restrições de unicidade.

---

## Endpoints

Perfis: **Admin** (administrador), **Attendant** (atendente) e **Mechanic** (mecânico). "Todos" significa qualquer usuário logado.

**Autenticação**

| Método e rota | Perfis |
|---|---|
| `POST /api/auth/login` | Anônimo (5 tentativas por minuto por IP) |
| `GET /api/auth/me` | Todos |

**Clientes, veículos e usuários**

| Método e rota | Perfis |
|---|---|
| `GET` / `POST /api/admin/customers` | Admin, Attendant |
| `GET` / `PUT /api/admin/customers/{id}` | Admin, Attendant |
| `DELETE /api/admin/customers/{id}` | Admin |
| `POST /api/admin/customers/search` (CPF/CNPJ no corpo) | Admin, Attendant |
| `GET /api/admin/vehicles` e `GET /api/admin/vehicles/{id}` | Todos |
| `POST /api/admin/vehicles` e `PUT /api/admin/vehicles/{id}` | Admin, Attendant |
| `DELETE /api/admin/vehicles/{id}` | Admin |
| `GET` / `POST /api/admin/users`, `GET` / `PUT` / `DELETE /api/admin/users/{id}` | Admin |
| `POST /api/admin/users/{id}/deactivate` e `/reactivate` | Admin |
| `POST /api/admin/users/search` (e-mail no corpo) | Admin |

**Catálogo de serviços, peças e estoque**

| Método e rota | Perfis |
|---|---|
| `GET /api/admin/services` e `GET /api/admin/services/{id}` | Todos |
| `POST /api/admin/services` e `PUT /api/admin/services/{id}` | Admin |
| `POST /api/admin/services/{id}/deactivate` e `/reactivate` | Admin |
| `GET /api/admin/parts` (filtro `belowMinimum=true`) e `GET /api/admin/parts/{id}` | Todos |
| `POST /api/admin/parts` e `PUT /api/admin/parts/{id}` | Admin |
| `POST /api/admin/parts/{id}/stock-entries` (entrada de estoque) | Admin |
| `POST /api/admin/parts/{id}/deactivate` e `/reactivate` | Admin |

**Ordem de serviço**

| Método e rota | Perfis |
|---|---|
| `GET /api/admin/service-orders` e `GET /api/admin/service-orders/{id}` | Todos |
| `POST /api/admin/service-orders` (abrir, com CPF/CNPJ e placa no corpo) | Admin, Attendant |
| `POST /api/admin/service-orders/{id}/diagnosis/start` | Admin, Mechanic |
| `POST /api/admin/service-orders/{id}/services` e `/parts` (incluir itens) | Admin, Mechanic |
| `DELETE /api/admin/service-orders/{id}/items/{itemId}` | Admin, Mechanic |
| `POST /api/admin/service-orders/{id}/quote/send` (gera o orçamento e o token do link) | Admin, Attendant |
| `POST /api/admin/service-orders/{id}/quote/approve` e `/quote/reject` (decisão no balcão) | Admin, Attendant |
| `POST /api/admin/service-orders/{id}/complete` | Admin, Mechanic |
| `POST /api/admin/service-orders/{id}/deliver` | Admin, Attendant |
| `GET /api/admin/metrics/average-execution-time` | Admin |

**Rotas públicas (cliente)**

| Método e rota | Perfis |
|---|---|
| `POST /api/public/service-orders/lookup` (número da OS e CPF/CNPJ no corpo) | Anônimo |
| `POST /api/public/quotes/{token}/approve` e `/reject` | Anônimo |
| `GET /health` | Anônimo |

Os erros seguem o ProblemDetails (`application/problem+json`), com `title`, `status`, `detail` (em português), `code`, `traceId` e, na validação, `errors` com cada campo. Status usados: 400 (dados inválidos), 401 (sem login), 403 (perfil sem permissão), 404, 409 (conflito ou transição inválida), 422 (regra de negócio), 429 e 500.

---

## Para desenvolvedores

Pré-requisitos: Docker Desktop, [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) e as ferramentas do repositório (`dotnet tool restore`, uma vez).

**Rodar a API fora do Docker**

O .NET não lê o `.env` sozinho, então carregue os valores no terminal antes do `dotnet run`:

```bash
# bash / Git Bash
docker compose up -d sqlserver
set -a; . ./.env; set +a
export ConnectionStrings__Oficina="Server=localhost,1433;Database=oficina;User Id=sa;Password=$MSSQL_SA_PASSWORD;Encrypt=True;TrustServerCertificate=True"
export Jwt__Secret="$JWT_SECRET"
dotnet run --project src/Oficina.Api
```

```powershell
# PowerShell
docker compose up -d sqlserver
Get-Content .env | Where-Object { $_ -match '^[A-Z_]+=' } | ForEach-Object { $k, $v = $_ -split '=', 2; Set-Item "env:$k" $v }
$env:ConnectionStrings__Oficina = "Server=localhost,1433;Database=oficina;User Id=sa;Password=$env:MSSQL_SA_PASSWORD;Encrypt=True;TrustServerCertificate=True"
$env:Jwt__Secret = $env:JWT_SECRET
dotnet run --project src/Oficina.Api
```

Rodando assim, a API usa a porta **5080**, e o Swagger fica em `http://localhost:5080/swagger`. Pelo Docker (`docker compose up`), a porta é a **8080**.

No Visual Studio (2026 ou 2022 17.14+):
1. Abra `Oficina.slnx` e defina `Oficina.Api` como projeto de inicialização.
2. Em **Propriedades → Depurar → Variáveis de ambiente**, informe `ConnectionStrings__Oficina` e `Jwt__Secret` com os valores do seu `.env`. Essa configuração fica no `launchSettings.json`; não faça commit dela.
3. Aperte **F5**.

Para usar um SQL Server local com autenticação do Windows, use como `ConnectionStrings__Oficina` o valor `Server=.\SQLEXPRESS;Database=oficina;Trusted_Connection=True;TrustServerCertificate=True`.

**Migrations**

Em Development, elas são aplicadas na subida da API. Para criar uma nova:

```bash
dotnet ef migrations add NomeDaMigration --project src/Oficina.Infrastructure --startup-project src/Oficina.Api --output-dir Persistence/Migrations
```

**Testes**

```bash
dotnet test --project tests/Oficina.UnitTests
dotnet test --project tests/Oficina.ContractTests
dotnet test --project tests/Oficina.FunctionalTests   # precisa do Docker aberto (Testcontainers)
```

O CI roda os três projetos e falha se a cobertura de linhas ficar abaixo de 85%. O relatório de cobertura aparece no resumo de cada execução (aba **Actions**) e no artefato `coverage-report`. Depois de mudar uma rota ou um DTO, atualize o snapshot do OpenAPI com `UPDATE_OPENAPI_SNAPSHOT=1 dotnet test --project tests/Oficina.ContractTests` (no PowerShell, `$env:UPDATE_OPENAPI_SNAPSHOT=1` antes do comando).

---

## CI/CD e análise de vulnerabilidades

| Workflow | Quando roda | O que faz |
|---|---|---|
| [CI](.github/workflows/ci.yml): `build-and-test` | Push e PR em `develop`, `develop-new` e `main` | Build com `-warnaserror`, testes unitários, de contrato e funcionais, cobertura mínima de 85% e análise do **SonarQube Cloud** com Quality Gate |
| [CI](.github/workflows/ci.yml): `vulnerability-scan` | Push e PR em `develop`, `develop-new` e `main` | `dotnet list package --vulnerable --include-transitive`. Falha se algum pacote NuGet tiver vulnerabilidade conhecida |
| [CD](.github/workflows/cd.yml) | Push em `main` e tags `v*` | Publica a imagem no GHCR e faz o smoke test do `docker compose` com `/health` |
| [Dependabot](.github/dependabot.yml) | Toda semana | PRs de atualização de NuGet, imagens Docker e GitHub Actions |

O resultado da análise está em [docs/seguranca/analise-vulnerabilidades.md](docs/seguranca/analise-vulnerabilidades.md) e no painel do [SonarQube Cloud](https://sonarcloud.io/project/overview?id=fiap-pos-challenge_mecanicaOS).

**Configuração do SonarQube Cloud (uma vez só):**
1. Em https://sonarcloud.io, entre com o GitHub e importe a organização `fiap-pos-challenge` e o repositório `mecanicaOS`.
2. Escolha **With GitHub Actions** como método de análise (desligue a *Automatic Analysis*) e gere o token.
3. No GitHub, em **Settings → Secrets and variables → Actions**, cadastre o token como secret `SONAR_TOKEN`.
4. Se a chave do projeto ou a organização forem diferentes de `fiap-pos-challenge_mecanicaOS` e `fiap-pos-challenge`, crie as variables `SONAR_PROJECT_KEY` e `SONAR_ORGANIZATION`.

Sem o secret, o CI pula as etapas do Sonar, e o resto continua rodando.

---

## Documentação DDD

- **Board no Miro (acesso sem pedir permissão):** https://miro.com/app/board/uXjVESyPi8o=/?share_link_id=587184665645
- **Exportação no repositório:** [docs/ddd/](docs/ddd/README.md), com:
  - o [Event Storming](docs/ddd/event-storming.pdf) da criação da OS, do acompanhamento e da gestão de peças e insumos;
  - os [diagramas](docs/ddd/diagramas.pdf): subdomínios, mapa de contextos, modelo de domínio, Domain Storytelling e C4;
  - a [linguagem ubíqua](docs/ddd/linguagem-ubiqua.md);
  - a [rastreabilidade](docs/ddd/rastreabilidade.md) entre o Event Storming e o código.
- **Análise de código e cobertura:** https://sonarcloud.io/project/overview?id=fiap-pos-challenge_mecanicaOS
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
docs/
  ddd/                      Event Storming, diagramas, linguagem ubíqua e rastreabilidade (PDF e fontes)
  seguranca/                resultado da análise de vulnerabilidades
  entrega/                  PDF de entrega e a fonte HTML
  postman/                  coleção com o fluxo completo e a suíte de testes
  enunciado/                enunciado do Tech Challenge
.github/                    workflows de CI/CD e Dependabot
instructions/               convenções do grupo (arquitetura e padrão de desenvolvimento)
```

## Padrões

- **Camadas:** a API chama a Application pelo MediatR. A Application usa o domínio e as interfaces, e a Infrastructure implementa as interfaces. O domínio não depende de ninguém.
- **CQRS:** um command ou query por caso de uso, cada um com validator (FluentValidation) e handler.
- **Result pattern:** erros esperados voltam como `Result` com um `Error` dos catálogos (`…Errors`), com código estável e mensagem em português. Exceção só para erro inesperado.
- **Soft delete e auditoria:** "excluir" preenche `deletedAt`. As colunas `createdAt` e `updatedAt` são preenchidas por um interceptor do EF Core. As consultas ignoram os registros excluídos.
- **Desativar não é excluir:** desativar (usuários, serviços e peças) só muda `isActive` para `false`. O registro continua consultável e pode ser reativado, mas não pode ser incluído em novas ordens de serviço.
- **Nomes:** código, rotas, JSON, tabelas e colunas em inglês e camelCase; mensagens ao usuário e README em português.
- **Dado pessoal nunca na URL:** buscas por CPF/CNPJ ou e-mail usam o corpo da requisição.
- **Sem comentários no código:** os nomes explicam o código, e as explicações ficam neste README e em `instructions/`.

Detalhes das convenções: [instructions/arquitetura.md](instructions/arquitetura.md) e [instructions/padrao-desenvolvimento.md](instructions/padrao-desenvolvimento.md).

# Oficina — Sistema Integrado de Atendimento e Execução de Serviços

[![CI](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/ci.yml/badge.svg)](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/ci.yml)
[![CD](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/cd.yml/badge.svg)](https://github.com/fiap-pos-challenge/mecanicaOS/actions/workflows/cd.yml)

Back-end de uma oficina mecânica, feito no Tech Challenge da Pós Tech SOAT (Software Architecture), Fase 1.

A API cobre:
- cadastro de clientes, veículos, serviços, peças e insumos, e controle de estoque;
- o ciclo completo da ordem de serviço: abertura, diagnóstico, orçamento, aprovação, execução, finalização e entrega;
- o acompanhamento da OS pelo cliente e a aprovação ou reprovação do orçamento por link;
- a métrica de tempo médio de execução;
- login com JWT e perfis (Admin, Attendant e Mechanic).

**Stack:** .NET 10, ASP.NET Core, SQL Server 2022, EF Core 10, MediatR, FluentValidation, Mapperly, JWT + BCrypt, Swagger, xUnit, NSubstitute e Testcontainers.

---

## Como executar

Só é preciso ter o **[Docker Desktop](https://www.docker.com/products/docker-desktop/)** instalado e aberto. Não precisa instalar .NET nem SQL Server.

**1. Baixe o projeto**

```bash
git clone https://github.com/fiap-pos-challenge/mecanicaOS.git
cd mecanicaOS
```

**2. Suba a aplicação**

```bash
docker compose up -d --build
```

A primeira execução leva alguns minutos, porque o Docker baixa as imagens do SQL Server e do .NET e compila a API. Na subida, a API cria o banco, aplica as migrations e cria o Admin de teste e os dados de exemplo.

**3. Abra o Swagger**

Acesse **http://localhost:8080/swagger**.

Se a página ainda não abrir, espere cerca de 30 segundos (o SQL Server demora um pouco para ficar pronto) e recarregue.

**4. Faça login**

1. Em `POST /api/auth/login`, clique em **Try it out** e envie:

   ```json
   { "email": "admin@oficina.local", "password": "Admin@123" }
   ```

2. Copie o `accessToken` da resposta.
3. Clique em **Authorize**, no topo da página, cole o token e confirme.

**5. Execute o fluxo completo**

O roteiro [docs/roteiro-swagger.md](docs/roteiro-swagger.md) mostra, passo a passo e com os payloads prontos, o fluxo de uma OS do cadastro do cliente até a entrega do veículo.

**Alternativa: Postman ou Insomnia**

Importe [docs/postman/oficina-fluxo-completo.postman_collection.json](docs/postman/oficina-fluxo-completo.postman_collection.json) e rode a collection inteira, em ordem (no Postman, **Run collection**). Ela executa o fluxo completo da OS, guarda token e ids entre as requisições e confere o resultado de cada passo. A variável `baseUrl` vem como `http://localhost:5080`; troque para `http://localhost:8080` se estiver usando o Docker. O login permite 5 tentativas por minuto, e cada execução faz 3, então espere um minuto entre duas execuções seguidas.

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
| `container oficina-sqlserver is unhealthy` | Normalmente é um banco de uma execução antiga com outra senha. Rode `docker compose down -v` e suba de novo. |
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

## Credenciais (somente desenvolvimento)

Estes valores ficam no `docker-compose.yml` e no `appsettings.Development.json` só para facilitar a execução local. **Não use em produção.**

| O quê | Valor |
|---|---|
| Admin de teste | `admin@oficina.local` / `Admin@123` |
| Atendente e mecânico de teste | `ana@oficina.local` e `caio@oficina.local` / `Oficina@2026` |
| SQL Server | servidor `localhost,1433`, usuário `sa`, senha `Oficina@Dev123`, banco `oficina` |

Para conectar no SSMS ou no Azure Data Studio, marque "Confiar no certificado do servidor".

Fora de Development, a API não cria o Admin de teste e não publica o Swagger. A chave do JWT deve vir da variável de ambiente `Jwt__Secret`, com no mínimo 32 caracteres.

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

```bash
docker compose up -d sqlserver
dotnet run --project src/Oficina.Api
```

O Swagger fica em `http://localhost:5080/swagger`. No Visual Studio (2026 ou 2022 17.14+), abra `Oficina.slnx`, defina `Oficina.Api` como projeto de inicialização e aperte **F5**. A conexão fica em `src/Oficina.Api/appsettings.Development.json`. Para usar um SQL Server local com autenticação do Windows, troque a `ConnectionStrings:Oficina` por `Server=.\SQLEXPRESS;Database=oficina;Trusted_Connection=True;TrustServerCertificate=True`.

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

O CI roda os três projetos e falha se a cobertura de linhas ficar abaixo de 85%. Depois de mudar uma rota ou um DTO, atualize o snapshot do OpenAPI com `UPDATE_OPENAPI_SNAPSHOT=1 dotnet test --project tests/Oficina.ContractTests` (no PowerShell, `$env:UPDATE_OPENAPI_SNAPSHOT=1` antes do comando).

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
docs/                       roteiro do fluxo no Swagger e enunciado
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

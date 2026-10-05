# Padrão de desenvolvimento — Oficina MVP

Regras que todo desenvolvedor do grupo segue. As decisões de arquitetura estão em [arquitetura.md](arquitetura.md).

---

## 1. Idioma

| Onde | Idioma |
|---|---|
| Código (classes, métodos, namespaces), rotas, banco e nomes de teste | Inglês |
| Commits, README, documentação DDD e mensagens exibidas ao usuário (só nos catálogos `…Errors`, `…Messages` e `ProblemTitles`) | Português |

## 2. Regras de código

| Regra | Como é garantida |
|---|---|
| `CancellationToken` em todo método async, como último parâmetro | `CA2016` e `CA1068` tratados como erro |
| Sem `ArgumentNullException.ThrowIfNull` em construtores | `BannedSymbols.txt` |
| Sem `throw new NotImplementedException()` | `BannedSymbols.txt` |
| Sem `DateTime.Now` e `DateTime.UtcNow` (usar `TimeProvider`) | `BannedSymbols.txt` |
| Sem `Math.Round` e `MidpointRounding` (arredondamento manual no `Money`) | `BannedSymbols.txt` |
| Sem collection expressions | `.editorconfig` (`IDE0300` a `IDE0305` desligados) |
| Sem comentários `TODO` | Checagem no CI |
| Sem arquivos `.gitkeep` | Checagem no CI |
| Log com template estruturado, sem interpolação | `CA2254` tratado como erro |
| Nullable ligado e warnings como erro | `Directory.Build.props` |
| **Zero comentários** em `.cs` (`//`, `/* */`, `///`) e `.sql` (`--`, `/* */`), exceto nas migrations geradas pelo EF Core (`Migrations/`) | Revisão no PR (Etapa 3) |

**Sobre comentários (Etapa 3):** o código se explica pelos nomes. As explicações vão para o README. Arquivos de configuração podem ter comentários (`.gitignore`, `.gitattributes`, `.editorconfig`, workflows, `Dockerfile`, `docker-compose.yml`, `.props` e `.csproj`). O Swagger é documentado por metadados de endpoint (`[EndpointSummary]`, `[EndpointDescription]`, `[Tags]`, `[ProducesResponseType]`), e não por XML comments.

**Sobre dados sigilosos:** nunca logar senha, hash, JWT, token de orçamento, CPF/CNPJ, e-mail, telefone, placa ou corpo de requisição. Logar apenas IDs, número da OS e `traceId`.

**Nenhum dado pessoal na URL (Etapa 2):** CPF/CNPJ, placa, e-mail e telefone nunca vão em parâmetro de rota ou de query, nem em rota autenticada. A URL vaza em logs de proxy, no histórico do navegador e em ferramentas de monitoramento. Buscas por esses dados usam `POST` com o dado no corpo (ex.: `POST /api/admin/customers/search`, `POST /api/public/service-orders/lookup`). Um teste de contrato falha se alguma rota tiver parâmetro chamado `document`, `plate`, `licensePlate`, `email` ou `phone`.

## 3. Estilo

- Namespace por arquivo e um tipo por arquivo, com o nome do arquivo igual ao do tipo.
- Classes `sealed` por padrão.
- Primary constructors em use cases, repositórios e controllers.
- `record` para Commands e DTOs.
- Value objects são `sealed class` com construtor privado e método `Create`.
- Campos privados como `_camelCase`. Métodos assíncronos com sufixo `Async`.
- `var` quando o tipo é óbvio pela linha.
- Nada de `.Result` ou `.Wait()`.
- O operador `!` (null-forgiving) só onde o compilador não consegue provar o que o código garante. O motivo fica registrado no README (não há comentários no código).
- Sem inicializador na declaração de propriedades (`= string.Empty`, `= new()`, `= 0`, `= null!`, `= default`). Obrigatório usa tipo não anulável, opcional usa tipo anulável, e o estado interno é criado em todos os construtores.

## 4. Nomes

| Artefato | Padrão | Exemplo |
|---|---|---|
| Command | `<Verbo><Substantivo>Command` | `SendEstimateCommand` |
| Use case | `<Verbo><Substantivo>UseCase`, com método `ExecuteAsync` | `SendEstimateUseCase` |
| Queries | `I<Agregado>Queries` | `IServiceOrderQueries` |
| DTO | `<Nome>Dto` | `ServiceOrderDetailsDto` |
| Repositório | `I<Agregado>Repository`, só para raiz de agregado | `IPartRepository` |
| Evento de domínio | Particípio passado | `EstimateApproved` |
| Handler de evento | `<Ação>On<Evento>` | `DeductStockOnEstimateApproved` |
| Configuração EF | `<Entidade>Configuration` | `ServiceOrderConfiguration` |
| Exceção | Sufixo `Exception` | `InsufficientStockException` |
| Classe de teste | `<Classe>Tests` | `ServiceOrderTests` |
| Método de teste | `Method_Scenario_ExpectedResult` | `ApproveEstimate_WhenStockIsInsufficient_ThrowsInsufficientStockException` |

O sufixo `Service` é proibido em classes técnicas, porque `Service` é uma entidade do domínio (o serviço do catálogo).

## 5. Mapperly

- Usado só para converter domínio em DTO e para projeções de query (`IQueryable`).
- Nunca usado para converter DTO ou Command em agregado. Agregados são criados só pelos métodos de fábrica.
- Os mappers ficam na Application (`<Contexto>Mapper.cs`) e em `Infrastructure/Queries`.
- Propriedade não mapeada (`RMG012`, `RMG020`) gera erro de compilação.
- Value objects são convertidos por método explícito no mapper.

## 6. Domínio rico

- Nenhum setter público.
- Criação só por métodos de fábrica ou construtor que valida.
- Métodos com nome de intenção. Não existe `Update(dto)`.
- Todo conceito com regra vira value object.
- O use case não tem `if` de regra de negócio.
- Mensagens das exceções de domínio em português.

## 7. Testes

| Tipo | O que cobre |
|---|---|
| Unitário de Domain | Cada transição de status, válida e inválida. Value objects. Estoque. Arredondamento do `Money` |
| Unitário de Application | Só a orquestração, com repositórios mockados via NSubstitute |
| Contrato (`Oficina.ContractTests`) | Snapshot do OpenAPI, formato do JSON, ProblemDetails, "toda rota exige autorização" e "nenhum dado pessoal na URL" |
| Funcional (`Oficina.FunctionalTests`) | Fluxos ponta a ponta pela API e persistência com SQL Server real: fluxo da OS, rollback por falta de estoque, reprovação, 401 e 403 por perfil, e cada saída de erro conferindo o `code` do ProblemDetails |

- Assert puro do xUnit. Não usar FluentAssertions nem Shouldly.
- Estrutura AAA (Arrange, Act, Assert), com um comportamento por teste.
- Test Data Builders em `Builders/`, por exemplo `ServiceOrderBuilder`.
- Testes funcionais com um container SQL Server (Testcontainers MsSql) por collection e limpeza do banco com Respawn entre os testes.
- Os testes criam os próprios dados e não dependem de dados inseridos manualmente.
- **A cobertura mínima é de 85% em linhas (Etapa 3), somando os testes unitários, de contrato e funcionais, medida sobre `src/`**, com gate no CI. Só `Migrations/` e `Program.cs` podem ser excluídos. Nunca se criam exclusões nem testes vazios para subir o número.

## 8. Ambiente

- `global.json` fixa a versão do SDK do .NET 10.
- `.config/dotnet-tools.json` lista as ferramentas locais (ex.: `reportgenerator`). Instale com `dotnet tool restore`.
- Pacotes NuGet ficam centralizados em `Directory.Packages.props`, só em versões estáveis. Adicionar pacote novo exige aprovação do grupo no PR.
- Em desenvolvimento, as credenciais locais (senha do `sa`, chave do JWT e Admin de teste) ficam versionadas no `docker-compose.yml` e no `appsettings.Development.json`, marcadas no README como "somente desenvolvimento". Fora de Development, segredos só por variável de ambiente.
- Finais de linha em LF, controlados pelo `.gitattributes` (`* text=auto eol=lf`).
- Quando o modelo EF mudar, gere uma nova migration (`dotnet dotnet-ef migrations add <Nome> --project src/Oficina.Infrastructure --startup-project src/Oficina.Api --output-dir Persistence/Migrations`). A API aplica as migrations na subida quando `Database__ApplyMigrationsOnStartup=true`.

## 9. Git flow

```
main ◄── release/x.y ◄── develop ◄── feature/E6-03-open-service-order
  ▲                         ▲
  └──────── hotfix/... ─────┘
```

| Branch | Sai de | Volta para |
|---|---|---|
| `feature/<issue>-<descricao>` | `develop` | `develop` (via PR) |
| `bugfix/<issue>-<descricao>` | `develop` | `develop` (via PR) |
| `release/x.y` | `develop` | `main` (via PR, com tag) e depois `develop` |
| `hotfix/<descricao>` | `main` | `main` e `develop` (via PR) |

- `main` e `develop` são protegidas: só aceitam PR com CI verde e 1 aprovação.
- Merge commit, sem squash.
- O PR fecha a issue com `Closes #n`.
- Tamanho recomendado de PR: até cerca de 400 linhas.
- A entrega final é a tag na `main`, e o hash dela vai no PDF de entrega.

### Commits

Conventional Commits em português, com scope obrigatório escolhido desta lista:

`service-orders`, `customers`, `catalog`, `inventory`, `identity`, `infra`, `ci`, `docs`

Exemplo: `feat(service-orders): adiciona envio de orçamento`

### Checklist do PR (Definition of Done)

- [ ] Testes do comportamento novo e de cada saída de erro
- [ ] Nenhum `if` de regra de negócio no use case
- [ ] Nenhum comentário em `.cs` e `.sql`
- [ ] Endpoint com policy explícita, metadados de Swagger e todos os tipos de resposta e status declarados
- [ ] Nenhum dado pessoal em rota ou query
- [ ] Nada sigiloso em log
- [ ] README e `docs/ddd/linguagem-ubiqua.md` atualizados, se aplicável
- [ ] Se o modelo EF mudou, o PR avisa que o banco precisa ser recriado

## 10. CI

O workflow `.github/workflows/ci.yml` roda em todo PR e push para `develop` e `main`, num único job:

1. `dotnet restore` e `dotnet tool restore`
2. Build em Release com warnings tratados como erro
3. Testes dos três projetos (unitários, contrato e funcionais) com cobertura (coverlet)
4. Relatório de cobertura com o ReportGenerator sobre `src/` (exceto `Migrations/` e `Program.cs`): resumo no job, comentário no PR e HTML como artefato
5. Falha se a cobertura de linhas ficar abaixo de **85%**

O `cd.yml` publica a imagem no GitHub Container Registry e faz um teste de fumaça com o `docker compose`.

## 11. Mudanças da Etapa 3

| Regra | Antes | Agora |
|---|---|---|
| Comentários | "Só o porquê" e XML comments em controllers e DTOs | Zero comentários em `.cs` e `.sql`, com checagem no CI (exceto as migrations geradas pelo EF Core). Swagger por metadados |
| Cobertura | 80% em Domain + Application | 85% em linhas sobre `src/`, somando os três projetos de teste. Só `Migrations/` e `Program.cs` são excluídos |
| Projetos de teste | Unitário e integração | `Oficina.UnitTests`, `Oficina.ContractTests` e `Oficina.FunctionalTests` |
| O que vai para o GitHub | Tudo | Código, testes, configuração de build/CI/Docker, README, `instructions/`, template de PR, `docs/enunciado/`, `docs/arquitetura/`, `docs/ddd/` e `docs/seguranca/`. Relatórios de cobertura gerados, documentos de trabalho (análises, planos, rascunhos de modelo, relatórios de execução) e coleções locais de API ficam fora do repositório |
| Commits | Por PR | Na Etapa 3, um único commit no fim do Prompt 6 |
| Contrato para o front-end | — | JSON camelCase, enums como string, datas ISO 8601 UTC, paginação `{ items, page, pageSize, totalItems }`, ProblemDetails com `code`/`traceId`/`errors`, CORS por configuração (`http://localhost:4200` em desenvolvimento), snapshot do OpenAPI e `GET /api/auth/me` |
| Continuidade | — | Todo trabalho começa e termina com build verde, todos os testes passando e cobertura ≥ 85%. Contratos entregues não mudam sem acordo |

## 12. Estado ao fim da Etapa 3

| Tema | Como ficou |
|---|---|
| Mapperly (§5) | Converte entidade em DTO com métodos explícitos para os value objects e `MapperIgnoreSource` para o que não vai ao DTO (por exemplo, `PasswordHash`). Não há projeção `IQueryable`: a consulta pagina no SQL, materializa com `AsNoTracking` e mapeia depois |
| Exceções (§4) | Erros esperados voltam como `Result` com um `Error` dos catálogos; não existem classes de exceção de domínio. Nomes de teste seguem `Method_Scenario_ExpectedResult` com o resultado (`ReturnsNotFound`, `IsBlocked`) |
| Validação | Um validator por command/query. As regras reaproveitam os value objects (`MustCreate`) e o código e a mensagem vêm do catálogo (`WithError`). Campos no erro em camelCase |
| Testes | Um `…HandlerTests` e um `…ValidatorTests` por caso de uso (sucesso, cada erro, `CommitAsync` só no sucesso). Contrato com o `ISender` substituído; funcional com banco real. Snapshot do OpenAPI atualizado só com `UPDATE_OPENAPI_SNAPSHOT=1` |
| Docker | `docker compose up -d --build` sobe o SQL Server e a API em Development (migrations e Admin de teste `admin@oficina.local` na subida). `docker compose down -v` zera o banco |

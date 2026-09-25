# Padrão de desenvolvimento — Oficina MVP

Regras que todo desenvolvedor do grupo segue. As decisões de arquitetura estão em [arquitetura.md](arquitetura.md).

---

## 1. Idioma

| Onde | Idioma |
|---|---|
| Código (classes, métodos, namespaces), rotas, banco e nomes de teste | Inglês |
| Commits, README, documentação DDD e mensagens de erro do domínio | Português |

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
| Comentários explicam só o porquê, nunca o quê | Checklist do PR |

**Sobre comentários:** a única exceção à regra do "porquê" são os XML comments de controllers e DTOs, que existem para documentar o Swagger.

**Sobre dados sigilosos:** nunca logar senha, hash, JWT, token de orçamento, CPF/CNPJ, e-mail, telefone, placa ou corpo de requisição. Logar apenas IDs, número da OS e `traceId`.

## 3. Estilo

- Namespace por arquivo e um tipo por arquivo, com o nome do arquivo igual ao do tipo.
- Classes `sealed` por padrão.
- Primary constructors em use cases, repositórios e controllers.
- `record` para Commands e DTOs.
- Value objects são `sealed class` com construtor privado e método `Create`.
- Campos privados como `_camelCase`. Métodos assíncronos com sufixo `Async`.
- `var` quando o tipo é óbvio pela linha.
- Nada de `.Result` ou `.Wait()`.
- O operador `!` (null-forgiving) só com comentário explicando o motivo.

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
| Integração | Fluxo completo da OS, rollback por falta de estoque, reprovação, 401 e 403 por perfil, e cada saída de erro dos diagramas conferindo o `code` do ProblemDetails |

- Assert puro do xUnit. Não usar FluentAssertions nem Shouldly.
- Estrutura AAA (Arrange, Act, Assert), com um comportamento por teste.
- Test Data Builders em `Builders/`, por exemplo `ServiceOrderBuilder`.
- Integração com um container Postgres por collection e limpeza do banco com Respawn entre os testes.
- Os testes criam os próprios dados e não dependem de dados inseridos manualmente.
- A cobertura mínima é de 80% em Domain + Application, com gate no CI. `Program.cs` e a camada Api ficam de fora do cálculo.

## 8. Ambiente

- `global.json` fixa a versão do SDK do .NET 10.
- `.config/dotnet-tools.json` lista as ferramentas locais (ex.: `reportgenerator`). Instale com `dotnet tool restore`.
- Pacotes NuGet ficam centralizados em `Directory.Packages.props`, só em versões estáveis. Adicionar pacote novo exige aprovação do grupo no PR.
- Segredos só em variáveis de ambiente. O `.env.example` fica no repositório e o `.env` fica no `.gitignore`.
- Quando o modelo EF mudar, recrie o banco com `docker compose down -v`.

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
- [ ] Comentários explicam só o porquê (XML comments só em controllers e DTOs)
- [ ] Endpoint com XML comment e respostas documentadas
- [ ] Nada sigiloso em log
- [ ] `.env.example`, README e `docs/ddd/linguagem-ubiqua.md` atualizados, se aplicável
- [ ] Se o modelo EF mudou, o PR avisa que o banco precisa ser recriado

## 10. CI

O workflow `.github/workflows/ci.yml` roda em todo PR para `develop` e `main`:

1. `dotnet restore` e `dotnet tool restore`
2. Checagem de `TODO` e `.gitkeep`
3. `dotnet format --verify-no-changes`
4. Build com analyzers tratados como erro
5. Testes unitários
6. Testes de integração
7. Cobertura com gate de 80%, com relatório salvo em `docs/evidencias/`
8. `dotnet list package --vulnerable`
9. Docker build e scan com Trivy, com relatório salvo em `docs/seguranca/`
10. Upload dos relatórios como artefato

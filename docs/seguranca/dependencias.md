# Análise de vulnerabilidades das dependências

Data da análise: 24/10/2026. Branch: `feature/frontend`.

Este documento registra a verificação das dependências do back-end (.NET) e do front-end (Angular). A análise estática do código (qualidade, bugs, vulnerabilidades e security hotspots) está em [sonarqube-relatorio.md](sonarqube-relatorio.md).

## Comandos

Back-end, na raiz do repositório:

```bash
dotnet list Oficina.slnx package --vulnerable --include-transitive
dotnet list Oficina.slnx package --outdated
```

Front-end, na pasta `web/`:

```bash
npm audit
npm outdated
npm run check:secrets
```

## Resultado

| Verificação | Resultado |
|---|---|
| `dotnet list package --vulnerable --include-transitive` | Nenhum pacote vulnerável nos 7 projetos (`Oficina.Api`, `Oficina.Application`, `Oficina.Domain`, `Oficina.Infrastructure`, `Oficina.ContractTests`, `Oficina.FunctionalTests` e `Oficina.UnitTests`) |
| NuGet Audit no `dotnet restore` | Sem avisos (auditoria padrão do SDK .NET 10, com avisos tratados como erro no build) |
| `npm audit` | `found 0 vulnerabilities` |
| `npm run check:secrets` | Nenhum segredo encontrado nos arquivos versionados; o `.env.example` só tem valores de desenvolvimento |

## Correções aplicadas nesta etapa

| Pacote | Antes | Depois | Motivo |
|---|---|---|---|
| Pacotes `Microsoft.*` do .NET 10 (ASP.NET Core, EF Core, Extensions) | 10.0.12 | 10.0.13 | Patch de segurança e correções do .NET 10 |
| `dotnet-ef` (ferramenta local) | 10.0.12 | 10.0.13 | Alinhado ao EF Core |
| `xunit.v3` | 4.0.1 | 4.0.2 | Patch |
| `System.Security.Cryptography.Pkcs` (pin transitivo, mantido) | 9.0.11 (via `Microsoft.Data.SqlClient`) | 10.0.13 | GHSA-846f-f764-76mx e GHSA-2fmv-rv2c-59x7. O pin continua em `Directory.Packages.props` enquanto o SqlClient trouxer a versão vulnerável |
| `vite` (transitivo do `@angular/build`) | versão vulnerável | 8.3.5 (`overrides` no `web/package.json`) | Advisory do servidor de desenvolvimento do Vite apontado pelo `npm audit` |

## Atualizações disponíveis sem vulnerabilidade

Nenhuma destas versões corrige vulnerabilidade. Ficam para uma atualização planejada, com testes completos.

| Pacote | Atual | Disponível | Decisão |
|---|---|---|---|
| `MediatR` | 12.5.0 | 14.x | Mantido: 12.5.0 é a última versão com licença Apache-2.0; as seguintes exigem licença comercial |
| `FluentValidation.DependencyInjectionExtensions` | 12.1.1 | 12.2.0 | Minor, sem advisory |
| `Swashbuckle.AspNetCore` | 10.2.3 | 10.3.1 | Minor, sem advisory; mudaria o snapshot do OpenAPI |
| `QRCoder` | 1.6.0 | 1.8.1 | Minor, sem advisory |
| `Testcontainers.MsSql` | 4.15.0 | 4.16.0 | Só testes |
| `Microsoft.Extensions.*.Testing`, `Microsoft.Testing.Extensions.TrxReport` | 10.10.0 / 2.4.1 | 10.11.0 / 2.5.1 | Só testes |
| Angular (`@angular/*`) | 22.2.1 | 22.2.3 / 22.2.4 | Patch. A atualização parcial gerou conflito de peer dependencies (`ERESOLVE`) e foi adiada para uma atualização conjunta de todo o Angular |
| `msw`, `typescript` | 2.15.0, 6.0.3 | 3.x, 7.x | Major, com mudanças de API |
| Demais pacotes de desenvolvimento do front (`vitest`, `eslint`, `playwright`, `prettier`, `jsdom`, `lucide`) | — | patch ou minor | Sem advisory |

## Como repetir

1. Rode os comandos da seção "Comandos".
2. Se houver pacote vulnerável, atualize a versão em `Directory.Packages.props` (back-end) ou em `web/package.json` (front-end).
3. Rode build, testes e cobertura antes de abrir o PR.
4. Atualize este documento com a data e o resultado.

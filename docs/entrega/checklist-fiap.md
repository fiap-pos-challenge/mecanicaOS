# Checklist do enunciado (Tech Challenge, Fase 1)

Cada item do enunciado ([docs/enunciado/tech-challenge-fase1.pdf](../enunciado/tech-challenge-fase1.pdf)) e onde está a evidência no repositório. Os caminhos são relativos à raiz.

Legenda: ✅ entregue no repositório · 👥 depende do grupo (fora do repositório).

## 1. Identificação da entrega (primeira página do PDF)

| Item | Situação | Onde |
|---|---|---|
| Curso SOAT, Fase 1, número do grupo | 👥 | PDF de entrega |
| Nome completo, RM e usuário do Discord de cada integrante | 👥 | PDF de entrega |
| Linguagem e principais tecnologias | ✅ | `README.md`, seção 4 |

## 2. Repositório

| Item | Situação | Onde |
|---|---|---|
| Repositório público no GitHub | 👥 | `https://github.com/fiap-pos-challenge/mecanicaOS` (conferir a visibilidade) |
| Versão identificada por branch, tag ou hash | 👥 | Tag na `main` e hash do commit no PDF |
| `main` protegida, tudo por Pull Request | 👥 | Configuração do GitHub; template em `.github/pull_request_template.md` |
| Sem credenciais, senhas ou tokens | ✅ | Segredos só no `.env` local (`.env.example` com valores de desenvolvimento), `.gitignore`, `npm run check:secrets` e `scripts/ci/create-env.sh` no CI |
| README completo | ✅ | `README.md` (itens abaixo) |
| Instruções para execução local | ✅ | `README.md`, seções 5, 6 e 15 |
| Testes com cobertura mínima de 80% | ✅ | Gate de 85% no CI (`.github/workflows/ci.yml`); [docs/qualidade/cobertura.md](../qualidade/cobertura.md) |
| Documentação da API (Swagger/OpenAPI) | ✅ | `/swagger` em Development; `tests/Oficina.ContractTests/OpenApi/Snapshots/openapi.json` |
| Relatório da análise de vulnerabilidades | ✅ | [docs/seguranca/sonarqube-relatorio.md](../seguranca/sonarqube-relatorio.md) e [docs/seguranca/dependencias.md](../seguranca/dependencias.md) |

### Itens mínimos do README

| Item | Seção do `README.md` |
|---|---|
| Objetivo do projeto | 1 |
| Descrição resumida da solução | 2 |
| Arquitetura utilizada | 3 |
| Tecnologias utilizadas | 4 |
| Pré-requisitos | 5 |
| Instruções para execução | 6 |
| Instruções para execução dos testes | 9 |
| Acesso ao Swagger ou documentação da API | 8 |
| Decisão e justificativa do banco de dados | 11 |
| Usuários, credenciais de demonstração ou procedimento de autenticação | 7 |
| Estrutura dos principais diretórios | 14 |
| Link para a documentação DDD | Topo e seção 3 (`docs/ddd/`) |
| Link para o vídeo da entrega | Topo (`[link do vídeo]`, 👥 preencher depois da publicação) |

## 3. Vídeo

| Item | Situação |
|---|---|
| YouTube, público, até 15 minutos, áudio compreensível, integrantes identificados | 👥 |
| Mostra o commit informado no PDF | 👥 |
| Conteúdo mínimo (proposta, arquitetura, camadas, banco, Event Storming, DDD, fluxo da OS, itens, orçamento, status, autenticação, execução, Swagger, testes e vulnerabilidades) | 👥 (roteiro sugerido no documento de entrega) |

## 4. Documentação DDD

| Item | Situação | Onde |
|---|---|---|
| Link externo acessível sem pedir permissão | 👥 / ✅ | Os `.drawio` abrem no [diagrams.net](https://app.diagrams.net/) direto do GitHub público (links em [docs/ddd/README.md](../ddd/README.md)). Se o grupo usar Miro, colocar o link no PDF |
| `docs/ddd/event-storming.pdf` | ✅ | Exportado de `docs/arquitetura/EventStorming_mecanicaOS.drawio` |
| `docs/ddd/diagramas.pdf` | ✅ | Subdomínios, mapa de contextos, modelo de domínio, Domain Storytelling e C4 |
| `docs/ddd/linguagem-ubiqua.md` | ✅ | Linguagem ubíqua e glossário |
| Event Storming da criação da OS | ✅ | `event-storming.pdf`, página do fluxo 1 |
| Event Storming do acompanhamento da OS | ✅ | `event-storming.pdf`, página do fluxo 2 |
| Event Storming da gestão de peças e insumos | ✅ | `event-storming.pdf`, página do fluxo 3 |
| Eventos, comandos, políticas, atores, agregados, entidades e objetos de valor | ✅ | Diagramas e seções de `linguagem-ubiqua.md` |
| Bounded contexts | ✅ | `diagramas.pdf` (mapa de contextos) e `linguagem-ubiqua.md` |
| Diagramas da disciplina de DDD | ✅ | `diagramas.pdf` (subdomínios, mapa de contextos, modelo de domínio, Domain Storytelling) |
| Relação entre a documentação e o código | ✅ | [docs/ddd/rastreabilidade.md](../ddd/rastreabilidade.md) |

## 5. Funcionalidades obrigatórias

| Item | Código | Teste |
|---|---|---|
| Cliente por CPF ou CNPJ | `src/Oficina.Domain/Customers/ValueObjects/Document.cs`, `POST /api/admin/customers/search` | `DocumentTests`, `CustomersApiTests` |
| Veículo com placa, marca, modelo e ano | `LicensePlate.cs`, `VehicleYear.cs`, `POST /api/admin/vehicles` | `LicensePlateTests`, `VehiclesApiTests` |
| Inclusão de serviços, peças e insumos | `ServiceOrder.cs`, `Estimate.cs`, `PUT /api/admin/service-orders/{id}/diagnosis`, `PUT /api/admin/quotes/{id}/draft` | `ServiceOrderTests`, `EstimateTests`, `QuotePortalApiTests` |
| Orçamento gerado automaticamente | `src/Oficina.Domain/Quotes/Pricing/EstimatePricing.cs` | `EstimateTests`, `QuotePortalApiTests` |
| Envio ou disponibilização do orçamento | `POST /api/admin/quotes/{id}/send` | `QuotePortalApiTests` |
| Registro da aprovação ou rejeição | `QuoteDecider`, `QuoteApprovalPolicy`, portal (`/api/public/portal/quote/*`) e balcão (`/api/admin/quotes/{id}/approve` e `/reject`) | `QuotePortalApiTests.Decisions`, `PublicServiceOrdersApiTests` |
| Status Recebida, Em diagnóstico, Aguardando aprovação, Em execução, Finalizada e Entregue | `ServiceOrderStatus` | `ServiceOrderTests` |
| Controle das alterações e regras de transição | Métodos do agregado `ServiceOrder` (`StartDiagnosis`, `SendQuote`, `ApproveQuote`, `RejectQuote`, `Complete`, `Deliver`), `POST /api/admin/service-orders/{id}/transitions` | `ServiceOrderTests`, `ServiceOrderWorkflowApiTests` |
| Consulta do andamento pelo cliente via API | `POST /api/public/service-orders/lookup`, `GET /api/public/portal/order` | `PublicServiceOrdersApiTests`, `QuotePortalApiTests` |
| CRUD de clientes | `/api/admin/customers` | `CustomersApiTests` |
| CRUD de veículos | `/api/admin/vehicles` | `VehiclesApiTests` |
| CRUD de serviços | `/api/admin/services` (`DELETE` desativa) | `ServicesApiTests` |
| CRUD de peças e insumos | `/api/admin/parts` (`DELETE` desativa) | `PartsApiTests` |
| Controle de estoque | `Part.cs`, `/api/admin/inventory/*`, baixa na aprovação | `PartTests`, `InventoryApiTests`, `ApproveByToken_InsufficientStock_Returns409AndRollsBack` |
| Listagem e detalhe de OS | `GET /api/admin/service-orders`, `POST /api/admin/service-orders/search`, `GET /api/admin/service-orders/{id}` | `ServiceOrdersApiTests` |
| Tempo médio de execução | `GET /api/admin/metrics/average-execution-time` | `ServiceOrdersApiTests` |

## 6. Requisitos técnicos

| Item | Situação | Onde |
|---|---|---|
| Back-end monolítico em camadas | ✅ | `src/` (4 projetos); testes de arquitetura em `tests/Oficina.ContractTests/Architecture` |
| Banco escolhido com justificativa | ✅ | `README.md`, seção 11 |
| APIs RESTful | ✅ | `src/Oficina.Api/Controllers` |
| Swagger/OpenAPI | ✅ | `README.md`, seção 8 |
| JWT nas APIs administrativas | ✅ | `FallbackPolicy` e policies em `src/Oficina.Api/Extensions`; `RouteAuthorizationTests`, `AuthorizationMatrixTests` |
| Validação de CPF/CNPJ e placa | ✅ | `Document.cs`, `LicensePlate.cs` e testes |
| Testes unitários | ✅ | `tests/Oficina.UnitTests` |
| Testes de integração dos fluxos principais | ✅ | `tests/Oficina.FunctionalTests` (SQL Server real com Testcontainers) |
| Cobertura mínima de 80% nos domínios críticos | ✅ | [docs/qualidade/cobertura.md](../qualidade/cobertura.md) |
| Dockerfile e docker-compose | ✅ | `Dockerfile`, `docker-compose.yml` |
| Transações nos fluxos principais | ✅ | Um `CommitAsync` por command; aprovação, itens e baixa de estoque no mesmo commit (`README.md`, seções 3 e 13) |

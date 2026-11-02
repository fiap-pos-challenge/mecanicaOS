# Rastreabilidade · Event Storming → código

Cada linha é um post-it dos três Event Storming ([`event-storming.pdf`](event-storming.pdf)), com o comando que o dispara, a rota, o agregado que decide, o evento de domínio levantado (quando existe), a política ou regra aplicada e os testes que cobrem. Todos os nomes existem no código. Onde não há evento de domínio, o fato fica registrado no próprio agregado (`StatusHistory`, `StockMovement`, colunas de data).

Caminhos: comandos e queries em `src/Oficina.Application/<Contexto>/`, agregados em `src/Oficina.Domain/<Contexto>/`, controllers em `src/Oficina.Api/Controllers/`. `FT` = `tests/Oficina.FunctionalTests/Api`, `UT` = `tests/Oficina.UnitTests`.

## Event Storming 1 · Criação da O.S.

| # | Passo (evento) | Ator | Comando · rota | Agregado | Evento de domínio | Política / regra | Testes |
|---|---|---|---|---|---|---|---|
| 1 | Cliente cadastrado | Atendente | `CreateCustomerCommand` · `POST /api/admin/customers` | `Customer` | — | `Document` valida CPF/CNPJ; documento único | `CustomersApiTests`, `CreateCustomerCommandHandlerTests`, `DocumentTests`, `CustomerTests` |
| 2 | Veículo cadastrado | Atendente | `CreateVehicleCommand` · `POST /api/admin/vehicles` | `Vehicle` | — | `LicensePlate` (antiga e Mercosul), `VehicleYear`, placa única | `VehiclesApiTests`, `CreateVehicleCommandHandlerTests`, `LicensePlateTests`, `VehicleTests` |
| 3 | O.S. aberta (Recebida) | Atendente | `OpenServiceOrderCommand` · `POST /api/admin/service-orders` | `ServiceOrder` | `ServiceOrderOpened` | `ServiceOrderOpeningPolicy` (veículo do cliente, uma OS aberta por veículo); `ServiceOrder.Open` (descrição) | `ServiceOrdersApiTests`, `OpenServiceOrderCommandHandlerTests`, `ServiceOrderOpeningPolicyTests`, `ServiceOrderTests` |
| 4 | Diagnóstico iniciado (Em diagnóstico) | Mecânico | `StartDiagnosisCommand` · `POST /api/admin/service-orders/{id}/diagnosis/start` | `ServiceOrder` | — (grava `StatusHistory`) | `ServiceOrder.StartDiagnosis` só de Recebida | `ServiceOrderFlowApiTests`, `ServiceOrderTransitionTests`, `ServiceOrderTransitionHandlersTests` |
| 5 | Serviço incluído | Mecânico | `AddServiceItemCommand` · `POST …/{id}/services` | `ServiceOrder` (`ServiceItem`) | — | só Em diagnóstico; serviço ativo; quantidade > 0; preço copiado | `ServiceOrderFlowApiTests`, `ServiceOrderTests` |
| 6 | Peça ou insumo incluído | Mecânico | `AddPartItemCommand` · `POST …/{id}/parts` | `ServiceOrder` (`PartItem`) | — | só Em diagnóstico; peça ativa; `Part.HasAvailable` | `ServiceOrderFlowApiTests`, `ServiceOrderTests`, `PartTests` |
| 7 | Item removido | Mecânico | `RemoveItemCommand` · `DELETE …/{id}/items/{itemId}` | `ServiceOrder` | — | só Em diagnóstico; item deve existir | `ServiceOrderTests`, `ServiceOrderTransitionHandlersTests` |
| 8 | Orçamento gerado e enviado (Aguardando aprovação) | Atendente | `SendQuoteCommand` · `POST …/{id}/quote/send` | `ServiceOrder` (`Quote`) | `QuoteSent` | ao menos um item; total = serviços + peças; `ApprovalToken` (hash SHA-256) | `ServiceOrderFlowApiTests`, `ServiceOrderTests`, `ServiceOrderValueObjectTests` |

## Event Storming 2 · Acompanhamento da O.S.

| # | Passo (evento) | Ator | Comando · rota | Agregado | Evento de domínio | Política / regra | Testes |
|---|---|---|---|---|---|---|---|
| 1 | Andamento consultado | Cliente | `LookupServiceOrderQuery` · `POST /api/public/service-orders/lookup` | — (read model `PublicServiceOrderDto`) | — | número e CPF/CNPJ precisam bater; dado pessoal só no corpo | `PublicServiceOrdersApiTests`, `LookupServiceOrderQueryHandlerTests`, `PersonalDataInUrlTests` |
| 2 | Orçamento aprovado pelo link (Em execução) | Cliente | `ApproveQuoteByTokenCommand` · `POST /api/public/quotes/{token}/approve` | `ServiceOrder` (`Quote`) | `QuoteApproved` | token de uso único; `QuoteApprovalPolicy` | `PublicServiceOrdersApiTests`, `ApproveQuoteByTokenCommandHandlerTests`, `QuoteApprovalPolicyTests` |
| 3 | Orçamento aprovado no balcão | Atendente | `ApproveQuoteCommand` · `POST …/{id}/quote/approve` | `ServiceOrder` (`Quote`) | `QuoteApproved` | mesma política; usuário registrado no `StatusHistory` | `ServiceOrderFlowApiTests`, `ApproveQuoteCommandHandlerTests` |
| 4 | Estoque baixado | Sistema | (mesma transação da aprovação) | `Part` (`StockMovement` Deduction) | — | `Part.DeductForServiceOrder`: saldo nunca negativo; tudo ou nada | `QuoteApprovalPolicyTests`, `PartTests` |
| 5 | Orçamento rejeitado (volta para Em diagnóstico) | Cliente / Atendente | `RejectQuoteByTokenCommand` · `POST /api/public/quotes/{token}/reject`; `RejectQuoteCommand` · `POST …/{id}/quote/reject` | `ServiceOrder` (`Quote`) | `QuoteRejected` | motivo obrigatório; novo envio cria versão + 1 | `ServiceOrderFlowApiTests`, `RejectQuoteByTokenCommandHandlerTests`, `ServiceOrderTransitionTests` |
| 6 | O.S. finalizada (Finalizada) | Mecânico | `CompleteServiceOrderCommand` · `POST …/{id}/complete` | `ServiceOrder` | `ServiceOrderCompleted` | só Em execução; grava `CompletedAt`; handler `NotifyCustomerOnServiceOrderCompleted` → `INotifier` (log, *To be implemented*) | `ServiceOrderFlowApiTests`, `NotifyCustomerOnServiceOrderCompletedTests`, `OutboxTests` |
| 7 | Veículo entregue (Entregue) | Atendente | `DeliverServiceOrderCommand` · `POST …/{id}/deliver` | `ServiceOrder` | — | só Finalizada; grava `DeliveredAt`; libera o veículo | `ServiceOrderFlowApiTests`, `ServiceOrderTransitionTests` |
| 8 | Tempo médio calculado | Admin | `GetAverageExecutionTimeQuery` · `GET /api/admin/metrics/average-execution-time` | — (read model) | — | média de `CompletedAt − ExecutionStartedAt` por período | `GetAverageExecutionTimeQueryHandlerTests`, `ServiceOrderFlowApiTests` |
| — | Listagem e detalhe da OS | Todos os perfis | `ListServiceOrdersQuery` · `GET /api/admin/service-orders`; `GetServiceOrderByIdQuery` · `GET …/{id}` | — | — | filtros e paginação | `ServiceOrdersApiTests`, `ListServiceOrdersQueryHandlerTests`, `GetServiceOrderByIdQueryHandlerTests` |
| — | Transições fora de ordem | — | qualquer comando de transição | `ServiceOrder` | — | `InvalidTransition` → HTTP 409 | `ServiceOrderFlowApiTests.Transitions_OutOfOrder_Return409`, `ServiceOrderTransitionTests` |

## Event Storming 3 · Peças e insumos

| # | Passo (evento) | Ator | Comando · rota | Agregado | Evento de domínio | Política / regra | Testes |
|---|---|---|---|---|---|---|---|
| 1 | Peça ou insumo cadastrado | Admin | `CreatePartCommand` · `POST /api/admin/parts` | `Part` | — | `PartCode` único; `PartType`; estoque inicial zero | `PartCommandHandlersTests`, `PartCodeTests`, `PartTests` |
| 2 | Dados e preço alterados | Admin | `UpdatePartCommand` · `PUT /api/admin/parts/{id}` | `Part` | — | `Part.ChangeDetails`; itens de OS existentes mantêm o preço copiado | `PartCommandHandlersTests`, `ServiceOrderFlowApiTests.Parts_CrudAndListingFilters_Work` |
| 3 | Entrada de estoque registrada | Admin | `RegisterStockEntryCommand` · `POST …/parts/{id}/stock-entries` | `Part` (`StockMovement` Entry) | — | quantidade > 0 (3 casas); motivo obrigatório | `PartCommandHandlersTests`, `PartTests` |
| 4 | Peça incluída na OS | Mecânico | `AddPartItemCommand` | `ServiceOrder` (`PartItem`) | — | peça ativa e com saldo | `ServiceOrderTests` |
| 5 | Estoque baixado na aprovação | Sistema | (aprovação do orçamento) | `Part` (`StockMovement` Deduction) | — | `QuoteApprovalPolicy` | `QuoteApprovalPolicyTests`, `PartTests` |
| 6 | Estoque abaixo do mínimo identificado | Admin / Atendente | `ListPartsQuery` · `GET /api/admin/parts?belowMinimum=true` | — (read model) | — | `Part.IsBelowMinimum` | `PartQueryHandlersTests`, `ServiceOrderFlowApiTests.Parts_CrudAndListingFilters_Work` |
| 7 | Peça desativada ou reativada | Admin | `DeactivatePartCommand` / `ReactivatePartCommand` · `POST …/parts/{id}/deactivate` e `/reactivate` | `Part` | — | desativar não exclui; inativa não entra em novas OS | `PartCommandHandlersTests`, `PartTests` |

## Elementos transversais

| Elemento | Onde no código | Testes |
|---|---|---|
| Autenticação JWT | `LoginCommand`, `JwtTokenGenerator`, `POST /api/auth/login` | `AuthApiTests`, `JwtTokenGeneratorTests`, `LoginCommandHandlerTests` |
| Autorização por perfil | `Policies`, atributos `[Authorize]` | `AuthorizationMatrixTests`, `RouteAuthorizationTests` |
| Transação e Outbox | `UnitOfWork.CommitAsync`, `OutboxWriter`, `OutboxBatchProcessor` | `OutboxTests`, `OutboxProcessorHostingTests` |
| Soft delete e auditoria | `AuditingInterceptor`, `ISoftDeletable`, `IAuditable` | `AuditingInterceptorTests`, `NoPhysicalDeleteTests` |
| Contrato da API (Swagger/OpenAPI) | `Oficina.Api`, snapshot em `tests/Oficina.ContractTests/OpenApi/Snapshots/openapi.json` | `OpenApiSnapshotTests` |
| Dependência entre camadas | projetos `Domain`, `Application`, `Infrastructure`, `Api` | `LayerDependencyTests` |

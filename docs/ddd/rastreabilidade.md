# Rastreabilidade · Event Storming → código

Cada linha é um post-it de evento dos três Event Storming ([`event-storming.pdf`](event-storming.pdf), fonte em [`docs/arquitetura/EventStorming_mecanicaOS.drawio`](../arquitetura/EventStorming_mecanicaOS.drawio)), com o comando que o dispara, o agregado que decide, o evento de domínio levantado (quando existe), a política que reage e o teste que cobre. Todos os nomes existem no código; onde não há evento de domínio, o fato fica registrado no próprio agregado.

## Event Storming 1 · Criação da O.S.

| # | Passo (evento) | Ator | Comando · rota | Agregado | Evento de domínio | Política / handler | Teste |
|---|---|---|---|---|---|---|---|
| 1 | Cliente cadastrado | Atendente | `CreateCustomerCommand` · `POST /api/admin/customers` | `Customer` | — | — | `CustomersApiTests`, `CreateCustomerCommandHandlerTests` |
| 2 | Veículo cadastrado | Atendente | `CreateVehicleCommand` · `POST /api/admin/vehicles` | `Vehicle` | — | — | `VehiclesApiTests`, `CreateVehicleCommandHandlerTests` |
| 3 | O.S. aberta (Recebida) | Atendente | `OpenServiceOrderCommand` · `POST /api/admin/service-orders` | `ServiceOrder` | `ServiceOrderOpened` | `ServiceOrderOpeningPolicy` (uma O.S. aberta por veículo) | `ServiceOrdersApiTests`, `ServiceOrderOpeningPolicyTests` |
| 4 | Diagnóstico iniciado | Mecânico | `StartServiceCommand` · `POST /api/admin/service-orders/{id}/start-service` | `ServiceOrder` | `ServiceOrderStatusChanged` | `OnServiceOrderStatusChanged` (tempo real e aviso) | `ServiceOrderWorkflowApiTests`, `ServiceOrderTransitionTests` |
| 5 | Checklist e fotos registrados | Mecânico | `UpdateChecklistCommand` · `PUT .../{id}/checklist`; `AddPhotoCommand` · `POST .../{id}/photos` | `ServiceOrder`, `ServiceOrderPhoto` | — | regra: até 12 fotos (`ServiceOrder.RegisterPhoto`) | `ServiceOrderWorkflowApiTests`, `QuotePortalApiTests.Photos_AreServedOnDemand_AndLimitedPerOrder` |
| 6 | Diagnóstico registrado | Mecânico | `SaveDiagnosisCommand` · `PUT .../{id}/diagnosis` | `ServiceOrder` | — | sugestões: `SuggestItemsQuery` | `ServiceOrderWorkflowApiTests` |
| 7 | Itens indicados para orçamento | Mecânico | `SendToQuoteCommand` · `POST .../{id}/send-to-quote` | `ServiceOrder` | — | regra: itens só Em diagnóstico | `ServiceOrderWorkflowApiTests`, `ServiceOrderTests` |
| 8 | Rascunho salvo | Atendente | `CreateQuoteCommand` · `POST /api/admin/quotes`; `SaveQuoteDraftCommand` · `PUT /api/admin/quotes/{id}/draft` | `Estimate` | — | `EstimatePricing` (desconto automático) | `QuotePortalApiTests`, `EstimateTests` |
| 9 | Aprovação de desconto pedida | Atendente | `RequestDiscountApprovalCommand` · `POST /api/admin/quotes/{id}/discount-approvals` | `Estimate` | — | regra: acima do limite do perfil (`Estimate.RequestApproval`) | `EstimateTests.DiscountApproval_IsOnlyForDiscountsAboveTheLimit_AndIsDecidedOnce` |
| 10 | Desconto aprovado ou recusado | Gerente | `DecideDiscountApprovalCommand` · `POST .../discount-approvals/{approvalId}/approve` ou `/reject` | `Estimate` | — | auditoria (`AuditEntry`) | `EstimateTests` |
| 11 | Orçamento enviado (Aguardando aprovação) | Atendente | `SendQuoteCommand` · `POST /api/admin/quotes/{id}/send` | `Estimate`, `ServiceOrder` | `QuoteSent`, `ServiceOrderStatusChanged` | `OnQuoteSent` (automações) | `QuotePortalApiTests`, `EstimateTests.EnsureReadyToSend_BlocksEmptyZeroAndUnapprovedDiscounts` |

## Event Storming 2 · Acompanhamento da O.S.

| # | Passo (evento) | Ator | Comando · rota | Agregado | Evento de domínio | Política / handler | Teste |
|---|---|---|---|---|---|---|---|
| 1 | Portal acessado | Cliente | `GetPortalAccessQuery` · `POST /api/public/portal/access` | `Estimate` | — | limite por IP (`PublicRateLimitPolicy`) | `QuotePortalApiTests`, `QuotePortalApiTests.PublicRoutes_ShareARateLimitPerClient` |
| 2 | Identidade confirmada | Cliente | `ConfirmPortalIdentityCommand` · `POST /api/public/portal/confirm` | `Estimate`, `PortalSession` | — | regra: 3 erros bloqueiam (`Estimate.RegisterPortalAttempt`, `rowVersion`) | `QuotePortalApiTests.ParallelWrongDigits_NeverRegisterMoreThanThreeFailures`, `EstimateTests` |
| 3 | Orçamento aprovado (Em execução) | Cliente / Atendente | `ApproveQuoteFromPortalCommand` · `POST /api/public/portal/quote/approve`; `ApproveQuoteAtDeskCommand` · `POST /api/admin/quotes/{id}/approve`; `TransitionServiceOrderCommand` · `POST /api/admin/service-orders/{id}/transitions` | `Estimate`, `ServiceOrder`, `Part` | `QuoteApproved`, `ServiceOrderStatusChanged` | `QuoteDecider` (porta única), `QuoteApprovalPolicy`, `OnQuoteApproved` | `QuotePortalApiTests.PartialPortalApproval_KeepsOnlyTheAcceptedItems_WithoutRecordingARejection`, `QuotePortalApiTests.BoardApproval_ApprovesTheQuote_SoAPartialPortalAnswerIsRefused`, `QuoteApprovalPolicyTests` |
| 4 | Estoque baixado | Sistema | (mesma transação da aprovação) | `Part` | `StockDeducted` | — | `PartTests.DeductForServiceOrder_RaisesStockDeducted_AndStockBelowMinimumWhenItCrossesTheMinimum` |
| 5 | Orçamento recusado (volta para Em diagnóstico) | Cliente / Atendente | `RejectQuoteFromPortalCommand` · `POST /api/public/portal/quote/reject`; `RejectQuoteAtDeskCommand` · `POST /api/admin/quotes/{id}/reject`; `TransitionServiceOrderCommand` | `Estimate`, `ServiceOrder` | `QuoteRejected`, `ServiceOrderStatusChanged` | `QuoteDecider`, `OnQuoteRejected` | `QuotePortalApiTests.BoardRejection_RejectsTheQuote_SoALaterApprovalCannotOverrideIt` |
| 6 | Nova versão criada | Atendente | `CreateQuoteVersionCommand` · `POST /api/admin/quotes/{id}/versions` | `Estimate` | — | — | `EstimateTests.Reject_NeedsAReason_AndAllowsANewVersionWithNewLineIds` |
| 7 | Tempo de trabalho registrado | Mecânico | `StartTimerCommand` / `StopTimerCommand` · `POST .../{id}/time-entries/start` e `/stop` | `ServiceOrder` (`TimeEntry`) | — | produtividade (`GetProductivityQuery`) | `ServiceOrderWorkflowApiTests` |
| 8 | O.S. finalizada | Mecânico | `FinishServiceCommand` · `POST .../{id}/finish-service` | `ServiceOrder` | `ServiceOrderCompleted`, `ServiceOrderStatusChanged` | `NotifyCustomerOnServiceOrderCompleted` (log, *To be implemented*) | `ServiceOrderWorkflowApiTests`, `NotifyCustomerOnServiceOrderCompletedTests` |
| 9 | Cobrança Pix gerada | Cliente | `CreatePixChargeCommand` · `POST /api/public/portal/payments/pix` | `PortalCharge`, `Estimate` | — | regra: só o valor aprovado e não pago (`Estimate.AmountToCharge`) | `QuotePortalApiTests`, `EstimateTests.AmountToCharge_IsTheApprovedTotalUntilPaid` |
| 10 | Pagamento confirmado (simulado) | Instituição de Pagamento (simulada) | `ConfirmPixPaymentCommand` · `POST /api/public/payments/pix/webhook` | `PortalCharge` | `PaymentConfirmed` | `IssueReceiptOnPaymentConfirmed`, `BroadcastPaymentConfirmed` | `QuotePortalApiTests.Quote_FromDraftToPortalApprovalPixPaymentAndDocuments` |
| 11 | Recibo emitido | Sistema | (handler do Outbox) | `IssuedDocument` | — | `DocumentIssuer` | `QuotePortalApiTests.Quote_FromDraftToPortalApprovalPixPaymentAndDocuments` |
| 12 | Veículo entregue | Atendente | `TransitionServiceOrderCommand` (Finalizada → Entregue) | `ServiceOrder` | `ServiceOrderStatusChanged` | `OnServiceOrderStatusChanged` (automação de entrega) | `ServiceOrderWorkflowApiTests`, `ServiceOrderTransitionTests` |
| — | Consulta pública da O.S. (read model) | Cliente | `LookupServiceOrderQuery` · `POST /api/public/service-orders/lookup` | — | — | — | `PublicServiceOrdersApiTests`, `LookupServiceOrderQueryHandlerTests` |

## Event Storming 3 · Peças e Insumos

| # | Passo (evento) | Ator | Comando · rota | Agregado | Evento de domínio | Política / handler | Teste |
|---|---|---|---|---|---|---|---|
| 1 | Peça cadastrada | Gerente | `CreatePartCommand` · `POST /api/admin/parts` | `Part` | — | — | `PartsApiTests`, `PartCommandHandlerTests` |
| 2 | Preço alterado | Gerente | `UpdatePartCommand` · `PUT /api/admin/parts/{id}` | `Part`, `PriceChange` | — | histórico de preços | `PartsApiTests`, `GetServicePriceHistoryQueryTests` |
| 3 | Pacote criado | Gerente | `CreatePackageCommand` · `POST /api/admin/packages` | `Package` | — | regra: peça em pacote ativo não desativa | `PackagesApiTests`, `PackageTests` |
| 4 | Fornecedor cadastrado | Gerente | `CreateSupplierCommand` · `POST /api/admin/inventory/suppliers` | `Supplier` | — | — | `InventoryApiTests` |
| 5 | Entrada registrada | Gerente | `RegisterEntryCommand` · `POST /api/admin/inventory/entries`; `ConfirmInvoiceCommand` · `POST /api/admin/inventory/invoices/confirm` | `Part` | — | — | `InventoryApiTests` |
| 6 | Saída registrada | Gerente | `RegisterExitCommand` · `POST /api/admin/inventory/exits` | `Part` | `StockBelowMinimum` (se cruzar o mínimo) | `OnStockBelowMinimum` | `InventoryApiTests`, `PartTests.RegisterExit_AndAdjust_RaiseStockBelowMinimumOnlyWhenTheyCrossTheMinimum` |
| 7 | Estoque baixado (vem da aprovação) | Sistema | (aprovação do orçamento) | `Part` | `StockDeducted` | `QuoteApprovalPolicy` | `QuoteApprovalPolicyTests`, `PartTests` |
| 8 | Estoque abaixo do mínimo | Sistema | (baixa, saída ou ajuste) | `Part` | `StockBelowMinimum` | `OnStockBelowMinimum` (automação e aviso); varredura `AutomationSweeper` como rede de segurança | `PartTests`, `OperationsApiTests` |
| 9 | Pedido de compra criado | Gerente / automação | `CreatePurchaseOrderCommand` · `POST /api/admin/inventory/purchase-orders` | `PurchaseOrder` | — | `PurchaseOrderDrafts` (ação `CreatePurchaseOrder`) | `InventoryApiTests` |
| 10 | Pedido enviado | Gerente | `SendPurchaseOrderCommand` · `POST /api/admin/inventory/purchase-orders/{id}/send` | `PurchaseOrder` | — | e-mail depois do commit | `InventoryApiTests` |
| 11 | Contagem enviada para aprovação | Mecânico / Gerente | `OpenCountCommand` · `POST /api/admin/inventory/counts`; `RecordCountCommand` · `PUT /api/admin/inventory/counts/{id}/lines` | `StockCount` | — | — | `InventoryApiTests` |
| 12 | Diferenças ajustadas no estoque | Gerente | `DecideCountCommand` · `POST /api/admin/inventory/counts/{id}/approve` | `StockCount`, `Part` | `StockBelowMinimum` (se cruzar o mínimo) | `OnStockBelowMinimum` | `InventoryApiTests`, `PartTests` |

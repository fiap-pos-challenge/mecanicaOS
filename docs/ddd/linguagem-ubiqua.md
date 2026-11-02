# Linguagem ubíqua · Oficina mecânica

> [!NOTE]
> **Como ler este dicionário.** Cada termo traz a **definição** e o que vale **no sistema**. A linha **No código** indica o nome real usado na implementação (classe, método, enum ou rota).
> O enunciado do Tech Challenge faz o papel de especialista do domínio. Código, rotas, JSON e banco usam inglês; esta documentação e as mensagens ao usuário usam português.

## Cliente

**Definição:** pessoa física ou jurídica, identificada por **CPF ou CNPJ**, que leva um veículo à oficina.

**No sistema:**
- O documento é validado pelos dígitos verificadores, aceita máscara e é guardado só com dígitos. Sequências repetidas (`111.111.111-11`) são recusadas.
- O documento é único: não existem dois clientes com o mesmo CPF/CNPJ.
- Nome com 2 a 160 caracteres, telefone obrigatório, e-mail e endereço opcionais.
- O cliente não faz login. Ele acompanha a OS pela consulta pública (número da OS + CPF/CNPJ) e decide o orçamento pelo link com token.
- Excluir é *soft delete*. Um cliente com OS aberta ou com veículos ativos não pode ser excluído.

**No código:** `Customer`, `Document` (`DocumentType { Cpf, Cnpj }`), `Phone`, `Email`, `Address`, `RegistrationDeletionPolicy`, rotas `/api/admin/customers`.

## Veículo

**Definição:** bem automotor de um cliente, identificado pela placa, sobre o qual a OS é aberta.

**No sistema:**
- Placa no formato antigo (`ABC1234`) ou Mercosul (`ABC1D23`), normalizada em maiúsculas e sem hífen. A placa é única e não muda depois do cadastro.
- Marca e modelo com até 60 caracteres. Ano a partir de 1900 e no máximo o ano seguinte ao atual.
- Pertence a um único cliente. Um veículo com OS aberta não pode ser excluído.

**No código:** `Vehicle`, `LicensePlate`, `VehicleYear`, rotas `/api/admin/vehicles`.

## Serviço

**Definição:** trabalho prestado pela oficina e oferecido em catálogo (ex.: alinhamento, troca de óleo), com preço base e tempo estimado.

**No sistema:**
- Nome de 2 a 120 caracteres, preço maior que zero, tempo estimado em minutos maior que zero.
- **Desativar não é excluir:** o serviço continua consultável e pode ser reativado, mas não entra em novas OS. Serviço inativo não pode ser alterado.
- Ao entrar na OS, nome e preço são **copiados** para o `ServiceItem`. Mudar o catálogo depois não altera OS existentes.

**No código:** `Service` (aggregate root), `ServiceItem`, rotas `/api/admin/services`. O sufixo *Service* é do domínio; classes técnicas evitam esse nome.

## Peça e insumo

**Definição:** material usado no reparo. **Peça** é componente que o veículo recebe (pastilha de freio). **Insumo** é material consumido na execução (óleo, aditivo). Ambos têm o mesmo cadastro e se diferenciam pelo tipo.

**No sistema:**
- Código único (letras maiúsculas, números, ponto e hífen, até 30 caracteres), nome, tipo, unidade de medida, preço unitário e estoque mínimo.
- O estoque começa em zero e muda por **entrada** (manual, com motivo) e por **baixa** (aprovação do orçamento). Cada alteração cria um `StockMovement` com o saldo resultante.
- Quantidades aceitam até 3 casas decimais e precisam ser maiores que zero. O saldo nunca fica negativo.
- **Abaixo do mínimo:** saldo menor que o estoque mínimo. A listagem aceita o filtro `belowMinimum=true`.
- Peça inativa não entra em novas OS. A peça só entra na OS se houver saldo suficiente, mas **a baixa do estoque só acontece na aprovação** (o saldo é conferido de novo nesse momento).

**No código:** `Part` (aggregate root), `PartType { Part, Supply }`, `PartCode`, `StockMovement`, `StockMovementType { Entry, Deduction }`, `Quantity`, rotas `/api/admin/parts`.

## Ordem de Serviço (OS)

**Definição:** registro de um atendimento de um veículo, do recebimento à entrega, com os serviços, peças, orçamento e histórico de status.

**No sistema:**
- Aberta com CPF/CNPJ do cliente, placa do veículo e descrição do problema (mínimo de 3 caracteres). O sistema atribui um **número sequencial** que o cliente usa para acompanhar.
- O veículo precisa pertencer ao cliente e **não pode ter outra OS aberta** (aberta = qualquer status exceto Entregue).
- O total da OS é o total do último orçamento enviado.
- Registra `ExecutionStartedAt` (aprovação), `CompletedAt` (finalização) e `DeliveredAt` (entrega).

**No código:** `ServiceOrder` (aggregate root), `ServiceOrderNumber`, `ServiceOrderOpeningPolicy`, rotas `/api/admin/service-orders`.

## Item da OS

**Definição:** linha de serviço (`ServiceItem`) ou de peça/insumo (`PartItem`) da OS, com descrição, preço unitário e quantidade copiados do catálogo.

**No sistema:** itens só entram ou saem com a OS **Em diagnóstico**. Serviço usa quantidade inteira maior que zero; peça usa `Quantity`. O total do item é preço × quantidade, arredondado meio para cima em centavos.

## Status

| Status (enunciado) | No código | Significado |
|---|---|---|
| Recebida | `Received` | OS aberta, aguardando o mecânico |
| Em diagnóstico | `InDiagnosis` | Mecânico avalia e monta os itens; volta para cá quando o orçamento é rejeitado |
| Aguardando aprovação | `WaitingApproval` | Orçamento enviado, aguardando decisão do cliente |
| Em execução | `InProgress` | Orçamento aprovado e serviço em andamento |
| Finalizada | `Completed` | Serviço concluído, veículo aguardando retirada |
| Entregue | `Delivered` | Veículo devolvido; OS encerrada |

Transições permitidas: Recebida → Em diagnóstico → Aguardando aprovação → Em execução → Finalizada → Entregue, e Aguardando aprovação → Em diagnóstico (rejeição). Qualquer outra resulta em `InvalidTransition` (HTTP 409). Cada mudança grava um `StatusHistory` com status anterior, novo, usuário, motivo e data.

## Orçamento

**Definição:** proposta de valor da OS, gerada automaticamente pela soma dos serviços e das peças, enviada ao cliente para aprovação.

**No sistema:**
- Gerado no envio (`SendQuote`) e só com a OS Em diagnóstico e ao menos um item. Guarda total de serviços, total de peças e total geral.
- Cada envio cria uma **versão** (1, 2, 3…). Depois de uma rejeição, o atendente ajusta os itens e envia a versão seguinte.
- Status do orçamento: `Pending`, `Approved`, `Rejected`.
- A resposta do envio devolve o **token** do link. No banco fica só o hash SHA-256; o token é de uso único e é apagado quando o orçamento é decidido.

**No código:** `Quote`, `QuoteStatus`, `ApprovalToken`, `SendQuoteCommand`.

## Aprovação e rejeição

- **Aprovação** (pelo link do cliente ou no balcão pelo atendente): OS vai para Em execução, `ExecutionStartedAt` é gravado e o estoque das peças é **baixado na mesma transação**. Se qualquer peça não tiver saldo, nada é gravado.
- **Rejeição:** exige motivo. A OS volta para Em diagnóstico e o motivo fica no orçamento e no histórico.
- Decidir um orçamento que não está aguardando aprovação resulta em conflito (409). O token usado ou desconhecido resulta em não encontrado (404).

**No código:** `ApproveQuoteCommand`, `ApproveQuoteByTokenCommand`, `RejectQuoteCommand`, `RejectQuoteByTokenCommand`, `QuoteApprovalPolicy`.

## Baixa de estoque

**Definição:** saída de peças do estoque por consumo em uma OS. Ocorre na aprovação, agrupando itens da mesma peça, e registra `StockMovement` do tipo `Deduction` com o motivo `serviceOrder:<número>`.

**No código:** `Part.DeductForServiceOrder`, `QuoteApprovalPolicy`.

## Tempo médio de execução

**Definição:** média do tempo entre a aprovação do orçamento (`ExecutionStartedAt`) e a finalização (`CompletedAt`) das OS finalizadas ou entregues, em minutos. Aceita filtro por data de finalização.

**No código:** `GetAverageExecutionTimeQuery`, rota `GET /api/admin/metrics/average-execution-time` (somente Admin).

## Consulta pública

**Definição:** leitura anônima do andamento da OS, informando número da OS e CPF/CNPJ no corpo (dado pessoal nunca vai na URL). Devolve status, itens e orçamento.

**No código:** `LookupServiceOrderQuery`, `PublicServiceOrderDto`, rota `POST /api/public/service-orders/lookup`.

## Usuário e perfis

| Perfil | No código | Pode |
|---|---|---|
| Administrador | `Role.Admin` | Tudo: cadastros, catálogo, peças, estoque, usuários e métricas |
| Atendente | `Role.Attendant` | Cadastrar clientes e veículos, abrir OS, enviar orçamento, decidir no balcão e entregar |
| Mecânico | `Role.Mechanic` | Iniciar diagnóstico, incluir e remover itens e finalizar a OS |

- Login com e-mail e senha devolve um JWT. Senhas são guardadas com BCrypt. Limite de 5 tentativas de login por minuto por IP.
- Desativar um usuário bloqueia o login sem apagar o registro. O último Admin ativo não pode ser desativado, excluído nem rebaixado, e ninguém altera a própria conta por essas ações.

**No código:** `User`, `Role`, `UserAdministrationPolicy`, `LoginCommand`, `Policies`.

## Regras que valem para todo o sistema

- **Result pattern:** erros esperados voltam como `Result` com um `Error` de catálogo (código estável e mensagem em português), mapeado para ProblemDetails.
- **Soft delete e auditoria:** `deletedAt`, `createdAt` e `updatedAt` em todas as entidades; consultas ignoram excluídos.
- **Transação e Outbox:** cada comando termina em `IUnitOfWork.CommitAsync`, que grava o agregado e os eventos de domínio na tabela Outbox na mesma transação. Um serviço em segundo plano entrega os eventos aos handlers depois.
- **Dinheiro:** `Money` não negativo, duas casas, arredondamento meio para cima.

## Subdomínios

| Tipo | Subdomínio | Por quê | No código |
|---|---|---|---|
| Principal | Ordens de Serviço e Orçamentos | Diferencia a oficina: conduzir a OS de ponta a ponta com status rastreável | `ServiceOrders` |
| Suporte | Cadastros | Cliente e veículo, necessários à OS | `Customers` |
| Suporte | Catálogo de serviços | Preços e tempos que alimentam o orçamento | `Catalog` |
| Suporte | Peças, insumos e estoque | Saldo e movimentações que a aprovação consome | `Inventory` |
| Genérico | Identidade e acesso | Login, JWT e perfis | `Identity` |
| Genérico | Notificações e Outbox | Aviso de conclusão (log) e entrega assíncrona de eventos | `INotifier`, `Messaging/Outbox` |

## Bounded contexts e relações

| Contexto | Relação | Padrão | Como integra no código |
|---|---|---|---|
| Cadastros (U) → Ordens de Serviço (D) | A OS guarda `CustomerId` e `VehicleId` | Cliente-Fornecedor | `ServiceOrder.CustomerId`, `ServiceOrderOpeningPolicy` |
| Catálogo (U) → Ordens de Serviço (D) | A OS copia nome e preço do serviço | Conformista | `ServiceItem.Create` |
| Estoque (U) ↔ Ordens de Serviço (D) | Peça valida saldo na inclusão e baixa na aprovação | Parceria (mesma transação) | `ServiceOrder.AddPartItem`, `QuoteApprovalPolicy` |
| Ordens de Serviço → Notificações | Evento `ServiceOrderCompleted` pelo Outbox | Publicação de eventos | `NotifyCustomerOnServiceOrderCompleted` → `INotifier` |
| Todos → Common | Tipos compartilhados | Shared Kernel | `Money`, `Quantity`, `Email`, `Result`, `AggregateRoot` |

## Atores

| Ator | Papel | No código |
|---|---|---|
| Cliente | Consulta a OS e decide o orçamento pelo link (anônimo) | rotas `/api/public` |
| Atendente | Recepção, orçamento, decisão no balcão e entrega | `Role.Attendant` |
| Mecânico | Diagnóstico, itens e finalização | `Role.Mechanic` |
| Administrador | Cadastros, catálogo, estoque, usuários e métricas | `Role.Admin` |
| Sistema | Baixa de estoque e processamento do Outbox | `QuoteApprovalPolicy`, `OutboxProcessor` |

## Agregados e entidades

| Agregado (raiz) | Entidades internas | Invariantes principais |
|---|---|---|
| `ServiceOrder` | `ServiceItem`, `PartItem`, `Quote`, `StatusHistory` | Itens só em diagnóstico; transições só pela máquina de status; orçamento só com itens; rejeição com motivo |
| `Part` | `StockMovement` | Saldo nunca negativo; entrada com motivo; nome, unidade e estoque mínimo válidos |
| `Service` | — | Preço e tempo positivos; inativo não é alterado |
| `Customer` | — | CPF/CNPJ válido e único |
| `Vehicle` | — | Placa válida e única; ano entre 1900 e ano+1 |
| `User` | — | E-mail único; último Admin ativo protegido |

## Objetos de valor

| Objeto de valor | Onde | O que garante |
|---|---|---|
| `Money` | `Domain/Common` | Não negativo, duas casas, arredondamento meio para cima |
| `Quantity` | `Domain/Common` | Maior que zero, até 3 casas |
| `Email` | `Domain/Common` | Formato válido |
| `Document` | `Domain/Customers` | CPF ou CNPJ com dígitos verificadores |
| `LicensePlate` | `Domain/Customers` | Placa antiga ou Mercosul |
| `VehicleYear` | `Domain/Customers` | Ano de 1900 até o ano seguinte |
| `Phone`, `Address` | `Domain/Customers` | Telefone e endereço válidos |
| `PartCode` | `Domain/Inventory` | Código normalizado em maiúsculas |
| `ServiceOrderNumber` | `Domain/ServiceOrders` | Número sequencial positivo |
| `ApprovalToken` | `Domain/ServiceOrders` | Hash SHA-256 do token do link |

## Comandos

| Contexto | Comandos |
|---|---|
| Ordens de Serviço | `OpenServiceOrderCommand`, `StartDiagnosisCommand`, `AddServiceItemCommand`, `AddPartItemCommand`, `RemoveItemCommand`, `SendQuoteCommand`, `ApproveQuoteCommand`, `ApproveQuoteByTokenCommand`, `RejectQuoteCommand`, `RejectQuoteByTokenCommand`, `CompleteServiceOrderCommand`, `DeliverServiceOrderCommand` |
| Cadastros | `CreateCustomerCommand`, `UpdateCustomerCommand`, `DeleteCustomerCommand`, `CreateVehicleCommand`, `UpdateVehicleCommand`, `DeleteVehicleCommand` |
| Catálogo | `CreateServiceCommand`, `UpdateServiceCommand`, `DeactivateServiceCommand`, `ReactivateServiceCommand` |
| Estoque | `CreatePartCommand`, `UpdatePartCommand`, `RegisterStockEntryCommand`, `DeactivatePartCommand`, `ReactivatePartCommand` |
| Identidade | `LoginCommand`, `CreateUserCommand`, `UpdateUserCommand`, `DeactivateUserCommand`, `ReactivateUserCommand`, `DeleteUserCommand` |

**Consultas (read models):** `ListServiceOrdersQuery`, `GetServiceOrderByIdQuery`, `LookupServiceOrderQuery`, `GetAverageExecutionTimeQuery`, além de `List…`, `Get…ById` e buscas por CPF/CNPJ ou e-mail dos demais contextos.

## Eventos de domínio

| Evento | Quem levanta | Quem trata (pelo Outbox, depois do commit) |
|---|---|---|
| `ServiceOrderOpened` | `ServiceOrder.Open` | — (fica registrado no Outbox) |
| `QuoteSent` | `ServiceOrder.SendQuote` | — |
| `QuoteApproved` | `ServiceOrder.ApproveQuote` | — |
| `QuoteRejected` | `ServiceOrder.RejectQuote` | — |
| `ServiceOrderCompleted` | `ServiceOrder.Complete` | `NotifyCustomerOnServiceOrderCompleted` (aviso em log, *To be implemented*: e-mail ou SMS) |

Entrada e baixa de estoque, entrega do veículo e mudanças de status são fatos registrados no próprio agregado (`StockMovement`, `StatusHistory`), sem evento de domínio.

## Políticas e regras de negócio

| Regra | Onde |
|---|---|
| Um veículo tem no máximo uma OS aberta e precisa ser do cliente | `ServiceOrderOpeningPolicy` |
| Itens só entram ou saem com a OS Em diagnóstico | `ServiceOrder.AddServiceItem` / `AddPartItem` / `RemoveItem` |
| Serviço e peça precisam estar ativos; peça precisa ter saldo | `ServiceOrder.AddServiceItem`, `AddPartItem` |
| Orçamento só com ao menos um item | `ServiceOrder.SendQuote` |
| Rejeição exige motivo | `ServiceOrder.RejectQuote` |
| Aprovação só com a OS Aguardando aprovação | `ServiceOrder.ApproveQuote` |
| Baixa de estoque na aprovação, na mesma transação, tudo ou nada | `QuoteApprovalPolicy` |
| Token do link é de uso único | `Quote.Approve`, `Quote.Reject` (apagam o token) |
| Cliente ou veículo com OS aberta não é excluído | `RegistrationDeletionPolicy` |
| Último Admin ativo não sai | `UserAdministrationPolicy` |
| Excluir ou desativar nunca apaga fisicamente | soft delete e `isActive` |

## Termos ambíguos resolvidos

| Termo | Adotado | Motivo |
|---|---|---|
| Aprovar × Autorizar | **Aprovar** o orçamento | Termo do enunciado; no código, `Approve` |
| Reprovar × Recusar × Rejeitar | **Rejeitar** | O enunciado fala "aprovação ou rejeição"; no código, `Reject` |
| Baixa × Reserva | **Baixa** | O estoque só é movimentado na aprovação (`Deduction`) |
| Finalizada × Entregue | Dois status distintos | Finalizada = serviço pronto; Entregue = veículo devolvido |
| Peça × Insumo | Mesmo cadastro, `PartType` diferente | Enunciado trata como "peças e insumos" com um só controle de estoque |
| `Service` | Serviço do catálogo | O sufixo *Service* fica proibido em classes técnicas |
| Execução | Da aprovação até a finalização | Define o tempo médio de execução |

## Documentos relacionados

- [Índice da documentação DDD](README.md)
- [Rastreabilidade documentação → código](rastreabilidade.md)
- [Event Storming em PDF](event-storming.pdf)
- [Diagramas em PDF](diagramas.pdf) (subdomínios, mapa de contextos, modelo de domínio, status da OS, Domain Storytelling e C4)

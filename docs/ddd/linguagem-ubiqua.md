# Linguagem ubíqua · Oficina mecânica

> [!NOTE]
> **Como ler este dicionário.** Cada termo traz a **definição**, as **observações do processo atual** e o que vale **no sistema**. O "processo atual" vem do Domain Storytelling *as-is* (as cenas da oficina antes do sistema), desenhado em [`docs/arquitetura/DomainStorytelling_mecanicaOS.drawio`](../arquitetura/DomainStorytelling_mecanicaOS.drawio) e exportado em [`diagramas.pdf`](diagramas.pdf).
> A linha **No código** indica o nome real usado na implementação (classe, método, propriedade, enum, rota ou arquivo do front com caminho).
> O enunciado do Tech Challenge faz o papel de especialista do domínio: quando o processo atual e o enunciado divergem, o sistema segue o enunciado.

## Cliente

**Definição:** Pessoa física ou jurídica, identificada por **CPF ou CNPJ**, que traz um Veículo para a Oficina relatando um ou mais Defeitos, confere e decide sobre o Orçamento e paga pelo Serviço executado.

**No sistema:**
- A identificação por CPF ou CNPJ é exigida pelo enunciado. O documento é sempre exibido mascarado.
- O Cliente não faz login: usa o **Portal do cliente** pelo link que recebe e a **consulta pública** da O.S. (número + CPF/CNPJ).
- **Consentimento:** autorização do Cliente para receber mensagens por WhatsApp e/ou e-mail. Sem consentimento, o sistema não envia.
- **Revelar dado:** mostrar CPF/CNPJ, telefone ou e-mail sem máscara exige um motivo e fica registrado na auditoria (LGPD).
- **Situação do cliente:** Ativo (veio nos últimos 90 dias), O.S. aberta, ou Sem retorno (mais de 90 dias sem vir).
- **Frota:** Cliente com 3 ou mais Veículos; pode ter desconto automático.
- **Importação de clientes:** envio de planilha CSV com nome, documento, telefone e e-mail. Linhas válidas viram Clientes; as erradas voltam com o motivo.
- **Exportação LGPD:** arquivo com todos os dados pessoais que a Oficina guarda de um Cliente, para atender o direito de acesso.

**No código:** `Customer` / `Document` (`DocumentType { Cpf, Cnpj }`), `Customer.ConsentWhatsApp` e `Customer.ConsentEmail`, `SensitiveDataAccess` (`RevealCustomerDataCommand`), `ImportCustomersCommand` e `ExportCustomerDataCommand` (`BackgroundJob.CustomerImport` / `BackgroundJob.CustomerDataExport`)

---
## Veículo

**Definição:** Bem automotor pertencente a um Cliente, trazido à Oficina para diagnóstico e reparo. É o objeto sobre o qual recaem os Defeitos relatados e o Diagnóstico realizado pela Oficina.

**No sistema:**
- O cadastro guarda placa (antiga ou Mercosul), marca, modelo, ano e **quilometragem**, que nunca diminui.
- O histórico de atendimentos anteriores vem das Ordens de Serviço do Veículo.
- Um Veículo só pode ter uma Ordem de Serviço aberta por vez.
- **Transferência de veículo:** passar o Veículo para outro Cliente (ex.: venda do carro).

**No código:** `Vehicle` / `LicensePlate`

---
## Defeito

**Definição:** Problema ou anomalia identificada em um Veículo (ex.: "barulho estranho", "vidro não sobe"). Pode ter duas origens possíveis: **relatado pelo Cliente** no momento da abertura da Ordem de Serviço, ou **identificado pela Oficina** durante o Diagnóstico.

**No sistema:**
- **Defeito relatado (Relato):** o que o Cliente contou na chegada, registrado na abertura da O.S., em texto livre.
- **Defeito identificado pela Oficina:** registrado pelo Mecânico no texto do **Diagnóstico**, junto com checklist e fotos. As **sugestões do diagnóstico** propõem Serviços e Peças a partir desse texto. Não existe uma lista estruturada de Defeitos.

**No código:** `ServiceOrder.ProblemDescription` (relato), `ServiceOrder.Diagnosis` (`SaveDiagnosisCommand`) e `SuggestItemsQuery` (identificado pela Oficina)

---
## Diagnóstico (do Defeito/Veículo)

> Termo anterior: **Análise**. Foi substituído por Diagnóstico para seguir o enunciado (status "Em diagnóstico").

**Definição:** Atividade técnica realizada pela Oficina, na qual o Veículo é examinado fisicamente em conjunto com o(s) Defeito(s) relatados pelo Cliente, servindo de base para a geração do Orçamento.

**Observações do processo atual:**

- Acontece dentro da cena "Orçamento da Ordem de Serviço", antes do Orçamento propriamente dito ser gerado.
- É durante o Diagnóstico que a Oficina confirma os Defeitos relatados e, potencialmente, identifica problemas adicionais que o Cliente não mencionou originalmente (ver **Defeito**).

**No sistema:**
- Corresponde ao status **Em diagnóstico**.
- O Mecânico registra o **checklist** do carro (ok, atenção, problema), **fotos** (que o Cliente vê no portal) e o texto do diagnóstico.
- **Enviar para orçamento:** o Mecânico indica os Serviços e Peças/Insumos e passa a O.S. para a recepção montar o Orçamento.
- Serviços, Peças e Insumos só podem ser incluídos ou removidos da O.S. enquanto ela está Em diagnóstico.

**Observação de nomenclatura:** Não confundir com a **Conferência do Orçamento**, feita posteriormente pelo Cliente.

**No código:** `ServiceOrder.StartDiagnosis` (`StartServiceCommand`), `ServiceOrderStatus.InDiagnosis`, `ServiceOrder.Checklist` (`UpdateChecklistCommand`), `ServiceOrderPhoto` (`AddPhotoCommand`, rota `GET /api/admin/service-orders/{id}/photos/{photoId}`), `SendToQuoteCommand`

---
## Orçamento

**Definição:** Proposta de preço gerada pela Oficina após o Diagnóstico, contendo os Serviços, Peças e Insumos necessários para o reparo. É enviada ao Cliente, que a confere e decide por Aprovação ou Recusa.

**No sistema:**
- **Rascunho:** o Orçamento é montado pela recepção a partir dos itens indicados no Diagnóstico, com Pacotes, Serviços e Peças do catálogo. Enquanto é rascunho, pode ser alterado à vontade.
- **Montador:** tela onde se escolhem Pacotes, Serviços e Peças, quantidades e desconto.
- **Desconto automático:** regra da Oficina aplicada sozinha (cliente fiel, frota ou primeira visita).
- **Desconto manual:** desconto digitado, com motivo.
- **Limite de desconto:** desconto máximo que cada perfil pode dar. Acima dele, abre-se um **pedido de aprovação de desconto**, que um Gerente ou Administrador aprova ou recusa.
- **Plano de pagamento:** formas aceitas (Pix, crédito, débito, dinheiro, boleto) e número de parcelas. O pagamento com cartão pelo portal é *To be implemented* (ver **Pagamento**).
- **Envio:** por WhatsApp, e-mail ou link, com validade (1, 3, 7 ou 15 dias). O envio leva a O.S. para **Aguardando aprovação**. O e-mail sai depois do commit; se falhar, o envio fica marcado como falho e pode ser reenviado.
- **Nova versão:** depois de uma Recusa, cria-se a versão seguinte do mesmo Orçamento.

**No código:** `Estimate` (orçamento montado e enviado; comandos `CreateQuoteCommand`, `SaveQuoteDraftCommand`, `SendQuoteCommand`, `CreateQuoteVersionCommand`) e `Quote` (registro do orçamento dentro da O.S.), `ApprovalToken`, `EstimatePricing` (descontos e limites, no domínio)

### Conferência (do Orçamento)
**Definição:** Atividade realizada pelo Cliente ao examinar o conteúdo do Orçamento (Serviços, Peças e Insumos propostos) antes de tomar sua decisão de Aprovação ou Recusa.

**No sistema:** acontece no **Portal do cliente**, que mostra andamento, fotos do diagnóstico e o Orçamento.

### Aprovação (do Orçamento)

**Definição:** Decisão positiva do Cliente em relação ao Orçamento apresentado, que autoriza a Oficina a iniciar a Execução da Ordem de Serviço.

**No sistema:**
- Pode ser feita pelo Cliente no Portal, registrada presencialmente pela recepção ou feita pelo quadro. Todas as entradas passam pelo mesmo caso de uso (`QuoteDecider`): o Orçamento e a O.S. nunca ficam com respostas diferentes.
- A aprovação só vale com a O.S. Recebida, Em diagnóstico ou Aguardando aprovação; fora disso falha com `quotes.notAwaitingApproval`.
- **Aprovação parcial:** o Cliente aprova só parte dos itens. O Orçamento fica **Aprovado** com os itens aceitos e os demais saem da O.S. diretamente, sem registrar uma Recusa nem voltar para Em diagnóstico.
- Na mesma transação: a O.S. vai para **Em execução** e é feita a **baixa** das Peças e Insumos aceitos no estoque (ver **Consumir**).

**No código:** `Estimate.Approve(acceptedLineIds, terms)`, `ServiceOrder.ApproveQuote(QuoteScope, ...)`, `QuoteApprovalPolicy`, `QuoteDecider`; comandos `ApproveQuoteAtDeskCommand`, `ApproveQuoteFromPortalCommand`, `ApproveQuoteCommand`, `TransitionServiceOrderCommand`; evento `QuoteApproved`

### Recusa (do Orçamento)

> Termo anterior neste dicionário: **Reprovação**. O sistema e o enunciado usam **Recusa / rejeição**.

**Definição:** Decisão negativa do Cliente em relação ao Orçamento apresentado.

**No sistema:**
- O motivo da Recusa é obrigatório.
- A O.S. volta para **Em diagnóstico**, e a Oficina pode montar uma **nova versão** do Orçamento.

**No código:** `Estimate.Reject`, `ServiceOrder.RejectQuoteAnswer`, `RejectQuoteAtDeskCommand`, `RejectQuoteFromPortalCommand`; evento `QuoteRejected`

---
## Peça

**Definição:** Componente físico do Veículo substituído ou instalado pela Oficina na execução de um Serviço (ex.: pastilha de freio, filtro, correia).

**No sistema:**
- Peça e Insumo são termos irmãos ("peças e insumos", como no enunciado). Ambos têm custo, preço de venda, estoque mínimo e controle de estoque.
- **Desativar:** tirar do uso sem apagar. Peça usada em Pacote ativo não pode ser desativada. **Excluir** (`DELETE /api/admin/parts/{id}`) é desativar, com a mesma regra.

**No código:** `Part` com `PartType.Part`

---
## Insumo

**Definição:** Material de consumo utilizado pela Oficina na execução de um Serviço (ex.: óleo, fluido de freio, graxa), vendido por litro ou unidade.

**Observações do processo atual:**
- É consumido durante a Execução da Ordem de Serviço.

**No sistema:**
- Peça e Insumo são termos irmãos (ver **Peça**).
- A baixa no estoque acontece na Aprovação do Orçamento (ver **Consumir**).

**No código:** `Part` com `PartType.Supply`

> Observação: no código, Peça e Insumo vivem na mesma entidade `Part`, diferenciados por `PartType`. O nome `Part` é técnico e cobre os dois termos.

---
## Estoque e Compras

**Definição:** Controle da quantidade disponível de cada Peça e Insumo na Oficina e da reposição junto aos fornecedores.

**No sistema:**
- **Entrada:** Peças que chegaram, lançadas manualmente, por código de barras ou lendo o XML da **nota fiscal eletrônica (NF-e)**.
- **Saída / baixa:** Peças que saíram (uso numa O.S., perda, devolução).
- **Movimentação:** histórico de entradas, saídas, baixas e ajustes, com o saldo após cada uma.
- **Estoque mínimo:** abaixo dele, a Peça entra em **reposição** e aparece nas **sugestões de compra**. O evento `StockBelowMinimum` é levantado na hora da baixa, da saída ou do ajuste que cruza o mínimo; a varredura periódica continua como rede de segurança.
- **Inventário / contagem:** conferência física do estoque. O Gerente aprova as diferenças, que viram **ajustes**.
- **Fornecedor:** empresa que vende Peças, com prazo de entrega.
- **Pedido de compra:** pedido enviado ao Fornecedor por e-mail, que pode nascer de uma sugestão de compra.
- Uma Peça ou Insumo sem saldo suficiente não pode ser incluído na O.S.

**No código:** `Part.StockQuantity`, `Part.MinimumStock`, `StockMovement` (`StockMovementType { Entry, Deduction, Exit, Adjustment }`), `StockCount`, `Supplier`, `PurchaseOrder`; comandos `RegisterEntryCommand`, `RegisterExitCommand`, `ConfirmInvoiceCommand`, `OpenCountCommand`, `DecideCountCommand`, `CreatePurchaseOrderCommand`; eventos `StockDeducted` e `StockBelowMinimum`

---
## Serviço

**Definição:** Tipo de trabalho/reparo que a Oficina oferece e executa sobre um Veículo (ex.: troca de óleo, alinhamento, reparo elétrico).

**Observações do processo atual:**

- Cadastro também informal: "às vezes está escrito até na parede".
- É o resultado/alvo da Execução e da Finalização.

**No sistema:**
- O catálogo guarda preço base, tempo estimado, **comissão** do Mecânico e **garantia**.
- **Peças usuais:** Peças que normalmente acompanham o Serviço e ajudam a montar o Orçamento.
- **Pacote:** combo de Serviços e Peças com preço fechado, mais barato que a soma dos itens (a **economia**).
- **Histórico de preço:** toda mudança de preço ou custo fica registrada, com quem mudou e quando.
- **Margem:** quanto sobra da venda depois do custo das Peças.
- Serviço desativado não pode ser incluído em O.S.; os itens que já estão nas ordens continuam como estão. **Excluir** (`DELETE /api/admin/services/{id}`) é desativar.

**No código:** `Service`, `ServiceUsualPart`, `Package`, `PriceChange`

---
## Consumir

**Definição:** Ação pela qual a Oficina registra o uso de uma Peça ou Insumo em uma Ordem de Serviço, dando baixa no Estoque.

**Observações do processo atual:**
- No processo atual, o uso acontece durante a Execução e a baixa não tem mecânica definida.

**No sistema:**
- A baixa é feita na **Aprovação do Orçamento**, na mesma transação que muda a O.S. para Em execução. Assim, o que foi aprovado fica separado para a O.S. e o estoque nunca fica inconsistente com ela.
- Saídas fora de uma O.S. (perda, devolução) são registradas como **Saída**.

> [!IMPORTANT]
> **Termo adotado: Baixa.** O código registra uma baixa (`StockMovementType.Deduction`), e todo o material (dicionário, Event Storming, README e mensagens) usa **Baixa**. O termo antigo "Reserva" não é mais usado.

**No código:** `Part.DeductForServiceOrder` (evento `StockDeducted`), `StockMovementType.Deduction`, `Part.RegisterExit`, `QuoteApprovalPolicy`

---

## Executar

**Definição:** Ação pela qual a Oficina efetivamente realiza o trabalho de reparo (o Serviço) sobre o Veículo, dentro dos limites definidos pelo Orçamento aprovado.

**No sistema (comportamento atual):**
- A Execução começa na Aprovação do Orçamento: a O.S. vai para **Em execução** e o início da execução é registrado nesse momento.
- **Cronômetro:** o Mecânico marca o tempo de trabalho em cada O.S. É a base da **produtividade** por Mecânico.
- O **tempo médio de execução** é medido do início da execução (Aprovação) até a Finalização, como pede o enunciado ("tempo médio de execução dos serviços").

> [!IMPORTANT]
> **Decisão do grupo.** O tempo médio de execução continua medido de `ExecutionStartedAt` (Aprovação) até `CompletedAt` (Finalização). Ele inclui a espera do Veículo na fila da oficina, e isso é intencional: é o tempo que o Cliente percebe. O tempo efetivamente trabalhado é medido pelo **Cronômetro** (`TimeEntry`) e aparece na **produtividade** de cada Mecânico.

**No código:** `ServiceOrderStatus.InProgress`, `ServiceOrder.ExecutionStartedAt`, `ServiceOrder.ExecutionTime`, `GetAverageExecutionTimeQuery`, `TimeEntry` (`StartTimerCommand` / `StopTimerCommand`), `GetProductivityQuery`

---
## Finalização

**Definição:** Conclusão do trabalho de reparo pela Oficina. Indica que o Veículo está pronto para ser retirado e pago pelo Cliente.

**No sistema:**
- Corresponde ao status **Finalizada**.
- Registra a data de término, usada no tempo médio de execução.
- O.S. finalizada e ainda não paga entra em **A receber**.
- O aviso automático ao Cliente ainda não é enviado: hoje fica só registrado em log (*To be implemented*).

**No código:** `ServiceOrder.Complete` (`FinishServiceCommand`), `ServiceOrderStatus.Completed`, evento `ServiceOrderCompleted`

---
## Entrega

**Definição:** Devolução física do Veículo ao Cliente, após a Finalização.

**No sistema:**
- Corresponde ao status **Entregue**.
- É o marco que encerra a Ordem de Serviço; depois dela, a O.S. não muda mais.

**No código:** `ServiceOrder.Deliver` (`TransitionServiceOrderCommand`), `ServiceOrderStatus.Delivered`

---
## Pagamento

**Definição:** Quitação, pelo Cliente, do valor dos Serviços, Peças e Insumos da Ordem de Serviço.

**No sistema:**
- **Cobrança Pix:** gerada no Portal do cliente com código copia-e-cola e QR code, no valor aprovado. Vale 30 minutos.
- **Confirmação de pagamento:** aviso assinado (webhook) de que o Pix caiu. Ao confirmar, a cobrança levanta o evento `PaymentConfirmed`, e o Processador de Outbox emite o **Recibo**, avisa a equipe e dispara as automações, sempre depois do commit.

> [!WARNING]
> **A confirmação do Pix é simulada.** O webhook espera um contrato próprio (`{ txId }` com assinatura HMAC do `txId`) e o QR code é um Pix estático gerado pelo sistema (`PixCodes`). Nenhum banco ou PSP real chama a API nesse formato; hoje só a carga de demonstração confirma pagamentos. A integração com um PSP real é *To be implemented*.
- **Pagamento com cartão:** previsto no plano de pagamento, mas o portal ainda responde que não está disponível (*To be implemented*).
- **A receber:** O.S. finalizadas ainda não pagas.

**No código:** `PortalCharge` (`MarkPaid` levanta `PaymentConfirmed`), `CreatePixChargeCommand`, `ConfirmPixPaymentCommand` (rota `POST /api/public/payments/pix/webhook`), `IPixWebhookVerifier`, `IssueReceiptOnPaymentConfirmed`, `DocumentIssuer.ReceiptKind`

---
## Portal do cliente

**Definição:** Página que o Cliente abre pelo link do Orçamento, sem login, para acompanhar a O.S., conferir e responder ao Orçamento, pagar e baixar documentos.

**No sistema:**
- **Confirmação de identidade:** o Cliente digita os 3 últimos dígitos do telefone. Cada tentativa é contada antes da conferência, com controle de concorrência (`rowVersion`), e depois de 3 erros o link é **bloqueado**, mesmo com requisições em paralelo.
- **Limite de requisições:** todas as rotas públicas (`/api/public/**`) têm limite por IP; acima dele a resposta é 429.
- **Sessão do portal:** acesso de 1 hora depois da confirmação.
- **Consulta pública da O.S.:** página `/portal` onde o Cliente vê a situação da O.S. com o número e o CPF/CNPJ.

**No código:** `Estimate.RegisterPortalAttempt` / `Estimate.ConfirmPortalIdentity`, `ConfirmPortalIdentityCommand`, `PortalSession`, `GetPortalOrderQuery`, `LookupServiceOrderQuery` (rota `POST /api/public/service-orders/lookup`), `SecurityExtensions.PublicRateLimitPolicy`

---
## Documento emitido

**Definição:** Cópia congelada de uma O.S., de um Orçamento ou de um Recibo, no momento em que foi emitida.

**No sistema:**
- Cada documento tem um **código de verificação** único (ex.: `REC-11-3F9A1C7B2E40`, com 12 caracteres aleatórios; os códigos antigos de 6 caracteres continuam válidos) e pode ser baixado em PDF.
- **Verificar autenticidade:** página pública `/v/{código}` que confirma se o documento é verdadeiro.
- **Recibo:** comprovante de pagamento, emitido quando o Pix é confirmado.

**No código:** `IssuedDocument`, `DocumentIssuer`, `GetDocumentQuery`, `VerifyDocumentQuery`, `SendDocumentCommand`

---
## Ordem de Serviço (O.S.)

**Definição:** Conceito central que amarra todo o atendimento de um Cliente/Veículo, desde o relato do(s) Defeito(s) até a Entrega do Veículo. Funciona como o "processo" ou "caso" que agrupa Cliente, Veículo, Defeito(s), Orçamento, Peças e Insumos consumidos e Serviços executados.

**Observações do processo atual:**

- O termo aparece nos **títulos** das cenas ("Orçamento da Ordem de Serviço", "Execução da Ordem de Serviço"), mas nunca como um objeto de trabalho desenhado explicitamente dentro das histórias, reforçando que é um conceito implícito que precisa ser explicitado no modelo.

**No sistema:**
- A O.S. passa a existir na **abertura**, com Cliente, Veículo, Defeito relatado e **previsão de entrega** (depois dela, a O.S. fica **atrasada**), no status **Recebida**.
- A O.S. fica aberta até a **Entrega**.
- Cada O.S. tem um número sequencial, usado pelo Cliente para acompanhá-la.
- **Quadro:** visão em colunas por status, onde se arrasta o cartão da O.S. Atualiza em tempo real.
- **Versão da O.S.:** número que muda a cada alteração e evita que duas pessoas sobrescrevam o trabalho uma da outra (**conflito de versão**).

**No código:** `ServiceOrder` / `ServiceOrderNumber`, `ServiceOrder.PromisedAt`, `ServiceOrder.Version`, `OpenServiceOrderCommand`, `ListServiceOrdersQuery`, quadro no front em `web/src/app/features/service-orders/components/board-view.ts`

---
## Status

**Definição no processo atual:** **Não existe como conceito formal hoje.** Não há nenhum controle estruturado sobre em qual etapa se encontra uma Ordem de Serviço ou um Veículo: a informação vive apenas na memória de quem está executando o trabalho ("vê lá qual é o próximo").

**Por que está no dicionário:** essa ausência é uma das dores centrais citadas no case ("dificuldade em acompanhar o status dos serviços") e uma das principais justificativas para o sistema novo.

**No sistema:** a O.S. tem seis status, definidos pelo enunciado. O status só muda por uma ação, e cada mudança fica registrada no histórico de status.

| Status | No código | Ação que leva a ele |
|---|---|---|
| Recebida | `Received` | Abrir a O.S. |
| Em diagnóstico | `InDiagnosis` | Iniciar o Diagnóstico, ou Recusa do Orçamento |
| Aguardando aprovação | `WaitingApproval` | Enviar o Orçamento |
| Em execução | `InProgress` | Aprovação do Orçamento |
| Finalizada | `Completed` | Finalizar a O.S. (aguardando retirada e pagamento) |
| Entregue | `Delivered` | Entregar o Veículo |

O Orçamento tem status próprios: **Aguardando cliente** (`Pending`), **Aprovado** (`Approved`) e **Recusado** (`Rejected`).

**No código:** `ServiceOrderStatus`, `StatusHistory`, `QuoteStatus`, `EstimateStatus`, evento `ServiceOrderStatusChanged`

---
## Usuário, Perfis e Equipe

**Definição:** Pessoa da Oficina que acessa o sistema. Cada Usuário tem um perfil (alçada):
- **Administrador:** pode tudo: equipe, configurações, segurança, exclusões e reprocessar falhas.
- **Gerente:** opera a oficina inteira, vê custos, relatórios, financeiro e automações, e aprova descontos acima do limite do Atendente. Não mexe em usuários nem em configurações sensíveis.
- **Atendente:** recepção: Clientes, Veículos, abertura de O.S., Orçamentos, documentos e painel. Desconto até o seu limite.
- **Mecânico:** modo mecânico: suas O.S., checklist, fotos, Diagnóstico, Cronômetro, envio para orçamento e contagem de estoque. Não vê custos nem dados financeiros.

**No sistema:**
- **Usuário ativo / inativo:** inativo não entra no sistema, mas o histórico continua. Desativar não é excluir.
- **Permissões por perfil:** o que cada perfil pode fazer. Hoje são fixas (somente leitura).
- **Convite:** link enviado por e-mail para um novo funcionário criar a própria senha. Vale 72 horas e uma única vez.
- **Sessão:** período em que a pessoa fica conectada; pode ser encerrada pelo Administrador.
- **Verificação em duas etapas (2FA):** código de 6 dígitos de um aplicativo autenticador pedido no login, quando ligado.
- **Reautenticação:** antes de mudar algo sensível, o sistema pede a senha de novo. Vale 5 minutos e uma única vez.
- **Perfil profissional:** especialidades, comissão, limite de desconto e horário de trabalho de cada funcionário.
- **Produtividade:** O.S. atendidas, horas trabalhadas, ocupação, faturamento e retrabalho por Mecânico.

**Observação:** no processo atual, todos esses papéis aparecem como "a Oficina".

**No código:** `User` / `Role { Admin, Attendant, Mechanic, Manager }`, `UserInvitation`, `UserSession`, `TwoFactorChallenge` e `User.TwoFactorSecret` (cifrado com Data Protection), `Reauthentication`, `TeamProfile`

---
## Gestão da oficina

Termos de apoio à gestão, que não vêm do processo atual nem do enunciado.

- **Painel:** resumo do dia, da semana ou do mês: faturamento, O.S. abertas, taxa de aprovação de orçamentos, a receber, faturamento diário, serviços mais vendidos e o pátio.
- **Aviso de atenção:** algo que precisa de ação (estoque baixo, orçamento sem resposta, O.S. atrasada, revisões vencendo), com botão de ação direta.
- **Pátio:** carros na oficina, agrupados por status.
- **Financeiro:** recebido no período, a receber, comissões por Mecânico e totais por forma de pagamento.
- **Ticket médio:** valor médio de cada O.S. entregue.
- **Relatório:** indicador (faturamento, quantidade de O.S., peças, ticket médio, margem, comissão) agrupado por mês, semana, Mecânico, Serviço, categoria, forma de pagamento ou Cliente.
- **Relatório salvo e agendado:** relatório guardado com nome, que pode ser enviado toda semana por e-mail.
- **Exportação:** relatório em Excel (XLSX) ou PDF.
- **Pergunte aos seus dados:** a pessoa escreve uma pergunta em português e o sistema monta o relatório. Roda dentro do sistema, sem inteligência artificial externa.
- **Automação:** regra "quando acontecer X, se Y, espere Z e faça W" (ex.: lembrar o Cliente de um Orçamento sem resposta depois de 2 dias). Envio automático por WhatsApp: *To be implemented* (hoje só registra em log).
- **Gatilho:** evento que dispara a automação: orçamento enviado, O.S. mudou de etapa, estoque abaixo do mínimo, O.S. entregue, revisão vencendo, pagamento atrasado ou pagamento recebido.
- **Fila:** trabalhos feitos em segundo plano (notificações, documentos, pagamentos, importações, relatórios, automações).
- **Falha:** trabalho da fila que não deu certo; o Administrador pode **reprocessar**.
- **Notificação:** aviso para a equipe dentro do sistema (sino), com cópia por e-mail se a pessoa quiser.
- **Tempo real:** o quadro de O.S. e as notificações atualizam sozinhos quando algo muda.
- **Plataforma:** plano contratado, usuários em uso contra o limite, uso do mês e módulos ativos.

**No código:** `GetDashboardSnapshotQuery`, `AttentionItemDto` (`HandleAttentionItemCommand`), `YardCountDto`, `GetFinanceSummaryQuery`, `ReceivableDto`, `FinanceKpisDto.AverageTicket`, `RunReportQuery`, `SavedReport`, `ExportReportCommand` (`ReportExportRequest`), `SuggestReportQuery`, `AutomationRule`, `AutomationTriggers`, `QueueHealthDto`, `QueueFailureDto` (`RetryQueueFailureCommand`), `Notification` (evento `NotificationCreated`), `RealtimeHub`, `PlatformDto` (`GetPlatformQuery`)

---
## Configurações

- **Dados da oficina:** nome, CNPJ, endereço, logo e horário de funcionamento.
- **Regras de desconto:** descontos automáticos e limites por perfil.
- **Pagamentos:** formas aceitas, parcelas, juros, taxas e **chave Pix**.
- **Modelos de mensagem:** textos enviados ao Cliente com variáveis (`{cliente}`, `{veiculo}`, `{placa}`, `{total}`, `{link}`, `{prazo}`) e a janela de horário de envio.
- **Segurança:** 2FA obrigatório, tempo de inatividade e bloqueio por tentativas.
- **Auditoria:** registro de quem alterou o quê: configurações, descontos aprovados, dados revelados e ajustes de estoque.
- **Integrações fiscais:** situação da NFS-e, NF-e e NFC-e, em planejamento (*To be implemented*).

**No código:** `WorkshopSettings`, `DiscountSettings`, `PaymentSettings`, `MessageSettings`, `SecuritySettings`, `AuditEntry`, `FiscalSettings`

---
## Regras que valem para todo o sistema

- **Dado pessoal nunca vai no endereço da página.** CPF, CNPJ, e-mail, telefone e placa são enviados só dentro da requisição.
- **Nada sigiloso vai para o log.**
- **Excluir é desativar.** Os registros ficam guardados para o histórico.
- **Tudo tem dono e data.** Cada registro guarda quando foi criado e alterado.
- **Valores em reais** com duas casas, arredondados para cima a partir de meio centavo.
- **Datas** guardadas em UTC e mostradas no horário de Brasília.
- **Mensagens de erro** em português e com um código fixo (ex.: `portal.locked`).

---

## Perguntas em aberto — consolidado

Lista das dúvidas identificadas nas cenas de Domain Storytelling, com a resposta adotada no sistema:

1. ~~A Ordem de Serviço passa a existir a partir do cadastro de Cliente/Veículo/Defeito, ou só a partir do Orçamento?~~ **Respondida:** na abertura, com Cliente, Veículo, Defeito relatado e previsão de entrega (status Recebida).
2. A anotação do Defeito/atendimento é feita em papel? Existe algum padrão? *(Processo atual; não se aplica ao sistema, onde o Defeito relatado é registrado na abertura da O.S.)*
3. ~~O cadastro de Insumos é lançado em planilha ou outro tipo de controle?~~ **Respondida:** no sistema há cadastro de Peças e Insumos com controle de estoque, entrada por NF-e e pedidos de compra.
4. ~~A Oficina costuma encontrar Defeitos durante a Análise que o Cliente não relatou originalmente?~~ **Respondida:** sim; ficam registrados no texto do Diagnóstico, com checklist e fotos, e viram itens do Orçamento.
5. ~~O envio do Orçamento precisa de assinatura, ou uma mensagem (ex.: WhatsApp) já basta?~~ **Respondida:** basta o envio por WhatsApp, e-mail ou link; o Cliente responde no Portal, sem assinatura.
6. ~~O que acontece quando o Cliente reprova o Orçamento?~~ **Respondida:** o motivo é obrigatório, a O.S. volta para Em diagnóstico e pode ser montada uma nova versão do Orçamento.
7. ~~Como é feita a baixa de um Insumo depois de consumido?~~ **Respondida:** na Aprovação do Orçamento, na mesma transação.
8. ~~Como a Oficina sabe que a Execução foi iniciada e qual Veículo está sendo trabalhado no momento?~~ **Respondida:** a Execução começa na Aprovação (`ExecutionStartedAt`); o Cronômetro registra o trabalho do Mecânico e o Quadro mostra o pátio. O tempo médio de execução vai da Aprovação à Finalização (ver a decisão em **Executar**).
9. ~~A Ordem de Serviço é finalizada no pagamento, ou em outro marco (ex.: entrega física do veículo)?~~ **Respondida:** a O.S. é encerrada na Entrega; Finalizada (aguardando retirada e pagamento) e Entregue são status diferentes.

---

## Subdomínios

| Tipo | Subdomínio | Por quê | No código |
|---|---|---|---|
| Principal | Ordens de Serviço e Orçamentos | É o que diferencia a oficina: abrir, diagnosticar, orçar, aprovar, executar e entregar com status rastreável. | `ServiceOrders`, `Quotes` |
| Suporte | Cadastros (clientes e veículos) | Necessário para abrir a O.S., mas não diferencia o negócio. | `Customers`, `Privacy` |
| Suporte | Catálogo | Serviços, peças, insumos e pacotes com preço; alimenta o orçamento. | `Catalog` |
| Suporte | Estoque e Compras | Controle de saldo, baixa, reposição, NF-e e pedidos de compra. | `Inventory` |
| Suporte | Gestão | Painel, relatórios, financeiro e automações apoiam decisões. | `Dashboard`, `Reports`, `Automations`, `Finance` |
| Genérico | Identidade e Equipe | Login, perfis, convites e 2FA são resolvidos da mesma forma em qualquer sistema. | `Identity`, `Team`, `Users` |
| Genérico | Notificações e e-mail | Avisos internos e SMTP. | `Notifications` |
| Genérico | Pagamentos (Pix simulado) | Cobrança e confirmação; um PSP real substituiria a simulação. | `Portal` (`PortalCharge`) |
| Genérico | Documentos, Configurações e Auditoria | Emissão de PDF, parâmetros da oficina e trilha de auditoria. | `Documents`, `Settings`, `Audit` |

## Bounded contexts e relações

| Contexto | Relação | Padrão | Como integra no código |
|---|---|---|---|
| Ordens de Serviço (U) → Orçamentos (D) | O Orçamento referencia a O.S. pelo id | Cliente-Fornecedor | `Estimate.ServiceOrderId`; a decisão passa por `QuoteDecider` |
| Catálogo (U) → Ordens de Serviço (D) | A O.S. copia nome e preço do item | Conformista | `ServiceItem.Create(service...)`, `PartItem.Create(part...)` |
| Estoque (U) ↔ Ordens de Serviço (D) | Baixa na aprovação | Parceria (mesma transação) | `QuoteApprovalPolicy` chama `Part.DeductForServiceOrder` |
| Cadastros (U) → Ordens de Serviço (D) | A O.S. referencia Cliente e Veículo | Cliente-Fornecedor | `ServiceOrder.CustomerId`, `ServiceOrder.VehicleId`, `ServiceOrderOpeningPolicy` |
| Ordens de Serviço / Estoque / Portal (U) → Gestão (D) | Eventos de domínio pelo Outbox | Cliente-Fornecedor (publicação de eventos) | `OnServiceOrderStatusChanged`, `OnStockBelowMinimum`, `IssueReceiptOnPaymentConfirmed` |
| Gestão → Notificações | Avisos à equipe | Cliente-Fornecedor | `ITeamNotificationPublisher` → `NotificationCreated` → `DeliverNotificationOnCreated` |
| Todos → Common | Tipos compartilhados | Shared Kernel | `Money`, `Quantity`, `Email`, `Result`, `AggregateRoot` |
| ViaCEP (U) → Cadastros (D) | Consulta de endereço | Camada Anticorrupção (ACL) | `IAddressLookup` / `ViaCepAddressLookup` |
| SMTP (U) → Notificações (D) | Envio de e-mail | ACL | `IEmailSender` / `SmtpEmailSender` |
| Instituição de Pagamento Pix (U) → Portal (D) | Cobrança e confirmação (simuladas) | ACL | `IPixCodes` / `PixCodes`, `IPixWebhookVerifier` / `HmacPixWebhookVerifier` |
| WhatsApp | Envio manual por link `wa.me` | Caminhos Separados (*To be implemented*) | `SendQuoteResultDto.WhatsAppUrl` |

## Atores

| Ator | Papel | No código |
|---|---|---|
| Cliente | Relata o defeito, confere e responde o orçamento, paga | `Customer`, Portal (`ConfirmPortalIdentityCommand`) |
| Atendente | Recepção: cadastros, abertura da O.S., orçamento | `Role.Attendant` |
| Mecânico | Diagnóstico, checklist, fotos, cronômetro, execução | `Role.Mechanic` |
| Gerente | Aprova descontos acima do limite, gestão e relatórios | `Role.Manager` |
| Administrador | Equipe, configurações e segurança | `Role.Admin` |
| Fornecedor | Recebe pedidos de compra e entrega peças (NF-e) | `Supplier`, `PurchaseOrder` |
| Sistemas externos | ViaCEP, SMTP, Instituição de Pagamento Pix (simulada), WhatsApp (link) | `ViaCepAddressLookup`, `SmtpEmailSender`, `PixCodes` |

## Agregados e entidades

| Agregado (raiz) | Entidades internas | Invariantes principais |
|---|---|---|
| `ServiceOrder` | `ServiceItem`, `PartItem`, `Quote`, `StatusHistory`, `TimeEntry`, `ChecklistItem` | Uma O.S. aberta por veículo; itens só em diagnóstico; transições só pela máquina de status; até 12 fotos |
| `ServiceOrderPhoto` | — | Tipo JPG/PNG/WebP e tamanho máximo; carregada sob demanda |
| `Estimate` | `EstimateLine`, `DiscountApproval`, `EstimateDelivery` (registros de valor) | Só responde estando pendente; desconto acima do limite precisa de aprovação; 3 erros bloqueiam o portal |
| `Part` | `StockMovement` | Saldo nunca negativo; baixa só com saldo; peça usada em pacote ativo não desativa |
| `Customer` | — | CPF/CNPJ válido e único |
| `Vehicle` | — | Placa válida e única; quilometragem nunca diminui |
| `Service` | `ServiceUsualPart` | Preço e comissão válidos |
| `Package` | `PackageItem` | Itens ativos e preço fechado |
| `User` | — | E-mail único; bloqueio por tentativas; último admin não sai |
| `StockCount`, `PurchaseOrder`, `Supplier` | `CountLine`, `PurchaseOrderLine` | Contagem aprovada vira ajuste; pedido só envia uma vez |
| `PortalCharge`, `Notification` | — | Cobrança paga uma vez (`PaymentConfirmed`); notificação levanta `NotificationCreated` |

## Objetos de valor

| Objeto de valor | Onde | O que garante |
|---|---|---|
| `Money` | `Domain/Common/ValueObjects` | Não negativo, duas casas, arredondamento meio para cima (único ponto de arredondamento monetário) |
| `Measure` | `Domain/Common/ValueObjects` | Arredondamento de valores que não são dinheiro (percentual, horas, dias) |
| `Quantity` | `Domain/Common/ValueObjects` | Maior que zero, até 3 casas |
| `Email` | `Domain/Common/ValueObjects` | Formato válido |
| `Document` | `Domain/Customers/ValueObjects` | CPF ou CNPJ com dígitos verificadores |
| `Phone`, `Address`, `LicensePlate`, `VehicleYear` | `Domain/Customers/ValueObjects` | Telefone, endereço, placa antiga ou Mercosul, ano válido |
| `PartCode` | `Domain/Inventory/ValueObjects` | Código da peça |
| `ServiceOrderNumber`, `ApprovalToken`, `QuoteScope` | `Domain/ServiceOrders/ValueObjects` | Número sequencial, hash do token, itens aceitos com total |

## Comandos

| Contexto | Comandos |
|---|---|
| Ordens de Serviço | `OpenServiceOrderCommand`, `StartServiceCommand`, `UpdateChecklistCommand`, `AddPhotoCommand`, `SaveDiagnosisCommand`, `SendToQuoteCommand`, `TransitionServiceOrderCommand`, `ApproveQuoteCommand`, `ApproveQuoteByTokenCommand`, `StartTimerCommand`, `StopTimerCommand`, `FinishServiceCommand` |
| Orçamentos | `CreateQuoteCommand`, `SaveQuoteDraftCommand`, `RequestDiscountApprovalCommand`, `DecideDiscountApprovalCommand`, `SendQuoteCommand`, `RetryQuoteDeliveryCommand`, `ApproveQuoteAtDeskCommand`, `RejectQuoteAtDeskCommand`, `CreateQuoteVersionCommand` |
| Portal | `ConfirmPortalIdentityCommand`, `ApproveQuoteFromPortalCommand`, `RejectQuoteFromPortalCommand`, `CreatePixChargeCommand`, `RefreshChargeStatusCommand`, `RequestCardPaymentCommand`, `ConfirmPixPaymentCommand` |
| Cadastros | `CreateCustomerCommand`, `UpdateCustomerCommand`, `DeleteCustomerCommand`, `RevealCustomerDataCommand`, `CreateVehicleCommand`, `UpdateVehicleCommand`, `TransferVehicleCommand`, `DeleteVehicleCommand` |
| Catálogo | `CreateServiceCommand`, `UpdateServiceCommand`, `DeactivateServiceCommand`, `ReactivateServiceCommand`, `CreatePartCommand`, `UpdatePartCommand`, `DeactivatePartCommand`, `ReactivatePartCommand`, `CreatePackageCommand`, `UpdatePackageCommand`, `DeactivatePackageCommand`, `ReactivatePackageCommand` |
| Estoque e Compras | `RegisterEntryCommand`, `RegisterExitCommand`, `ConfirmInvoiceCommand`, `OpenCountCommand`, `RecordCountCommand`, `DecideCountCommand`, `CreateSupplierCommand`, `UpdateSupplierCommand`, `CreatePurchaseOrderCommand`, `SendPurchaseOrderCommand` |
| Identidade e Equipe | `LoginCommand`, `VerifyTwoFactorCommand`, `RefreshSessionCommand`, `LogoutCommand`, `ReauthenticateCommand`, `SetupTwoFactorCommand`, `EnableTwoFactorCommand`, `DisableTwoFactorCommand`, `AcceptInvitationCommand`, `CreateUserCommand`, `InviteUserCommand`, `ResendInvitationCommand`, `UpdateUserCommand`, `DeactivateUserCommand`, `ReactivateUserCommand`, `DeleteUserCommand`, `UpdateTeamProfileCommand`, `UpdateRolePermissionsCommand` |
| Gestão | `HandleAttentionItemCommand`, `SaveDashboardLayoutCommand`, `CreateSavedReportCommand`, `UpdateSavedReportCommand`, `DeleteSavedReportCommand`, `ScheduleSavedReportCommand`, `ExportReportCommand`, `CreateAutomationRuleCommand`, `UpdateAutomationRuleCommand`, `SetAutomationRuleStatusCommand`, `DeleteAutomationRuleCommand`, `RetryQueueFailureCommand`, `ImportCustomersCommand`, `ExportCustomerDataCommand` |
| Notificações, Documentos e Configurações | `MarkNotificationReadCommand`, `MarkAllNotificationsReadCommand`, `SaveNotificationPreferencesCommand`, `SendDocumentCommand`, `SaveWorkshopSettingsCommand`, `SaveWorkshopLogoCommand`, `SaveDiscountSettingsCommand`, `SavePaymentSettingsCommand`, `SaveMessageSettingsCommand`, `SaveSecuritySettingsCommand`, `RevokeSessionCommand` |

## Eventos de domínio

| Evento | Quem levanta | Quem trata (pelo Outbox, depois do commit) |
|---|---|---|
| `ServiceOrderOpened` | `ServiceOrder.Open` | — |
| `ServiceOrderStatusChanged` | toda mudança de status da `ServiceOrder` | `OnServiceOrderStatusChanged` (tempo real, automações, aviso à equipe) |
| `QuoteSent` | `ServiceOrder.SendQuote` | `OnQuoteSent` (automações) |
| `QuoteApproved` | `ServiceOrder.ApproveQuote` | `OnQuoteApproved` (aviso à equipe) |
| `QuoteRejected` | `ServiceOrder.RejectQuote` | `OnQuoteRejected` (aviso à equipe) |
| `ServiceOrderCompleted` | `ServiceOrder.Complete` | `NotifyCustomerOnServiceOrderCompleted` (aviso ao cliente em log, *To be implemented*) |
| `StockDeducted` | `Part.DeductForServiceOrder` | — (registra a baixa no Outbox para integrações) |
| `StockBelowMinimum` | `Part` ao cruzar o mínimo (baixa, saída ou ajuste) | `OnStockBelowMinimum` (automação de reposição e aviso) |
| `PaymentConfirmed` | `PortalCharge.MarkPaid` | `IssueReceiptOnPaymentConfirmed` (recibo, aviso, automação) e `BroadcastPaymentConfirmed` (tempo real) |
| `NotificationCreated` | `Notification.Create` | `DeliverNotificationOnCreated` (tempo real e e-mails, preferências em lote) |

## Políticas e regras de negócio

| Regra | Onde |
|---|---|
| Um veículo tem no máximo uma O.S. aberta | `ServiceOrderOpeningPolicy`, índice `ux_serviceOrders_openPerVehicle` |
| Itens só entram ou saem com a O.S. Em diagnóstico | `ServiceOrder.AddServiceItem` / `AddPartItem` / `RemoveItem` |
| Peça só entra na O.S. com saldo suficiente | `ServiceOrder.AddPartItem` (`Part.HasAvailable`) |
| Desconto manual acima do limite do perfil precisa de aprovação do Gerente | `Estimate.RequestApproval`, `Estimate.EnsureReadyToSend` |
| Recusa exige motivo (mínimo 5 caracteres) | `Estimate.Reject`, `ServiceOrder.RejectQuote` |
| Aprovação só com a O.S. aguardando resposta | `ServiceOrder.ApproveQuote`, `QuoteErrors.NotAwaitingApproval` |
| Aprovação parcial tira da O.S. os itens não aceitos, sem recusa | `Estimate.Approve`, `ServiceOrder.ApproveQuote(QuoteScope...)` |
| Baixa no estoque na aprovação, na mesma transação | `QuoteApprovalPolicy` |
| 3 erros bloqueiam o link do portal | `Estimate.MaxPortalAttempts`, `Estimate.RegisterPortalAttempt` |
| Rotas públicas têm limite de requisições por IP | `SecurityExtensions.PublicRateLimitPolicy` |
| Até 12 fotos por O.S. | `ServiceOrder.MaxPhotos`, `ServiceOrder.RegisterPhoto` |
| Excluir é desativar | `DeactivateServiceCommand`, `DeactivatePartCommand`, soft delete de clientes e veículos |
| Efeitos (e-mail, tempo real, recibo) só depois do commit | Outbox: `OutboxWriter`, `OutboxBatchProcessor` |

## Termos ambíguos resolvidos

| Antes | Adotado | Motivo |
|---|---|---|
| Análise | **Diagnóstico** | Segue o status "Em diagnóstico" do enunciado |
| Reprovação | **Recusa** | O sistema e o enunciado usam recusa/rejeição |
| Reserva | **Baixa** | O código registra `StockMovementType.Deduction` |
| `Estimate` × `Quote` | `Estimate` é o orçamento montado e enviado; `Quote` é o registro do orçamento dentro da O.S. | Dois agregados com papéis diferentes |
| `Service` | Entidade do catálogo (serviço prestado) | Por isso o sufixo `Service` é proibido em classes técnicas (`AutomationSweeper`, `ReportScheduler`, handlers) |

## Documentos relacionados

- [Rastreabilidade documentação → código](rastreabilidade.md)
- [Event Storming em PDF](event-storming.pdf)
- [Diagramas em PDF](diagramas.pdf) (subdomínios, Domain Storytelling, mapa de contextos, modelo de domínio e C4)
- [Índice da documentação DDD](README.md)

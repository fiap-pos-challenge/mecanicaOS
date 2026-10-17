# Linguagem ubíqua — mecanicaOS

Este documento explica, em palavras simples, **o que o sistema faz** e **o que cada termo significa**. Ele vale igual para quem programa, para quem cuida das regras do negócio e para quem nunca viu o código. Cada termo mostra também o nome usado no código, entre parênteses, para que todos falem a mesma língua.

---

## 1. O que é o sistema

O **mecanicaOS** é o sistema de gestão de uma oficina mecânica. Ele acompanha o carro do cliente desde a chegada até a entrega:

1. o cliente chega e a recepção **abre uma ordem de serviço**;
2. o mecânico **diagnostica** o problema e indica serviços e peças;
3. a recepção monta e **envia o orçamento** ao cliente;
4. o cliente **aprova ou recusa** pelo celular, no **portal do cliente**, e pode **pagar com Pix**;
5. o mecânico **executa** o serviço e marca o tempo trabalhado;
6. a oficina **finaliza** e **entrega** o carro, com **recibo** e documentos com código de verificação.

Em volta desse fluxo, o sistema cuida de **clientes e veículos**, **catálogo de serviços e peças**, **estoque**, **equipe**, **painel de indicadores**, **financeiro**, **relatórios**, **automações**, **notificações** e **configurações**.

---

## 2. Pessoas e perfis (alçadas)

| Termo | Código | O que pode fazer |
|---|---|---|
| **Administrador** | `Admin` | Tudo: equipe, configurações, segurança, plataforma, exclusões e reprocessar falhas. |
| **Gerente** | `Manager` | Opera a oficina inteira, vê custos, relatórios, financeiro, automações e aprova descontos acima do limite do atendente. Não mexe em usuários nem em configurações sensíveis. |
| **Atendente** | `Attendant` | Recepção: clientes, veículos, abertura de OS, orçamentos, documentos, painel. Desconto até o seu limite. |
| **Mecânico** | `Mechanic` | Modo mecânico: suas OS, checklist, fotos, diagnóstico, cronômetro, envio para orçamento e contagem de estoque. Não vê custos nem dados financeiros. |
| **Cliente** | — | Não tem login. Usa o **portal do cliente** pelo link que recebe e a **consulta pública** da OS. |
| **Convite** | `UserInvitation` | Link enviado por e-mail para um novo funcionário criar a própria senha. Vale 72 horas e uma única vez. |
| **Usuário ativo / inativo** | `IsActive` | Inativo não entra no sistema, mas o histórico continua. Desativar não é excluir. |
| **Reautenticação** | `Reauthentication` | Antes de mudar algo sensível (dados da oficina, pagamentos, segurança, sessões), o sistema pede a senha de novo. Vale 5 minutos e uma única vez. |
| **Verificação em duas etapas (2FA)** | `TwoFactor` | Código de 6 dígitos de um aplicativo autenticador pedido no login, quando ligado. |
| **Sessão** | `UserSession` | Período em que a pessoa fica conectada. Pode ser encerrada pelo administrador. |

---

## 3. Cadastros

| Termo | Código | Significado |
|---|---|---|
| **Cliente** | `Customer` | Pessoa física (CPF) ou empresa (CNPJ) dona de veículos. |
| **Documento** | `Document` | CPF ou CNPJ do cliente. Sempre validado e sempre exibido mascarado. |
| **Revelar dado** | `SensitiveDataAccess` | Mostrar CPF, telefone ou e-mail sem máscara. Exige um motivo e fica registrado na auditoria (LGPD). |
| **Consentimento** | `MessagingConsent` | Autorização do cliente para receber mensagens por WhatsApp e/ou e-mail. Sem consentimento, o sistema não envia. |
| **Veículo** | `Vehicle` | Carro do cliente: placa, marca, modelo, ano e quilometragem. A quilometragem nunca diminui. |
| **Placa** | `LicensePlate` | Placa antiga ou Mercosul. Nunca aparece em endereço de página (URL). |
| **Transferência de veículo** | `Transfer` | Passar o veículo para outro cliente (ex.: venda do carro). |
| **Frota** | `Fleet` | Cliente com 3 ou mais veículos. Pode ter desconto automático. |
| **Situação do cliente** | `Situation` | **Ativo** (veio nos últimos 90 dias), **OS aberta** (tem carro na oficina) ou **Sem retorno** (mais de 90 dias sem vir). |
| **Importação de clientes** | `CustomerImport` | Envio de planilha CSV com nome, documento, telefone e e-mail. Linhas válidas viram clientes; as erradas voltam com o motivo. |
| **Exportação LGPD** | `CustomerDataExport` | Arquivo com todos os dados pessoais que a oficina guarda de um cliente, para atender o direito de acesso. |

---

## 4. Catálogo

| Termo | Código | Significado |
|---|---|---|
| **Serviço** | `Service` | Mão de obra vendida: preço base, tempo estimado, comissão do mecânico e garantia. |
| **Peça** | `Part` (`PartType.Part`) | Item físico vendido e controlado no estoque. Tem custo, preço de venda e estoque mínimo. |
| **Insumo** | `Part` (`PartType.Supply`) | Material de consumo, como óleo e fluido, vendido por litro ou unidade. |
| **Peças usuais** | `UsualParts` | Peças que normalmente acompanham um serviço. Ajudam a montar o orçamento. |
| **Pacote** | `Package` | Combo de serviços e peças com preço fechado, mais barato que a soma dos itens (a **economia**). |
| **Histórico de preço** | `PriceChange` | Toda mudança de preço ou custo fica registrada com quem mudou e quando. |
| **Margem** | `Margin` | Quanto sobra da venda depois do custo das peças. |
| **Desativar** | `Deactivate` | Tirar do uso sem apagar. Peça usada em pacote ativo não pode ser desativada. |

---

## 5. Ordem de serviço (OS)

| Termo | Código | Significado |
|---|---|---|
| **Ordem de serviço (OS)** | `ServiceOrder` | Registro de tudo o que acontece com um veículo numa visita. Tem número sequencial. |
| **Abrir OS** | `Open` | Recepção registra cliente, veículo, relato do problema e previsão de entrega. Um veículo só pode ter uma OS aberta. |
| **Relato** | `ProblemDescription` | O que o cliente contou na chegada. |
| **Previsão de entrega** | `PromisedAt` | Data e hora combinadas. Depois dela, a OS fica **atrasada**. |
| **Checklist** | `Checklist` | Lista de verificação do carro (ok, atenção, problema). |
| **Fotos** | `ServiceOrderPhoto` | Fotos do diagnóstico. O cliente vê no portal. |
| **Diagnóstico** | `Diagnosis` | O que o mecânico encontrou. |
| **Sugestões do diagnóstico** | `SuggestItems` | Serviços e peças sugeridos a partir do texto do diagnóstico. |
| **Enviar para orçamento** | `SendToQuote` | Mecânico indica os itens e passa a OS para a recepção orçar. |
| **Cronômetro** | `TimeEntry` | Tempo de trabalho do mecânico em cada OS. Base da produtividade. |
| **Versão da OS** | `Version` | Número que muda a cada alteração. Evita que duas pessoas sobrescrevam o trabalho uma da outra (**conflito de versão**). |
| **Quadro** | `Board` | Visão em colunas por situação, onde se arrasta o cartão da OS. |

**Situações da OS** (`ServiceOrderStatus`), sempre nesta ordem:

| Situação | Código | Quando |
|---|---|---|
| Recebida | `Received` | OS aberta, carro aguardando o mecânico. |
| Em diagnóstico | `InDiagnosis` | Mecânico avaliando. Volta para cá quando o cliente recusa o orçamento. |
| Aguardando aprovação | `WaitingApproval` | Orçamento enviado ao cliente. |
| Em execução | `InProgress` | Cliente aprovou; peças reservadas e serviço em andamento. |
| Finalizada | `Completed` | Serviço pronto, aguardando retirada e pagamento. |
| Entregue | `Delivered` | Carro devolvido. A OS não muda mais. |

---

## 6. Orçamento

| Termo | Código | Significado |
|---|---|---|
| **Orçamento** | `Estimate` / `Quote` | Proposta de preço enviada ao cliente, ligada a uma OS. |
| **Rascunho** | `Draft` | Orçamento ainda sendo montado. Pode ser alterado à vontade. |
| **Montador** | `QuoteEditor` | Tela onde se escolhem pacotes, serviços e peças, quantidade e desconto. |
| **Desconto automático** | `DiscountRule` | Regra da oficina aplicada sozinha: cliente fiel, frota ou primeira visita. |
| **Desconto manual** | `ManualDiscount` | Desconto digitado, com motivo. |
| **Limite de desconto** | `DiscountLimit` | Desconto máximo que cada perfil pode dar sem pedir aprovação. |
| **Pedido de aprovação de desconto** | `DiscountApproval` | Quando o desconto passa do limite, um gerente ou administrador aprova ou recusa. |
| **Plano de pagamento** | `PaymentPlan` | Formas aceitas (Pix, crédito, débito, dinheiro, boleto) e número de parcelas. |
| **Envio** | `Delivery` | Mandar o orçamento por WhatsApp, e-mail ou link. Tem validade (1, 3, 7 ou 15 dias). |
| **Aprovação parcial** | `AcceptedLineIds` | O cliente pode aprovar só parte dos itens. Os outros saem da OS. |
| **Recusa** | `Reject` | O cliente recusa com um motivo. A OS volta para diagnóstico. |
| **Nova versão** | `NextVersion` | Depois de uma recusa, cria-se a versão 2 do mesmo orçamento. |

**Situações do orçamento**: **Aguardando cliente** (`Pending`), **Aprovado** (`Approved`), **Recusado** (`Rejected`).

---

## 7. Portal do cliente e pagamento

| Termo | Código | Significado |
|---|---|---|
| **Portal do cliente** | `Portal` | Página que o cliente abre pelo link do orçamento, sem login. Mostra andamento, fotos, orçamento, pagamento e documentos. |
| **Confirmação de identidade** | `Confirm` | O cliente digita os 3 últimos dígitos do telefone. Depois de 3 erros, o link é **bloqueado**. |
| **Sessão do portal** | `PortalSession` | Acesso de 1 hora depois da confirmação. |
| **Cobrança Pix** | `PortalCharge` | Código copia-e-cola e QR code com o valor aprovado. Vale 30 minutos. |
| **Confirmação de pagamento** | `PixWebhook` | Aviso assinado do banco de que o Pix caiu. Gera o **recibo** automaticamente. |
| **Consulta pública da OS** | `Lookup` | Página `/portal` onde o cliente vê a situação da OS com o número e o CPF/CNPJ. |

---

## 8. Documentos

| Termo | Código | Significado |
|---|---|---|
| **Documento emitido** | `IssuedDocument` | Cópia congelada de uma **OS**, de um **orçamento** ou de um **recibo**, no momento em que foi emitida. |
| **Código de verificação** | `Code` | Código único do documento (ex.: `REC-11-912C8A`). |
| **Verificar autenticidade** | `Verification` | Página pública `/v/{código}` que confirma se o documento é verdadeiro. |
| **PDF** | `Pdf` | Arquivo para baixar ou imprimir. |
| **Recibo** | `Receipt` | Comprovante de pagamento, emitido quando o Pix é confirmado. |

---

## 9. Estoque e compras

| Termo | Código | Significado |
|---|---|---|
| **Estoque** | `StockQuantity` | Quantidade de cada peça disponível. |
| **Estoque mínimo** | `MinimumStock` | Abaixo dele, a peça entra em **reposição**. |
| **Entrada** | `StockEntry` | Peças que chegaram (manual, por código de barras ou lendo o XML da **nota fiscal eletrônica**). |
| **Saída / baixa** | `StockExit` | Peças que saíram (uso numa OS, perda, devolução). |
| **Movimentação** | `StockMovement` | Histórico de entradas, saídas e ajustes de cada peça. |
| **Reserva** | `Reserved` | Peça separada para uma OS aprovada. |
| **Inventário / contagem** | `StockCount` | Conferência física do estoque. O gerente aprova as diferenças. |
| **Ajuste** | `Adjustment` | Correção do estoque vinda de uma contagem aprovada. |
| **Fornecedor** | `Supplier` | Empresa que vende peças, com prazo de entrega. |
| **Pedido de compra** | `PurchaseOrder` | Pedido enviado ao fornecedor. Pode nascer de uma **sugestão de compra**. |

---

## 10. Equipe

| Termo | Código | Significado |
|---|---|---|
| **Perfil profissional** | `TeamProfile` | Especialidades, comissão, limite de desconto e horário de trabalho de cada funcionário. |
| **Comissão** | `CommissionPercent` | Percentual sobre os serviços que o mecânico executa. |
| **Produtividade** | `Productivity` | OS atendidas, horas trabalhadas, ocupação, faturamento e retrabalho por mecânico. |
| **Permissões por perfil** | `RolePermissions` | O que cada perfil pode fazer. Hoje são fixas (somente leitura). |

---

## 11. Gestão

| Termo | Código | Significado |
|---|---|---|
| **Painel** | `Dashboard` | Resumo do dia, da semana ou do mês: faturamento, OS abertas, taxa de aprovação de orçamentos, valores a receber, faturamento diário, serviços mais vendidos e o pátio. |
| **Aviso de atenção** | `AttentionItem` | Algo que precisa de ação: estoque baixo, orçamento sem resposta, OS atrasada, revisões vencendo. Tem botão de ação direta. |
| **Pátio** | `Yard` | Carros na oficina por situação. |
| **Financeiro** | `Finance` | Recebido no período, a receber, comissões por mecânico e totais por forma de pagamento. |
| **A receber** | `Receivable` | OS finalizadas ainda não pagas. |
| **Ticket médio** | `AverageTicket` | Valor médio de cada OS entregue. |
| **Relatório** | `Report` | Indicador (faturamento, quantidade de OS, peças, ticket médio, margem, comissão) agrupado por mês, semana, mecânico, serviço, categoria, forma de pagamento ou cliente. |
| **Relatório salvo e agendado** | `SavedReport` | Relatório guardado com nome, que pode ser enviado toda semana por e-mail. |
| **Exportação** | `ReportExport` | Relatório em Excel (XLSX) ou PDF. |
| **Pergunte aos seus dados** | `Assistant` | Escreva uma pergunta em português e o sistema monta o relatório. Roda dentro do sistema, sem inteligência artificial externa. |
| **Automação** | `AutomationRule` | Regra "quando acontecer X, se Y, espere Z e faça W". Ex.: lembrar o cliente de um orçamento sem resposta depois de 2 dias. |
| **Gatilho** | `Trigger` | O evento que dispara a automação (orçamento enviado, OS mudou de etapa, estoque abaixo do mínimo, OS entregue, revisão vencendo, pagamento atrasado, pagamento recebido). |
| **Fila** | `Queue` | Trabalhos feitos em segundo plano (notificações, documentos, pagamentos, importações, relatórios, automações). |
| **Falha** | `Failure` | Trabalho que não deu certo. O administrador pode **reprocessar**. |
| **Notificação** | `Notification` | Aviso para a equipe dentro do sistema (sino), com cópia por e-mail se a pessoa quiser. |
| **Tempo real** | `Realtime` | O quadro de OS e as notificações atualizam sozinhos quando algo muda. |
| **Plataforma** | `Platform` | Plano contratado, usuários em uso contra o limite, uso do mês e módulos ativos. |

---

## 12. Configurações

| Termo | Código | Significado |
|---|---|---|
| **Dados da oficina** | `WorkshopSettings` | Nome, CNPJ, endereço, logo e horário de funcionamento. |
| **Regras de desconto** | `DiscountSettings` | Descontos automáticos e limites por perfil. |
| **Pagamentos** | `PaymentSettings` | Formas aceitas, parcelas, juros, taxas e **chave Pix**. |
| **Modelos de mensagem** | `MessageSettings` | Textos enviados ao cliente com variáveis `{cliente}`, `{veiculo}`, `{placa}`, `{total}`, `{link}`, `{prazo}`, e a janela de horário de envio. |
| **Segurança** | `SecuritySettings` | 2FA obrigatório, tempo de inatividade, bloqueio por tentativas. |
| **Auditoria** | `AuditEntry` | Registro de quem alterou o quê: configurações, descontos aprovados, dados revelados e ajustes de estoque. |
| **Integrações fiscais** | `FiscalSettings` | Situação da NFS-e, NF-e e NFC-e (em planejamento). |

---

## 13. Regras que valem para todo o sistema

- **Dado pessoal nunca vai no endereço da página.** CPF, CNPJ, e-mail, telefone e placa são enviados só dentro da requisição.
- **Nada sigiloso vai para o log.**
- **Excluir é desativar.** Os registros ficam guardados para o histórico.
- **Tudo tem dono e data.** Cada registro guarda quando foi criado e alterado.
- **Valores em reais** com duas casas, arredondados para cima a partir de meio centavo.
- **Datas** guardadas em UTC e mostradas no horário de Brasília.
- **Mensagens de erro** em português e com um código fixo (ex.: `portal.locked`).

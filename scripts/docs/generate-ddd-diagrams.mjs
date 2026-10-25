import { writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const out = join(root, 'docs', 'arquitetura');

const COLORS = {
  core: '#FFE0B2',
  supporting: '#C8E6C9',
  generic: '#E1E4E8',
  context: '#DCEBFA',
  external: '#F9B4D2',
  aggregate: '#FFF9B1',
  event: '#FFA930',
  rule: '#EAD9F2',
  value: '#A6DC73',
  hot: '#FF8A80',
  note: '#FFFDE7',
  object: '#FFFFFF',
};

const escape = (value) =>
  value.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');

class Page {
  constructor(id, name, width = 2400, height = 1500) {
    Object.assign(this, { id, name, width, height, cells: [], next: 0 });
  }

  key() {
    this.next += 1;
    return `${this.id}-${this.next}`;
  }

  vertex(value, style, x, y, width, height, parent = '1') {
    const id = this.key();
    this.cells.push(`<mxCell id="${id}" value="${escape(value)}" style="${style}" vertex="1" parent="${parent}"><mxGeometry x="${x}" y="${y}" width="${width}" height="${height}" as="geometry"/></mxCell>`);
    return id;
  }

  edge(value, source, target, style = '') {
    const id = this.key();
    const base = 'edgeStyle=orthogonalEdgeStyle;rounded=1;html=1;endArrow=blockThin;endFill=1;strokeColor=#555555;strokeWidth=1.5;fontSize=12;labelBackgroundColor=#FFFFFF;';
    this.cells.push(`<mxCell id="${id}" value="${escape(value)}" style="${base}${style}" edge="1" parent="1" source="${source}" target="${target}"><mxGeometry relative="1" as="geometry"/></mxCell>`);
    return id;
  }

  title(text, subtitle) {
    this.vertex(`<b style="font-size:24px">${text}</b><br><span style="font-size:14px;color:#555555">${subtitle}</span>`, 'text;html=1;align=left;verticalAlign=top;whiteSpace=wrap;', 40, 20, this.width - 80, 80);
  }

  card(text, fill, x, y, width = 280, height = 120, extra = '') {
    return this.vertex(text, `rounded=1;arcSize=6;whiteSpace=wrap;html=1;fillColor=${fill};strokeColor=#666666;fontSize=13;align=left;verticalAlign=top;spacing=10;${extra}`, x, y, width, height);
  }

  sticky(text, fill, x, y, width = 180, height = 90) {
    return this.vertex(`<b style="font-size:13px">${text}</b>`, `whiteSpace=wrap;html=1;strokeColor=none;shadow=1;fillColor=${fill};fontSize=12;spacing=8;`, x, y, width, height);
  }

  group(text, x, y, width, height) {
    return this.vertex(`<b>${text}</b>`, 'rounded=1;arcSize=3;whiteSpace=wrap;html=1;dashed=1;fillColor=none;strokeColor=#888888;verticalAlign=top;align=left;spacing=10;fontSize=14;', x, y, width, height);
  }

  actor(name, x, y) {
    return this.vertex(name, 'shape=umlActor;verticalLabelPosition=bottom;verticalAlign=top;html=1;outlineConnect=0;fillColor=#FEF445;strokeColor=#333333;fontSize=13;fontStyle=1;', x, y, 40, 70);
  }

  workObject(name, x, y, width = 170) {
    return this.vertex(name, 'shape=note;whiteSpace=wrap;html=1;size=14;fillColor=#FFFFFF;strokeColor=#333333;fontSize=12;', x, y, width, 60);
  }

  system(name, x, y, width = 170) {
    return this.vertex(name, 'rounded=1;whiteSpace=wrap;html=1;fillColor=#1168BD;strokeColor=#0B4884;fontColor=#FFFFFF;fontSize=12;fontStyle=1;', x, y, width, 60);
  }

  annotation(text, x, y, width = 260, hot = false) {
    return this.vertex(text, `shape=note;whiteSpace=wrap;html=1;size=12;fillColor=${hot ? COLORS.hot : COLORS.note};strokeColor=#999999;fontSize=11;align=left;spacing=6;`, x, y, width, 70);
  }

  legend(items, x, y) {
    this.vertex('<b>Legenda</b>', 'text;html=1;align=left;fontSize=14;', x, y, 200, 24);
    items.forEach(([label, style], index) => {
      this.vertex('', style, x, y + 32 + index * 40, 30, 26);
      this.vertex(label, 'text;html=1;align=left;verticalAlign=middle;fontSize=12;whiteSpace=wrap;', x + 40, y + 30 + index * 40, 320, 30);
    });
  }

  xml() {
    return `<diagram id="${this.id}" name="${escape(this.name)}"><mxGraphModel dx="1400" dy="900" grid="0" gridSize="10" guides="1" tooltips="1" connect="1" arrows="1" fold="1" page="1" pageScale="1" pageWidth="${this.width}" pageHeight="${this.height}" background="#FAFAF7" math="0" shadow="0"><root><mxCell id="0"/><mxCell id="1" parent="0"/>${this.cells.join('')}</root></mxGraphModel></diagram>`;
  }
}

const file = (pages) => `<mxfile host="app.diagrams.net" type="device">${pages.map((page) => page.xml()).join('')}</mxfile>\n`;

function subdomains() {
  const page = new Page('sub', '1. Subdomínios', 2200, 1100);
  page.title('Subdomínios · mecanicaOS', 'Classificação da Aula 01 (Principal, Suporte e Genérico). Cada bloco aponta a pasta do código e o motivo da classificação.');
  const columns = [
    ['Principal', COLORS.core, [
      ['Ordens de Serviço', 'Abrir, diagnosticar, executar, finalizar e entregar com status rastreável. É o que diferencia a oficina e resolve a dor central do enunciado.', 'Domain/ServiceOrders'],
      ['Orçamentos', 'Montar, enviar e decidir o orçamento (total, parcial ou recusa) com baixa atômica no estoque.', 'Domain/Quotes'],
    ]],
    ['Suporte', COLORS.supporting, [
      ['Cadastros', 'Clientes (CPF/CNPJ) e veículos (placa). Necessários para a O.S., sem diferencial próprio.', 'Domain/Customers · Privacy'],
      ['Catálogo', 'Serviços, peças, insumos e pacotes com preço e histórico.', 'Domain/Catalog'],
      ['Estoque e Compras', 'Saldo, baixa, reposição, NF-e, inventário e pedidos de compra.', 'Domain/Inventory'],
      ['Gestão', 'Painel, relatórios, financeiro e automações apoiam decisões.', 'Application/Dashboard · Reports · Finance · Automations'],
    ]],
    ['Genérico', COLORS.generic, [
      ['Identidade e Equipe', 'Login, perfis, convites, 2FA: igual em qualquer sistema.', 'Domain/Identity · Team'],
      ['Notificações e e-mail', 'Avisos internos e SMTP.', 'Domain/Notifications'],
      ['Pagamentos (Pix simulado)', 'Cobrança e confirmação; um PSP real substituiria a simulação.', 'Domain/Portal (PortalCharge)'],
      ['Documentos, Configurações e Auditoria', 'PDF com código de verificação, parâmetros e trilha de auditoria.', 'Domain/Documents · Settings · Audit'],
    ]],
  ];
  columns.forEach(([kind, fill, items], column) => {
    const x = 40 + column * 700;
    page.group(kind, x, 120, 660, 920);
    items.forEach(([name, why, code], index) => {
      page.card(`<b style="font-size:15px">${name}</b><br><br>${why}<br><br><span style="font-family:monospace;font-size:11px">${code}</span>`, fill, x + 20, 170 + index * 210, 620, 190);
    });
  });
  return page;
}

function contextMap() {
  const page = new Page('map', '2. Mapa de Contextos', 2600, 1500);
  page.title('Mapa de Contextos · mecanicaOS', 'Aula 04. Cada seta vai do contexto Upstream (U) para o Downstream (D), com o padrão de integração e como o código integra de verdade.');
  const ctx = (name, detail, x, y, fill = COLORS.context) =>
    page.card(`<b style="font-size:15px">${name}</b><br><span style="font-size:11px;font-family:monospace">${detail}</span>`, fill, x, y, 300, 110);
  const orders = ctx('Ordens de Serviço', 'Domain/ServiceOrders', 1050, 520, COLORS.core);
  const quotes = ctx('Orçamentos', 'Domain/Quotes', 1050, 860, COLORS.core);
  const customers = ctx('Cadastros e Privacidade', 'Domain/Customers · Privacy', 300, 300, COLORS.supporting);
  const catalog = ctx('Catálogo', 'Domain/Catalog', 1050, 180, COLORS.supporting);
  const inventory = ctx('Estoque e Compras', 'Domain/Inventory', 1800, 300, COLORS.supporting);
  const portal = ctx('Portal, Pagamentos e Documentos', 'Domain/Portal · Documents', 300, 860, COLORS.generic);
  const management = ctx('Gestão', 'Dashboard · Reports · Automations · Settings · Audit', 1800, 860, COLORS.supporting);
  const notifications = ctx('Notificações', 'Domain/Notifications', 1800, 1150, COLORS.generic);
  const identity = ctx('Identidade e Equipe', 'Domain/Identity · Team', 300, 1150, COLORS.generic);
  const shared = ctx('Shared Kernel', 'Domain/Common: Money, Quantity, Email, Result, AggregateRoot', 1050, 1200, COLORS.generic);
  const viaCep = page.card('<b>ViaCEP</b><br>[Sistema externo]', COLORS.external, 40, 120, 220, 80);
  const smtp = page.card('<b>Servidor SMTP</b><br>[Sistema externo]', COLORS.external, 2300, 1150, 240, 80);
  const pix = page.card('<b>Instituição de Pagamento (Pix)</b><br>[Sistema externo · confirmação simulada]', COLORS.external, 40, 1380, 300, 80);
  const whatsApp = page.card('<b>WhatsApp</b><br>[Sistema externo · To be implemented]', COLORS.external, 2300, 120, 260, 80, 'dashed=1;');
  page.edge('U → D · Cliente-Fornecedor<br>ServiceOrder.CustomerId / VehicleId', customers, orders);
  page.edge('U → D · Conformista<br>ServiceItem / PartItem copiam nome e preço', catalog, orders);
  page.edge('Parceria · mesma transação<br>QuoteApprovalPolicy → Part.DeductForServiceOrder', orders, inventory);
  page.edge('U → D · Cliente-Fornecedor<br>Estimate.ServiceOrderId · QuoteDecider', orders, quotes);
  page.edge('U → D · Cliente-Fornecedor<br>sessão e cobrança por EstimateId', quotes, portal);
  page.edge('U → D · eventos pelo Outbox<br>ServiceOrderStatusChanged · QuoteSent', orders, management);
  page.edge('U → D · eventos pelo Outbox<br>StockBelowMinimum', inventory, management);
  page.edge('U → D · eventos pelo Outbox<br>PaymentConfirmed', portal, management, 'exitX=1;exitY=0.8;');
  page.edge('U → D · Cliente-Fornecedor<br>NotificationCreated', management, notifications);
  page.edge('U → D · Conformista<br>perfis e limites de desconto', identity, quotes);
  page.edge('Shared Kernel', shared, orders, 'dashed=1;endArrow=none;');
  page.edge('U → D · ACL<br>IAddressLookup / ViaCepAddressLookup', viaCep, customers);
  page.edge('U → D · ACL<br>IEmailSender / SmtpEmailSender', smtp, notifications);
  page.edge('U → D · ACL (simulada)<br>IPixCodes / IPixWebhookVerifier', pix, portal, 'dashed=1;');
  page.edge('Caminhos Separados<br>link wa.me (envio manual)', quotes, whatsApp, 'dashed=1;');
  return page;
}

function domainModel() {
  const page = new Page('model', '3. Modelo de Domínio', 2600, 1700);
  page.title('Modelo de Domínio · agregados, entidades, objetos de valor e eventos', 'Só nomes que existem em src/Oficina.Domain. Amarelo: raiz do agregado. Verde: objetos de valor. Lilás: invariantes. Laranja: eventos de domínio levantados.');
  const aggregates = [
    ['ServiceOrder', 'Entidades: ServiceItem, PartItem, Quote, StatusHistory, TimeEntry, ChecklistItem', 'ServiceOrderNumber, Money, Quantity, ApprovalToken, QuoteScope', 'Uma O.S. aberta por veículo · itens só Em diagnóstico · transições pela máquina de status · até 12 fotos', 'ServiceOrderOpened, ServiceOrderStatusChanged, QuoteSent, QuoteApproved, QuoteRejected, ServiceOrderCompleted'],
    ['Estimate', 'Registros: EstimateLine, ManualDiscount, DiscountApproval, EstimateDelivery', 'PricingTerms, EstimateTotals (EstimatePricing)', 'Só responde pendente · desconto acima do limite exige aprovação · 3 erros bloqueiam o portal · aprovação parcial sem recusa', '— (a O.S. levanta os eventos da decisão)'],
    ['Part', 'Entidade: StockMovement', 'PartCode, Money, Quantity', 'Saldo nunca negativo · baixa só com saldo · peça em pacote ativo não desativa', 'StockDeducted, StockBelowMinimum'],
    ['ServiceOrderPhoto', '—', '—', 'JPG/PNG/WebP · tamanho máximo · carregada sob demanda', '—'],
    ['Customer', '—', 'Document, Phone, Email, Address', 'CPF/CNPJ válido e único', '—'],
    ['Vehicle', '—', 'LicensePlate, VehicleYear', 'Placa única · quilometragem nunca diminui', '—'],
    ['Service', 'Entidade: ServiceUsualPart', 'Money', 'Preço, comissão e garantia válidos', '—'],
    ['Package', 'Entidade: PackageItem', 'Money, Quantity', 'Itens ativos e preço fechado', '—'],
    ['User', '—', 'Email', 'E-mail único · bloqueio por tentativas · segredo 2FA cifrado', '—'],
    ['PortalCharge', '—', '—', 'Paga uma única vez · expira em 30 minutos', 'PaymentConfirmed'],
    ['StockCount · PurchaseOrder · Supplier', 'Entidades: CountLine, PurchaseOrderLine', 'Quantity, Money', 'Contagem aprovada vira ajuste · pedido enviado uma vez', '—'],
    ['Notification', '—', '—', 'Aviso único por chave de deduplicação', 'NotificationCreated'],
  ];
  aggregates.forEach(([root, entities, values, rules, events], index) => {
    const x = 40 + (index % 4) * 640;
    const y = 120 + Math.floor(index / 4) * 520;
    page.group(root, x, y, 620, 500);
    page.sticky(`${root}<br><span style="font-weight:normal;font-size:11px">${entities}</span>`, COLORS.aggregate, x + 20, y + 40, 580, 100);
    page.sticky(`Objetos de valor<br><span style="font-weight:normal;font-size:11px">${values}</span>`, COLORS.value, x + 20, y + 155, 580, 90);
    page.sticky(`Invariantes<br><span style="font-weight:normal;font-size:11px">${rules}</span>`, COLORS.rule, x + 20, y + 260, 580, 110);
    page.sticky(`Eventos<br><span style="font-weight:normal;font-size:11px">${events}</span>`, COLORS.event, x + 20, y + 385, 580, 95);
  });
  return page;
}

function story(id, name, subtitle, actors, objects, steps, notes) {
  const page = new Page(id, name, 2200, 1000);
  page.title(name, subtitle);
  const ids = {};
  page.group('Escopo da cena', 30, 110, 1820, 860);
  actors.forEach(([key, label, x, y]) => (ids[key] = page.actor(label, x, y)));
  objects.forEach(([key, label, x, y, kind]) => (ids[key] = kind === 'system' ? page.system(label, x, y) : page.workObject(label, x, y)));
  steps.forEach(([source, target, label], index) => page.edge(`<b>${index + 1}</b> ${label}`, ids[source], ids[target]));
  notes.forEach(([text, hot], index) => page.annotation(text, 1880, 120 + index * 90, 300, hot));
  page.legend([
    ['Ator', 'shape=umlActor;html=1;fillColor=#FEF445;'],
    ['Objeto de trabalho', 'shape=note;html=1;size=8;fillColor=#FFFFFF;'],
    ['Sistema (to-be)', 'rounded=1;html=1;fillColor=#1168BD;'],
    ['Anotação', `shape=note;html=1;size=8;fillColor=${COLORS.note};`],
    ['Dor do processo atual (hot spot)', `shape=note;html=1;size=8;fillColor=${COLORS.hot};`],
  ], 1880, 700);
  return page;
}

const asIs = [
  story('as1', 'As-is 1 · Cadastro de cliente e veículo', 'Processo manual de hoje: o atendente anota os dados do cliente e do carro.', [
    ['cl', 'Cliente', 100, 300], ['at', 'Atendente', 700, 300],
  ], [['dados', 'Dados pessoais e do veículo', 380, 160], ['papel', 'Ficha em papel / planilha', 1000, 320], ['def', 'Defeito relatado', 380, 520]], [
    ['cl', 'dados', 'informa'], ['dados', 'at', 'para o'], ['cl', 'def', 'relata'], ['def', 'at', 'para o'], ['at', 'papel', 'anota na'],
  ], [['Cadastro em papel ou planilha: dados se perdem e não há busca por CPF/CNPJ', true], ['Defeito relatado fica em texto solto, sem vínculo com uma O.S.', true]]),
  story('as2', 'As-is 2 · Cadastro de insumos ou materiais', 'Peças e insumos são controlados de forma informal.', [
    ['of', 'Oficina', 100, 300], ['fo', 'Fornecedor', 1100, 300],
  ], [['nota', 'Nota de compra', 700, 160], ['plan', 'Planilha de estoque', 400, 420]], [
    ['fo', 'nota', 'entrega peças com'], ['nota', 'of', 'para a'], ['of', 'plan', 'lança (quando lembra) na'],
  ], [['Falha no controle de peças: saldo da planilha não bate com a prateleira', true], ['Não há estoque mínimo nem aviso de reposição', true]]),
  story('as3', 'As-is 3 · Cadastro de serviços prestados', 'Lista de serviços e preços sem padrão.', [['of', 'Oficina', 100, 300]], [['lista', 'Lista de serviços e preços', 500, 300], ['parede', 'Anotação na parede', 900, 300]], [
    ['of', 'lista', 'mantém de cabeça a'], ['of', 'parede', 'às vezes escreve na'],
  ], [['Preço "às vezes escrito até na parede": sem histórico nem padrão', true]]),
  story('as4', 'As-is 4 · Orçamento da Ordem de Serviço', 'Mecânico avalia o veículo e a recepção monta o orçamento informal.', [
    ['me', 'Mecânico', 100, 300], ['at', 'Atendente', 800, 300],
  ], [['carro', 'Veículo com defeito', 400, 160], ['itens', 'Serviços e peças necessários', 400, 460], ['orc', 'Orçamento informal', 1150, 300]], [
    ['me', 'carro', 'examina o'], ['me', 'itens', 'anota os'], ['itens', 'at', 'repassa ao'], ['at', 'orc', 'monta o'],
  ], [['Diagnóstico não fica registrado: a O.S. existe só na memória de quem executa', true], ['Orçamento sem padrão nem versão', true]]),
  story('as5', 'As-is 5 · Análise do orçamento pelo cliente', 'O orçamento vai por WhatsApp e a resposta volta pelo mesmo canal.', [
    ['at', 'Atendente', 100, 300], ['cl', 'Cliente', 1000, 300],
  ], [['msg', 'Orçamento por WhatsApp', 500, 300]], [['at', 'msg', 'envia'], ['msg', 'cl', 'para o'], ['cl', 'at', 'responde (aprova ou recusa) ao']], [
    ['Orçamento informal por WhatsApp: aprovação sem registro e sem aceite parcial', true],
  ]),
  story('as6', 'As-is 6 · Execução da Ordem de Serviço', 'Mecânico executa e usa peças do estoque.', [['me', 'Mecânico', 100, 300]], [
    ['peca', 'Peças e insumos', 500, 160], ['carro', 'Veículo', 500, 460], ['plan', 'Planilha de estoque', 1000, 160],
  ], [['me', 'peca', 'pega na prateleira'], ['me', 'carro', 'executa o serviço no'], ['me', 'plan', 'nem sempre dá baixa na']], [
    ['Status só na memória de quem executa: ninguém sabe em que etapa o carro está', true], ['Baixa de estoque sem mecânica definida', true],
  ]),
  story('as7', 'As-is 7 · Entrega do veículo', 'Pagamento e entrega acontecem no balcão.', [['cl', 'Cliente', 100, 300], ['at', 'Atendente', 800, 300]], [
    ['pag', 'Pagamento', 450, 160], ['carro', 'Veículo pronto', 450, 460], ['rec', 'Recibo em papel', 1150, 300],
  ], [['cl', 'pag', 'faz o'], ['pag', 'at', 'para o'], ['at', 'carro', 'devolve o'], ['at', 'rec', 'às vezes emite']], [
    ['Perda de histórico: nada liga o pagamento ao serviço executado', true],
  ]),
];

const toBe = [
  story('tb1', 'To-be 1 · Abertura da O.S.', 'Com o sistema: identificação por CPF/CNPJ e cadastro do veículo.', [['cl', 'Cliente', 100, 300], ['at', 'Atendente', 600, 300]], [
    ['sys', 'mecanicaOS', 1000, 300, 'system'], ['cad', 'Cliente (CPF/CNPJ) e Veículo (placa)', 1000, 120], ['os', 'O.S. Recebida', 1000, 520],
  ], [['cl', 'at', 'informa CPF/CNPJ ao'], ['at', 'cad', 'busca ou cadastra'], ['at', 'sys', 'abre a O.S. no'], ['sys', 'os', 'registra (ServiceOrderOpened)']], [
    ['OpenServiceOrderCommand · regra: um veículo, uma O.S. aberta', false],
  ]),
  story('tb2', 'To-be 2 · Diagnóstico e envio para orçamento', 'Mecânico registra checklist, fotos e diagnóstico.', [['me', 'Mecânico', 100, 300]], [
    ['sys', 'mecanicaOS', 600, 300, 'system'], ['diag', 'Checklist, fotos e diagnóstico', 1000, 160], ['itens', 'Itens indicados', 1000, 460],
  ], [['me', 'sys', 'inicia o atendimento no'], ['me', 'diag', 'registra'], ['me', 'itens', 'envia para orçamento']], [
    ['StartServiceCommand, AddPhotoCommand, SaveDiagnosisCommand, SendToQuoteCommand', false],
  ]),
  story('tb3', 'To-be 3 · Orçamento: montagem, envio e decisão pelo portal', 'Aprovação total ou parcial ou recusa pelo Portal do cliente.', [['at', 'Atendente', 100, 300], ['cl', 'Cliente', 1300, 300]], [
    ['orc', 'Orçamento (Estimate)', 500, 160], ['link', 'Link do portal', 900, 160], ['dec', 'Aprovação total/parcial ou recusa', 900, 460],
  ], [['at', 'orc', 'monta e envia o'], ['orc', 'link', 'gera o'], ['link', 'cl', 'abre o portal o'], ['cl', 'dec', 'registra a']], [
    ['Uma única porta: QuoteDecider (portal, balcão e quadro)', false], ['Aprovação parcial tira itens não aceitos, sem recusa', false],
  ]),
  story('tb4', 'To-be 4 · Execução, baixa de estoque e finalização', 'Na aprovação a peça é baixada na mesma transação.', [['me', 'Mecânico', 100, 300]], [
    ['sys', 'mecanicaOS', 600, 300, 'system'], ['est', 'Estoque (Part)', 1000, 160], ['os', 'O.S. Finalizada', 1000, 460],
  ], [['sys', 'est', 'dá baixa na aprovação (StockDeducted)'], ['me', 'sys', 'cronometra e finaliza no'], ['sys', 'os', 'muda para']], [
    ['StockBelowMinimum na hora da baixa dispara reposição', false],
  ]),
  story('tb5', 'To-be 5 · Pagamento Pix simulado, recibo e entrega', 'Cliente paga pelo portal; recibo sai pelo Outbox.', [['cl', 'Cliente', 100, 300], ['at', 'Atendente', 1300, 300]], [
    ['pix', 'Cobrança Pix', 500, 160], ['rec', 'Recibo (documento emitido)', 900, 160], ['carro', 'Veículo entregue', 900, 460],
  ], [['cl', 'pix', 'paga a'], ['pix', 'rec', 'gera (PaymentConfirmed)'], ['at', 'carro', 'entrega o']], [
    ['Confirmação do Pix simulada; PSP real: To be implemented', true],
  ]),
  story('tb6', 'To-be 6 · Reposição de estoque', 'Entrada por NF-e e pedido de compra ao fornecedor.', [['ge', 'Gerente', 100, 300], ['fo', 'Fornecedor', 1300, 300]], [
    ['sug', 'Sugestão de compra', 500, 160], ['ped', 'Pedido de compra', 900, 160], ['nfe', 'NF-e (XML)', 900, 460],
  ], [['ge', 'sug', 'consulta a'], ['ge', 'ped', 'envia o'], ['ped', 'fo', 'por e-mail ao'], ['fo', 'nfe', 'entrega com a'], ['nfe', 'ge', 'é lida e confirmada pelo']], [
    ['CreatePurchaseOrderCommand, SendPurchaseOrderCommand, ConfirmInvoiceCommand', false],
  ]),
];

function comparison() {
  const page = new Page('cmp', 'As-is × To-be', 2200, 1100);
  page.title('As-is × To-be', 'Cada dor do processo atual, como o sistema resolve e onde está no código.');
  const rows = [
    ['Cadastro em papel/planilha, sem busca', 'Cliente por CPF/CNPJ e veículo por placa, buscas por POST', 'CreateCustomerCommand · SearchCustomerByDocumentQuery'],
    ['Status só na memória de quem executa', 'Seis status do enunciado, quadro em tempo real e histórico', 'ServiceOrderStatus · StatusHistory · ServiceOrderStatusChanged'],
    ['Orçamento informal por WhatsApp', 'Orçamento versionado, link do portal, aprovação total/parcial ou recusa', 'Estimate · QuoteDecider · ApproveQuoteFromPortalCommand'],
    ['Falha no controle de peças', 'Baixa atômica na aprovação, estoque mínimo e aviso imediato', 'QuoteApprovalPolicy · StockDeducted · StockBelowMinimum'],
    ['Preço sem padrão', 'Catálogo com histórico de preços e pacotes', 'Service · Package · PriceChange'],
    ['Perda de histórico', 'Documentos emitidos com código de verificação e auditoria', 'IssuedDocument · VerifyDocumentQuery · AuditEntry'],
    ['Pagamento solto do serviço', 'Cobrança Pix ligada ao orçamento e recibo automático (simulado)', 'PortalCharge · PaymentConfirmed · IssueReceiptOnPaymentConfirmed'],
  ];
  const widths = [600, 700, 700];
  ['Dor do processo atual', 'Como o sistema resolve', 'Onde está no código'].forEach((header, column) => {
    page.card(`<b style="font-size:15px">${header}</b>`, '#E4E4DE', 40 + widths.slice(0, column).reduce((a, b) => a + b, 0), 120, widths[column], 60);
  });
  rows.forEach((row, index) => {
    row.forEach((cell, column) => {
      const fill = column === 0 ? COLORS.hot : column === 1 ? COLORS.supporting : COLORS.object;
      page.card(column === 2 ? `<span style="font-family:monospace;font-size:12px">${cell}</span>` : cell, fill, 40 + widths.slice(0, column).reduce((a, b) => a + b, 0), 190 + index * 120, widths[column], 110);
    });
  });
  return page;
}

writeFileSync(join(out, 'DDD_mecanicaOS.drawio'), file([subdomains(), contextMap(), domainModel()]));
writeFileSync(join(out, 'DomainStorytelling_mecanicaOS.drawio'), file([...asIs, ...toBe, comparison()]));
process.stdout.write('Diagramas DDD e Domain Storytelling gerados em docs/arquitetura.\n');

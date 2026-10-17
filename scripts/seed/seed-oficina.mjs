import { createHmac } from 'node:crypto';

const API = (process.env.API_URL ?? 'http://localhost:8080').replace(/\/$/, '');
const ADMIN_EMAIL = process.env.ADMIN_EMAIL ?? 'admin@oficina.local';
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? 'Admin@123';
const PIX_SECRET = process.env.PIX_WEBHOOK_SECRET ?? '';
const PASSWORD = 'Oficina@2026';
const PHOTO =
  'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==';

const log = (message) => console.log(`[seed] ${message}`);

async function call(method, path, { token, body, headers = {}, expect = [200, 201, 202, 204] } = {}) {
  const response = await fetch(`${API}${path}`, {
    method,
    headers: {
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...headers,
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await response.text();
  const json = text ? JSON.parse(text) : null;
  if (!expect.includes(response.status)) {
    throw new Error(`${method} ${path} -> ${response.status} ${json?.code ?? ''} ${json?.detail ?? text}`);
  }

  return json;
}

async function waitForApi() {
  for (let attempt = 0; attempt < 90; attempt++) {
    try {
      const response = await fetch(`${API}/health`);
      if (response.ok) {
        return;
      }
    } catch {
      await new Promise((resolve) => setTimeout(resolve, 2000));
      continue;
    }

    await new Promise((resolve) => setTimeout(resolve, 2000));
  }

  throw new Error('API indisponível');
}

async function login(email, password) {
  const result = await call('POST', '/api/auth/login', { body: { email, password } });
  return result.accessToken;
}

async function reauth(token, password) {
  return (await call('POST', '/api/auth/reauthenticate', { token, body: { password } })).reauthToken;
}

async function sensitive(token, password, method, path, body) {
  return call(method, path, { token, body, headers: { 'X-Reauthentication': await reauth(token, password) } });
}

function checkDigit(digits, weights) {
  const sum = digits.reduce((total, digit, index) => total + digit * weights[index], 0);
  const rest = sum % 11;
  return rest < 2 ? 0 : 11 - rest;
}

function cpf(seed) {
  const base = String(100000000 + seed * 7919).slice(0, 9).split('').map(Number);
  const first = checkDigit(base, [10, 9, 8, 7, 6, 5, 4, 3, 2]);
  const second = checkDigit([...base, first], [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]);
  return [...base, first, second].join('');
}

function cnpj(seed) {
  const base = `${String(10000000 + seed * 104729).slice(0, 8)}0001`.split('').map(Number);
  const first = checkDigit(base, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
  const second = checkDigit([...base, first], [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
  return [...base, first, second].join('');
}

const schedule = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday']
  .map((day) => ({ day, start: '08:00', end: '18:00' }))
  .concat([{ day: 'Saturday', start: '08:00', end: '12:00' }]);

const weekHours = [
  { weekday: 'Monday', closed: false, opens: '08:00', closes: '18:00' },
  { weekday: 'Tuesday', closed: false, opens: '08:00', closes: '18:00' },
  { weekday: 'Wednesday', closed: false, opens: '08:00', closes: '18:00' },
  { weekday: 'Thursday', closed: false, opens: '08:00', closes: '18:00' },
  { weekday: 'Friday', closed: false, opens: '08:00', closes: '18:00' },
  { weekday: 'Saturday', closed: false, opens: '08:00', closes: '12:00' },
  { weekday: 'Sunday', closed: true, opens: '00:00', closes: '00:00' },
];

const TEAM = [
  { name: 'Gabriela Gestora', email: 'gerente@oficina.local', role: 'Manager', specialties: ['Gestão', 'Garantia'], commission: 0, discount: 20 },
  { name: 'Ana Atendente', email: 'ana.atendente@oficina.local', role: 'Attendant', specialties: ['Atendimento', 'Caixa'], commission: 2, discount: 10 },
  { name: 'Bruno Atendente', email: 'bruno.atendente@oficina.local', role: 'Attendant', specialties: ['Atendimento'], commission: 2, discount: 10 },
  { name: 'Marcos Mecânico', email: 'marcos.mecanico@oficina.local', role: 'Mechanic', specialties: ['Motor', 'Injeção eletrônica'], commission: 10, discount: 0 },
  { name: 'Paula Mecânica', email: 'paula.mecanica@oficina.local', role: 'Mechanic', specialties: ['Freios', 'Suspensão'], commission: 10, discount: 0 },
  { name: 'Rafael Mecânico', email: 'rafael.mecanico@oficina.local', role: 'Mechanic', specialties: ['Elétrica', 'Ar-condicionado'], commission: 12, discount: 0 },
];

const SUPPLIERS = [
  { name: 'Distribuidora Freio Sul', email: 'vendas@freiosul.example.com', phone: '(51) 3333-4444', leadTimeDays: 3 },
  { name: 'Auto Peças Centro', email: 'pedidos@autopecascentro.example.com', phone: '(11) 3222-1100', leadTimeDays: 2 },
  { name: 'Lubrificantes Brasil', email: 'comercial@lubribrasil.example.com', phone: '(21) 2555-0101', leadTimeDays: 5 },
];

const PARTS = [
  { code: 'OLE-5W30', name: 'Óleo 5W30 sintético (litro)', type: 'Supply', unit: 'L', price: 58, cost: 32, min: 20, stock: 60, supplier: 'Lubrificantes Brasil' },
  { code: 'FLT-OLEO', name: 'Filtro de óleo', type: 'Part', unit: 'un', price: 45, cost: 22, min: 10, stock: 25, supplier: 'Auto Peças Centro' },
  { code: 'FLT-AR', name: 'Filtro de ar do motor', type: 'Part', unit: 'un', price: 62, cost: 30, min: 8, stock: 14, supplier: 'Auto Peças Centro' },
  { code: 'FLT-CAB', name: 'Filtro de cabine', type: 'Part', unit: 'un', price: 55, cost: 25, min: 8, stock: 12, supplier: 'Auto Peças Centro' },
  { code: 'PST-DIA', name: 'Pastilha de freio dianteira', type: 'Part', unit: 'jogo', price: 180, cost: 90, min: 6, stock: 3, supplier: 'Distribuidora Freio Sul' },
  { code: 'DSC-DIA', name: 'Disco de freio dianteiro', type: 'Part', unit: 'un', price: 240, cost: 130, min: 4, stock: 10, supplier: 'Distribuidora Freio Sul' },
  { code: 'FLD-FREIO', name: 'Fluido de freio DOT 4', type: 'Supply', unit: 'un', price: 38, cost: 18, min: 10, stock: 30, supplier: 'Distribuidora Freio Sul' },
  { code: 'VEL-IGN', name: 'Vela de ignição', type: 'Part', unit: 'un', price: 42, cost: 20, min: 16, stock: 40, supplier: 'Auto Peças Centro' },
  { code: 'COR-DENT', name: 'Kit correia dentada', type: 'Part', unit: 'kit', price: 420, cost: 250, min: 3, stock: 5, supplier: 'Auto Peças Centro' },
  { code: 'AMT-DIA', name: 'Amortecedor dianteiro', type: 'Part', unit: 'un', price: 380, cost: 210, min: 4, stock: 8, supplier: 'Distribuidora Freio Sul' },
  { code: 'BAT-60', name: 'Bateria 60Ah', type: 'Part', unit: 'un', price: 560, cost: 360, min: 3, stock: 2, supplier: 'Auto Peças Centro' },
  { code: 'LIQ-ARR', name: 'Aditivo de arrefecimento', type: 'Supply', unit: 'L', price: 35, cost: 15, min: 12, stock: 24, supplier: 'Lubrificantes Brasil' },
  { code: 'LMP-H4', name: 'Lâmpada farol H4', type: 'Part', unit: 'un', price: 39, cost: 14, min: 10, stock: 20, supplier: 'Auto Peças Centro' },
  { code: 'GAS-AC', name: 'Gás do ar-condicionado R134a', type: 'Supply', unit: 'kg', price: 150, cost: 80, min: 2, stock: 6, supplier: 'Lubrificantes Brasil' },
];

const SERVICES = [
  { name: 'Troca de óleo e filtro', price: 90, minutes: 40, commission: 10, warranty: 90, parts: [['OLE-5W30', 4], ['FLT-OLEO', 1]] },
  { name: 'Alinhamento 3D', price: 120, minutes: 40, commission: 10, warranty: 30, parts: [] },
  { name: 'Balanceamento (4 rodas)', price: 80, minutes: 30, commission: 10, warranty: 30, parts: [] },
  { name: 'Troca de pastilhas dianteiras', price: 150, minutes: 60, commission: 12, warranty: 180, parts: [['PST-DIA', 1]] },
  { name: 'Troca de discos e pastilhas', price: 260, minutes: 90, commission: 12, warranty: 180, parts: [['DSC-DIA', 2], ['PST-DIA', 1]] },
  { name: 'Diagnóstico eletrônico', price: 150, minutes: 45, commission: 8, warranty: 0, parts: [] },
  { name: 'Troca de correia dentada', price: 480, minutes: 180, commission: 15, warranty: 365, parts: [['COR-DENT', 1]] },
  { name: 'Revisão de suspensão', price: 220, minutes: 120, commission: 12, warranty: 90, parts: [] },
  { name: 'Higienização do ar-condicionado', price: 180, minutes: 60, commission: 10, warranty: 90, parts: [['FLT-CAB', 1]] },
  { name: 'Troca de velas', price: 100, minutes: 40, commission: 10, warranty: 90, parts: [['VEL-IGN', 4]] },
  { name: 'Troca de fluido de freio', price: 90, minutes: 40, commission: 10, warranty: 180, parts: [['FLD-FREIO', 1]] },
];

const CUSTOMERS = [
  { name: 'Joana Prado', phone: '(11) 98765-4321', email: 'joana.prado@exemplo.com', whatsApp: true, mail: true, vehicles: [['FJK4E21', 'Volkswagen', 'Polo 1.0 TSI', 2021, 41230]] },
  { name: 'Carlos Mendes', phone: '(11) 97654-3210', email: 'carlos.mendes@exemplo.com', whatsApp: true, mail: true, vehicles: [['EXP9C88', 'Honda', 'Civic EXL', 2018, 88400]] },
  { name: 'Mariana Lopes', phone: '(21) 99812-3344', email: 'mariana.lopes@exemplo.com', whatsApp: true, mail: false, vehicles: [['RJM2A10', 'Jeep', 'Renegade', 2020, 63000]] },
  { name: 'Pedro Almeida', phone: '(31) 98877-1122', email: null, whatsApp: true, mail: false, vehicles: [['BHZ5D33', 'Fiat', 'Argo Drive', 2022, 22100]] },
  { name: 'Fernanda Costa', phone: '(41) 99654-7788', email: 'fernanda.costa@exemplo.com', whatsApp: false, mail: true, vehicles: [['CWB1F45', 'Toyota', 'Corolla XEi', 2017, 112300]] },
  { name: 'Ricardo Souza', phone: '(51) 98123-4567', email: 'ricardo.souza@exemplo.com', whatsApp: true, mail: true, vehicles: [['POA7G21', 'Chevrolet', 'Onix LT', 2023, 15400]] },
  { name: 'Luciana Ferreira', phone: '(11) 96543-2109', email: 'luciana.ferreira@exemplo.com', whatsApp: true, mail: true, vehicles: [['SPX3H90', 'Hyundai', 'HB20 Comfort', 2019, 70500]] },
  { name: 'Gustavo Ribeiro', phone: '(19) 99701-2233', email: null, whatsApp: false, mail: false, vehicles: [['CPN8J12', 'Renault', 'Sandero', 2016, 134000]] },
  { name: 'Transportes Rota Sul Ltda', phone: '(51) 3344-5566', email: 'frota@rotasul.example.com', whatsApp: true, mail: true, company: true, vehicles: [['RSL1A23', 'Fiat', 'Fiorino', 2022, 88400], ['RSL2B34', 'Fiat', 'Fiorino', 2022, 92150], ['ABC1D34', 'Renault', 'Master', 2019, 154900]] },
  { name: 'Padaria Pão Quente ME', phone: '(11) 3456-7890', email: 'contato@paoquente.example.com', whatsApp: true, mail: true, company: true, vehicles: [['PQT4K56', 'Volkswagen', 'Saveiro', 2020, 76000]] },
];

async function main() {
  await waitForApi();
  const admin = await login(ADMIN_EMAIL, ADMIN_PASSWORD);
  const existing = await call('GET', '/api/admin/customers?page=1&pageSize=1', { token: admin });
  if (existing.totalItems > 0) {
    log('O banco já tem clientes; a carga de demonstração não roda de novo.');
    return;
  }

  log('Configurando a oficina...');
  await sensitive(admin, ADMIN_PASSWORD, 'PUT', '/api/admin/settings/workshop', {
    name: 'Auto Center Paulista',
    document: '11222333000181',
    address: { zipCode: '01310100', street: 'Avenida Paulista', number: '1000', district: 'Bela Vista', city: 'São Paulo', state: 'SP' },
    hours: weekHours,
  });
  await sensitive(admin, ADMIN_PASSWORD, 'PUT', '/api/admin/settings/payments', {
    methods: ['Pix', 'Credit', 'Debit', 'Cash'],
    maxInstallments: 6,
    interestFreeInstallments: 3,
    monthlyInterestPercent: 1.99,
    fees: [
      { method: 'Pix', feePercent: 0, settlementDays: 0 },
      { method: 'Credit', feePercent: 3.49, settlementDays: 30 },
      { method: 'Debit', feePercent: 1.89, settlementDays: 1 },
      { method: 'Cash', feePercent: 0, settlementDays: 0 },
    ],
    pixKey: 'financeiro@autocenterpaulista.com.br',
  });
  await sensitive(admin, ADMIN_PASSWORD, 'PUT', '/api/admin/settings/discounts', {
    rules: [
      { id: '00000000-0000-0000-0000-000000000000', name: 'Cliente fiel', type: 'Percent', value: 5, condition: 'LoyalCustomer', isActive: true },
      { id: '00000000-0000-0000-0000-000000000000', name: 'Frota', type: 'Percent', value: 8, condition: 'Fleet', isActive: true },
      { id: '00000000-0000-0000-0000-000000000000', name: 'Primeira visita', type: 'Amount', value: 30, condition: 'FirstService', isActive: false },
    ],
    limits: [
      { role: 'Admin', percent: 30 },
      { role: 'Manager', percent: 20 },
      { role: 'Attendant', percent: 10 },
      { role: 'Mechanic', percent: 0 },
    ],
  });

  log('Criando a equipe...');
  const users = {};
  for (const member of TEAM) {
    const user = await call('POST', '/api/admin/users', { token: admin, body: { name: member.name, email: member.email, password: PASSWORD, role: member.role } });
    users[member.email] = user.id;
    await call('PUT', `/api/admin/team/${user.id}/profile`, {
      token: admin,
      body: { specialties: member.specialties, commissionPercent: member.commission, discountLimitPercent: member.discount, schedule },
    });
  }

  log('Cadastrando fornecedores, peças e estoque...');
  const suppliers = {};
  for (const supplier of SUPPLIERS) {
    suppliers[supplier.name] = (await call('POST', '/api/admin/inventory/suppliers', { token: admin, body: { ...supplier, document: null } })).id;
  }

  const parts = {};
  for (const part of PARTS) {
    const created = await call('POST', '/api/admin/parts', {
      token: admin,
      body: { code: part.code, name: part.name, type: part.type, unitOfMeasure: part.unit, unitPrice: part.price, cost: part.cost, minimumStock: part.min, supplier: part.supplier, barcode: null },
    });
    parts[part.code] = created.id;
    await call('POST', '/api/admin/inventory/entries', {
      token: admin,
      body: { partId: created.id, quantity: part.stock, unitCost: part.cost, reason: 'Estoque inicial', supplierId: suppliers[part.supplier], invoiceNumber: `NF-${1000 + PARTS.indexOf(part)}` },
    });
  }

  log('Cadastrando serviços e pacotes...');
  const services = {};
  for (const service of SERVICES) {
    const created = await call('POST', '/api/admin/services', {
      token: admin,
      body: {
        name: service.name,
        description: null,
        basePrice: service.price,
        estimatedMinutes: service.minutes,
        commissionPercent: service.commission,
        warrantyDays: service.warranty,
        usualParts: service.parts.map(([code, quantity]) => ({ partId: parts[code], quantity })),
      },
    });
    services[service.name] = created.id;
  }

  const packages = [
    { name: 'Revisão 10 mil km', price: 320, items: [['Service', 'Troca de óleo e filtro', 1], ['Part', 'OLE-5W30', 4], ['Part', 'FLT-OLEO', 1], ['Part', 'FLT-AR', 1]], suggestion: { minMileage: 9000, maxMileage: 12000 } },
    { name: 'Freio completo dianteiro', price: 820, items: [['Service', 'Troca de discos e pastilhas', 1], ['Part', 'DSC-DIA', 2], ['Part', 'PST-DIA', 1], ['Part', 'FLD-FREIO', 1]], suggestion: null },
    { name: 'Geometria completa', price: 180, items: [['Service', 'Alinhamento 3D', 1], ['Service', 'Balanceamento (4 rodas)', 1]], suggestion: null },
  ];
  for (const pack of packages) {
    await call('POST', '/api/admin/packages', {
      token: admin,
      body: {
        name: pack.name,
        description: null,
        price: pack.price,
        suggestion: pack.suggestion,
        items: pack.items.map(([kind, key, quantity]) => ({ kind, itemId: kind === 'Service' ? services[key] : parts[key], quantity })),
      },
    });
  }

  log('Cadastrando clientes e veículos...');
  const customers = [];
  for (const [index, customer] of CUSTOMERS.entries()) {
    const created = await call('POST', '/api/admin/customers', {
      token: admin,
      body: {
        name: customer.name,
        document: customer.company ? cnpj(index + 1) : cpf(index + 1),
        email: customer.email,
        phone: customer.phone,
        address: { zipCode: '01310100', street: 'Avenida Paulista', number: String(100 + index * 10), complement: null, district: 'Bela Vista', city: 'São Paulo', state: 'SP' },
        messagingConsent: { whatsApp: customer.whatsApp, email: customer.mail && customer.email !== null },
      },
    });
    const vehicles = [];
    for (const [plate, brand, model, year, mileage] of customer.vehicles) {
      vehicles.push((await call('POST', '/api/admin/vehicles', { token: admin, body: { customerId: created.id, licensePlate: plate, brand, model, year, mileage } })).id);
    }

    customers.push({ id: created.id, name: customer.name, phone: customer.phone, vehicles });
  }

  const mechanics = {
    marcos: await login('marcos.mecanico@oficina.local', PASSWORD),
    paula: await login('paula.mecanica@oficina.local', PASSWORD),
  };

  const open = async (customer, vehicleIndex, problem, promiseDays) => {
    const promisedAt = promiseDays === null ? null : new Date(Date.now() + promiseDays * 86_400_000).toISOString();
    return call('POST', '/api/admin/service-orders', {
      token: admin,
      body: { customerId: customer.id, vehicleId: customer.vehicles[vehicleIndex], problemDescription: problem, promisedAt, mileage: null },
    });
  };
  const detail = (id) => call('GET', `/api/admin/service-orders/${id}`, { token: admin });
  const move = async (id, to, reason = null) => {
    const current = await detail(id);
    return call('POST', `/api/admin/service-orders/${id}/transitions`, { token: admin, body: { to, version: current.version, reason } });
  };
  const diagnose = async (mechanic, id, diagnosis, items) => {
    const work = await call('POST', `/api/admin/service-orders/${id}/start-service`, { token: mechanic });
    await call('PUT', `/api/admin/service-orders/${id}/checklist`, {
      token: mechanic,
      body: { items: work.checklist.slice(0, 4).map((item, index) => ({ id: item.id, state: index === 1 ? 'Attention' : 'Ok' })) },
    });
    await call('POST', `/api/admin/service-orders/${id}/photos`, { token: mechanic, body: { clientId: `foto-${id}`, fileName: 'avaria.png', contentType: 'image/png', dataUrl: PHOTO } });
    await call('PUT', `/api/admin/service-orders/${id}/diagnosis`, { token: mechanic, body: { description: diagnosis } });
    if (items) {
      await call('POST', `/api/admin/service-orders/${id}/send-to-quote`, { token: mechanic, body: { description: diagnosis, items } });
    }
  };
  const quote = async (id, channel) => {
    const created = await call('POST', '/api/admin/quotes', { token: admin, body: { serviceOrderId: id } });
    const draft = await call('POST', `/api/admin/quotes/${created.id}/message-draft`, { token: admin, body: { channel } });
    const sent = await call('POST', `/api/admin/quotes/${created.id}/send`, { token: admin, body: { channel, expiresInDays: 7, message: draft.message } });
    return { id: created.id, link: sent.delivery.link };
  };
  const execute = async (mechanic, id) => {
    await call('POST', `/api/admin/service-orders/${id}/time-entries/start`, { token: mechanic });
    await call('POST', `/api/admin/service-orders/${id}/time-entries/stop`, { token: mechanic });
  };
  const finish = async (mechanic, id) => call('POST', `/api/admin/service-orders/${id}/finish-service`, { token: mechanic });
  const service = (name) => ({ kind: 'Service', itemId: services[name] });
  const part = (code) => ({ kind: 'Part', itemId: parts[code] });

  log('Abrindo ordens de serviço em todas as etapas...');
  const [joana, carlos, mariana, pedro, fernanda, ricardo, luciana, gustavo, rotaSul, padaria] = customers;

  await open(pedro, 0, 'Barulho na suspensão dianteira ao passar em lombadas', 3);
  await open(gustavo, 0, 'Revisão dos 130 mil km e luz da injeção acesa', 2);

  const diagnosis1 = await open(mariana, 0, 'Ar-condicionado não gela', 2);
  await diagnose(mechanics.paula, diagnosis1.id, 'Filtro de cabine saturado e baixa carga de gás', null);

  const waiting1 = await open(joana, 0, 'Freio chiando e pedal baixo', 2);
  await diagnose(mechanics.paula, waiting1.id, 'Pastilhas no limite e fluido contaminado', [service('Troca de pastilhas dianteiras'), part('PST-DIA'), service('Troca de fluido de freio'), part('FLD-FREIO')]);
  const portalQuote = await quote(waiting1.id, 'Link');

  const waiting2 = await open(rotaSul, 0, 'Revisão preventiva da frota', 4);
  await diagnose(mechanics.marcos, waiting2.id, 'Troca de óleo vencida e filtro de ar sujo', [service('Troca de óleo e filtro'), part('OLE-5W30'), part('FLT-OLEO'), part('FLT-AR')]);
  await quote(waiting2.id, 'WhatsApp');

  const running1 = await open(carlos, 0, 'Embreagem patinando e correia com ruído', 3);
  await diagnose(mechanics.marcos, running1.id, 'Correia dentada ressecada, troca recomendada', [service('Troca de correia dentada'), part('COR-DENT')]);
  const runningQuote = await quote(running1.id, 'Email');
  await call('POST', `/api/admin/quotes/${runningQuote.id}/approve`, { token: admin });
  await execute(mechanics.marcos, running1.id);

  const completed1 = await open(fernanda, 0, 'Revisão completa', 1);
  await diagnose(mechanics.paula, completed1.id, 'Velas desgastadas', [service('Troca de velas'), part('VEL-IGN')]);
  const completedQuote = await quote(completed1.id, 'Email');
  await call('POST', `/api/admin/quotes/${completedQuote.id}/approve`, { token: admin });
  await execute(mechanics.paula, completed1.id);
  await finish(mechanics.paula, completed1.id);

  const delivered = [
    [ricardo, 0, 'Troca de óleo', [service('Troca de óleo e filtro'), part('OLE-5W30'), part('FLT-OLEO')], mechanics.marcos],
    [luciana, 0, 'Volante puxando para a direita', [service('Alinhamento 3D'), service('Balanceamento (4 rodas)')], mechanics.paula],
    [padaria, 0, 'Farol queimado e bateria fraca', [part('LMP-H4'), part('BAT-60'), service('Diagnóstico eletrônico')], mechanics.marcos],
  ];
  for (const [customer, vehicle, problem, items, mechanic] of delivered) {
    const order = await open(customer, vehicle, problem, 1);
    await diagnose(mechanic, order.id, problem, items);
    const sent = await quote(order.id, 'Link');
    await call('POST', `/api/admin/quotes/${sent.id}/approve`, { token: admin });
    await execute(mechanic, order.id);
    await finish(mechanic, order.id);
    await move(order.id, 'Delivered');
  }

  log('Pagando um orçamento pelo portal do cliente com Pix...');
  const paidOrder = await open(rotaSul, 1, 'Pastilhas gastas na Fiorino 2', 2);
  await diagnose(mechanics.paula, paidOrder.id, 'Pastilhas no fim', [service('Troca de pastilhas dianteiras'), part('PST-DIA')]);
  const paidQuote = await quote(paidOrder.id, 'Link');
  const token = decodeURIComponent(paidQuote.link.split('/o/')[1]);
  await call('POST', '/api/public/portal/access', { body: { token } });
  const session = (await call('POST', '/api/public/portal/confirm', { body: { token, lastDigits: rotaSul.phone.replace(/\D/g, '').slice(-3) } })).session;
  const view = await call('GET', '/api/public/portal/order', { headers: { 'X-Portal-Session': session } });
  await call('POST', '/api/public/portal/quote/approve', { headers: { 'X-Portal-Session': session }, body: { itemIds: view.quote.items.map((item) => item.id) } });
  const pix = await call('POST', '/api/public/portal/payments/pix', { headers: { 'X-Portal-Session': session } });
  const txId = /0525([0-9A-F]{25})6304/.exec(pix.copyPaste)?.[1];
  if (PIX_SECRET && txId) {
    const signature = createHmac('sha256', PIX_SECRET).update(txId).digest('hex');
    await call('POST', '/api/public/payments/pix/webhook', { headers: { 'X-Pix-Signature': signature }, body: { txId } });
  }

  await execute(mechanics.paula, paidOrder.id);
  await finish(mechanics.paula, paidOrder.id);
  await move(paidOrder.id, 'Delivered');

  log('Criando automações, relatórios salvos, contagem e pedido de compra...');
  const rules = [
    { name: 'Lembrete de orçamento sem resposta', trigger: 'QuoteSent', conditions: [{ field: 'quoteStatus', operator: 'equals', value: 'Pending' }], wait: { amount: 2, unit: 'days' }, actions: [{ kind: 'SendWhatsApp', message: 'Olá {cliente}, o orçamento da OS {os} ainda aguarda sua resposta: {link}' }] },
    { name: 'Avisar mudança de etapa', trigger: 'ServiceOrderStatusChanged', conditions: [{ field: 'customerConsent', operator: 'equals', value: 'true' }], wait: null, actions: [{ kind: 'SendEmail', message: 'Olá {cliente}, a OS {os} do seu {veiculo} mudou de etapa.' }] },
    { name: 'Repor estoque automaticamente', trigger: 'StockBelowMinimum', conditions: [], wait: null, actions: [{ kind: 'CreatePurchaseOrder', message: null }, { kind: 'NotifyTeam', message: 'Estoque baixo: pedido de compra criado.' }] },
    { name: 'Pós-venda', trigger: 'ServiceOrderDelivered', conditions: [], wait: { amount: 3, unit: 'days' }, actions: [{ kind: 'SendWhatsApp', message: 'Olá {cliente}, como está o seu {veiculo} depois do serviço?' }] },
  ];
  for (const rule of rules) {
    await call('POST', '/api/admin/automations/rules', { token: admin, body: { definition: rule } });
  }

  const report = (measure, groupBy, chart, preset) => ({ measure, groupBy, period: { preset, from: null, to: null }, chart });
  const monthly = await call('POST', '/api/admin/reports/saved', { token: admin, body: { name: 'Faturamento por mês', definition: report('revenue', 'month', 'column', 'yearToDate') } });
  await call('PUT', `/api/admin/reports/saved/${monthly.id}/schedule`, { token: admin, body: { schedule: { weekday: 'Monday', time: '08:00', recipients: ['dono@autocenterpaulista.com.br'], format: 'pdf' } } });
  await call('POST', '/api/admin/reports/saved', { token: admin, body: { name: 'Ticket médio por mecânico', definition: report('averageTicket', 'mechanic', 'table', 'thisMonth') } });
  await call('POST', '/api/admin/reports/saved', { token: admin, body: { name: 'Serviços mais vendidos', definition: report('orderCount', 'service', 'column', 'thisMonth') } });

  await call('POST', '/api/admin/inventory/counts', { token: admin, body: { location: 'GERAL' } });
  await call('POST', '/api/admin/inventory/purchase-orders', {
    token: admin,
    body: { supplierId: suppliers['Distribuidora Freio Sul'], lines: [{ partId: parts['PST-DIA'], quantity: 10, unitCost: 90 }] },
  });

  log('Pronto. Usuários (senha Oficina@2026):');
  for (const member of TEAM) {
    log(`  ${member.role.padEnd(9)} ${member.email}`);
  }

  log(`  Admin     ${ADMIN_EMAIL} (senha ${ADMIN_PASSWORD})`);
  log(`Portal do cliente com orçamento pendente: ${portalQuote.link.replace(/^https?:\/\/[^/]+/, 'http://localhost:4200')}`);
  log(`  confirmação: últimos 3 dígitos do telefone de ${joana.name} (${joana.phone.replace(/\D/g, '').slice(-3)})`);
}

main().catch((error) => {
  console.error(`[seed] falhou: ${error.message}`);
  process.exit(1);
});

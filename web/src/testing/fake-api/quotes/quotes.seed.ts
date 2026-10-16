import { FakeCatalog } from '../catalog/catalog.seed';
import { fakeId } from '../fake-api/fake-ids';

export type FakeQuoteStatus = 'Pending' | 'Approved' | 'Rejected';
export type FakeItemKind = 'Package' | 'Service' | 'Part';
export type FakeDiscountType = 'Percent' | 'Amount';
export type FakeChannel = 'WhatsApp' | 'Email' | 'Link';
export type FakeDeliveryStatus = 'Queued' | 'Sent' | 'Opened' | 'Failed';
export type FakeCondition = 'LoyalCustomer' | 'FirstService' | 'Fleet';
export type FakePaymentMethod = 'Pix' | 'Credit' | 'Debit' | 'Cash' | 'Boleto';

export interface FakeDiscount {
  type: FakeDiscountType;
  value: number;
}

export interface FakeQuoteLine {
  id: string;
  kind: FakeItemKind;
  itemId: string;
  name: string;
  unitPrice: number;
  quantity: number;
  source: 'Manual' | 'Ai';
  fractional: boolean;
}

export interface FakeApproval {
  id: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  discount: FakeDiscount;
  reason: string;
  requestedBy: string;
  requestedAt: string;
  decidedBy: string | null;
}

export interface FakeDelivery {
  id: string;
  channel: FakeChannel;
  status: FakeDeliveryStatus;
  link: string;
  expiresAt: string;
  sentAt: string | null;
  openedAt: string | null;
  failureReason: string | null;
  attempts: number;
}

export interface FakeQuoteRecord {
  id: string;
  number: number;
  serviceOrderId: string;
  version: number;
  status: FakeQuoteStatus;
  items: FakeQuoteLine[];
  discountMode: 'Automatic' | 'Manual' | 'None';
  manualDiscount: (FakeDiscount & { reason: string }) | null;
  payment: { methods: FakePaymentMethod[]; maxInstallments: number };
  approval: FakeApproval | null;
  delivery: FakeDelivery | null;
  rejectionReason: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface FakeServiceOrder {
  id: string;
  number: number;
  customerId: string;
  plate: string;
  vehicle: string;
  mileage: number;
  diagnosis: string | null;
  suggestions: { kind: FakeItemKind; itemId: string; reason: string }[];
}

export interface FakeDiscountRule extends FakeDiscount {
  id: string;
  name: string;
  condition: FakeCondition;
  isActive: boolean;
}

export interface FakeQuoteNotification {
  role: 'Manager';
  text: string;
  quoteId: string;
}

export interface FakeQuotes {
  serviceOrders: FakeServiceOrder[];
  quotes: FakeQuoteRecord[];
  rules: FakeDiscountRule[];
  payment: {
    methods: FakePaymentMethod[];
    maxInstallments: number;
    interestFreeInstallments: number;
    monthlyInterestPercent: number;
    fees: { method: FakePaymentMethod; feePercent: number; settlementDays: number }[];
  };
  discountLimits: { Admin: number; Manager: number; Attendant: number; Mechanic: number };
  expirationDays: number[];
  loyalCustomerIds: string[];
  firstServiceCustomerIds: string[];
  fleetCustomerIds: string[];
  flakyCustomerIds: string[];
  notifications: FakeQuoteNotification[];
}

export const FAKE_PORTAL_ORIGIN = 'https://portal.oficina.example';

interface SeedCustomer {
  readonly id: string;
  readonly name: string;
}

const daysFromNow = (days: number): string =>
  new Date(Date.now() + days * 24 * 60 * 60 * 1000).toISOString();

function byName<T extends { name: string; id: string }>(items: readonly T[], name: string): T {
  const item = items.find((candidate) => candidate.name === name);
  if (!item) {
    throw new Error(`Seed item ${name} not found.`);
  }

  return item;
}

export function createQuotesSeed(
  catalog: FakeCatalog,
  customers: readonly SeedCustomer[],
): FakeQuotes {
  const [joana, carlos, fleet] = customers;
  if (!joana || !carlos || !fleet) {
    throw new Error('Quotes seed needs three customers.');
  }

  const revision = byName(catalog.packages, 'Revisão 40 mil km');
  const brakes = byName(catalog.packages, 'Kit freio dianteiro');
  const pads = byName(catalog.parts, 'Pastilha de freio dianteira');
  const spark = byName(catalog.parts, 'Vela de ignição');
  const diagnosis = byName(catalog.services, 'Diagnóstico eletrônico');
  const alignment = byName(catalog.services, 'Alinhamento 3D');
  const oilChange = byName(catalog.services, 'Troca de óleo');

  const order = (
    number: number,
    customerId: string,
    plate: string,
    vehicle: string,
    mileage: number,
    text: string | null,
    suggestions: FakeServiceOrder['suggestions'] = [],
  ): FakeServiceOrder => ({
    id: fakeId(),
    number,
    customerId,
    plate,
    vehicle,
    mileage,
    diagnosis: text,
    suggestions,
  });

  const polo = order(
    3321,
    joana.id,
    'FJK4E21',
    'VW Polo 1.0 TSI 2021',
    41_230,
    'Ruído na frenagem dianteira e falha no cilindro 2. Revisão dos 40 mil km vencida.',
    [
      { kind: 'Package', itemId: revision.id, reason: 'Quilometragem dentro da faixa da revisão.' },
      { kind: 'Package', itemId: brakes.id, reason: 'Ruído na frenagem dianteira.' },
      { kind: 'Part', itemId: spark.id, reason: 'Falha de ignição no cilindro 2.' },
    ],
  );
  const fiorino = order(
    3322,
    fleet.id,
    'RSL1A23',
    'Fiat Fiorino 2022',
    88_400,
    'Revisão de frota.',
    [{ kind: 'Service', itemId: oilChange.id, reason: 'Troca de óleo a cada 10 mil km.' }],
  );
  const onix = order(3323, carlos.id, 'QWE5R67', 'Chevrolet Onix 2020', 63_100, null);
  const master = order(3310, fleet.id, 'ABC1234', 'Renault Master 2019', 154_900, null);
  const kombi = order(3301, fleet.id, 'RSL2B34', 'Fiat Fiorino 2022', 92_150, null);

  const line = (
    kind: FakeItemKind,
    itemId: string,
    name: string,
    unitPrice: number,
    quantity: number,
    source: 'Manual' | 'Ai' = 'Manual',
  ): FakeQuoteLine => ({
    id: fakeId(),
    kind,
    itemId,
    name,
    unitPrice,
    quantity,
    source,
    fractional: false,
  });

  const record = (
    number: number,
    serviceOrderId: string,
    version: number,
    status: FakeQuoteStatus,
    items: FakeQuoteLine[],
    extra: Partial<FakeQuoteRecord> = {},
  ): FakeQuoteRecord => ({
    id: fakeId(),
    number,
    serviceOrderId,
    version,
    status,
    items,
    discountMode: 'Automatic',
    manualDiscount: null,
    payment: { methods: ['Pix', 'Credit', 'Debit'], maxInstallments: 6 },
    approval: null,
    delivery: null,
    rejectionReason: null,
    createdAt: daysFromNow(-3),
    updatedAt: daysFromNow(-1),
    ...extra,
  });

  return {
    serviceOrders: [polo, fiorino, onix, master, kombi],
    quotes: [
      record(1047, polo.id, 1, 'Pending', [
        line('Package', revision.id, revision.name, revision.price, 1),
        line('Part', pads.id, pads.name, 179, 1),
        line('Part', spark.id, spark.name, spark.unitPrice, 4, 'Ai'),
      ]),
      record(
        1042,
        master.id,
        1,
        'Rejected',
        [
          line('Service', diagnosis.id, diagnosis.name, diagnosis.basePrice, 1),
          line('Service', alignment.id, alignment.name, alignment.basePrice, 1),
        ],
        { rejectionReason: 'Achou o alinhamento caro.', createdAt: daysFromNow(-12) },
      ),
      record(
        1042,
        master.id,
        2,
        'Pending',
        [line('Service', diagnosis.id, diagnosis.name, diagnosis.basePrice, 1)],
        {
          createdAt: daysFromNow(-10),
          delivery: {
            id: fakeId(),
            channel: 'Link',
            status: 'Sent',
            link: `${FAKE_PORTAL_ORIGIN}/q/expired-demo`,
            expiresAt: daysFromNow(-3),
            sentAt: daysFromNow(-10),
            openedAt: null,
            failureReason: null,
            attempts: 1,
          },
        },
      ),
      record(
        1040,
        kombi.id,
        1,
        'Approved',
        [line('Service', oilChange.id, oilChange.name, oilChange.basePrice, 1)],
        { createdAt: daysFromNow(-20) },
      ),
    ],
    rules: [
      {
        id: fakeId(),
        name: 'Cliente fiel',
        type: 'Percent',
        value: 5,
        condition: 'LoyalCustomer',
        isActive: true,
      },
      {
        id: fakeId(),
        name: 'Primeiro serviço',
        type: 'Amount',
        value: 30,
        condition: 'FirstService',
        isActive: true,
      },
      {
        id: fakeId(),
        name: 'Frota',
        type: 'Percent',
        value: 8,
        condition: 'Fleet',
        isActive: true,
      },
      {
        id: fakeId(),
        name: 'Aniversário da oficina',
        type: 'Percent',
        value: 12,
        condition: 'LoyalCustomer',
        isActive: false,
      },
    ],
    payment: {
      methods: ['Pix', 'Credit', 'Debit', 'Cash', 'Boleto'],
      maxInstallments: 10,
      interestFreeInstallments: 3,
      monthlyInterestPercent: 1.99,
      fees: [
        { method: 'Pix', feePercent: 0.99, settlementDays: 0 },
        { method: 'Credit', feePercent: 3.49, settlementDays: 30 },
        { method: 'Debit', feePercent: 1.89, settlementDays: 1 },
        { method: 'Cash', feePercent: 0, settlementDays: 0 },
        { method: 'Boleto', feePercent: 2.5, settlementDays: 2 },
      ],
    },
    discountLimits: { Admin: 30, Manager: 20, Attendant: 10, Mechanic: 0 },
    expirationDays: [1, 3, 7, 15],
    loyalCustomerIds: [joana.id],
    firstServiceCustomerIds: [carlos.id],
    fleetCustomerIds: [fleet.id],
    flakyCustomerIds: [carlos.id],
    notifications: [],
  };
}

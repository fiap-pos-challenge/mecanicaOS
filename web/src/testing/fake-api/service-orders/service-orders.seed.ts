import { FakeCatalog } from '../catalog/catalog.seed';
import { fakeId } from '../fake-api/fake-ids';

export type FakeOrderStatus =
  'Received' | 'InDiagnosis' | 'WaitingApproval' | 'InProgress' | 'Completed' | 'Delivered';

export type FakeTag = 'Late' | 'MissingPart' | 'ReminderSent';

export interface FakeHistoryEntry {
  previousStatus: FakeOrderStatus | null;
  newStatus: FakeOrderStatus;
  changedBy: string | null;
  note: string | null;
  changedAt: string;
}

export interface FakeChecklistItem {
  id: string;
  label: string;
  state: 'Ok' | 'Attention' | 'Replace' | null;
}

export interface FakePhoto {
  id: string;
  clientId: string | null;
  url: string;
  caption: string;
  takenAt: string;
}

export interface FakeTimeEntry {
  id: string;
  mechanicName: string;
  startedAt: string;
  endedAt: string | null;
}

export interface FakeOrderQuoteItem {
  name: string;
  kind: 'Service' | 'Part';
  quantity: number;
  unitPrice: number;
}

export interface FakeBoardOrder {
  id: string;
  number: number;
  status: FakeOrderStatus;
  version: number;
  customerId: string;
  plate: string;
  vehicle: string;
  mileage: number;
  summary: string;
  complaint: string;
  mechanicName: string | null;
  promisedAt: string | null;
  tags: FakeTag[];
  quoteItems: FakeOrderQuoteItem[];
  quoteStatus: 'Pending' | 'Approved' | 'Rejected' | null;
  discount: number;
  history: FakeHistoryEntry[];
  checklist: FakeChecklistItem[];
  photos: FakePhoto[];
  diagnosis: string;
  sentToQuote: boolean;
  timeEntries: FakeTimeEntry[];
  parts: {
    name: string;
    quantity: number;
    unit: string;
    reserved: boolean;
    leadTimeDays: number | null;
  }[];
  documents: { kind: 'ServiceOrder' | 'Quote' | 'Receipt'; code: string; issuedAt: string }[];
  updatedAt: string;
}

export interface FakeServiceOrders {
  orders: FakeBoardOrder[];
}

export const FAKE_MECHANIC_NAME = 'Marcos Mecânico';

const STEPS: readonly FakeOrderStatus[] = [
  'Received',
  'InDiagnosis',
  'WaitingApproval',
  'InProgress',
  'Completed',
  'Delivered',
];

const hoursFromNow = (hours: number): string =>
  new Date(Date.now() + hours * 60 * 60 * 1000).toISOString();

const CHECKLIST = ['Óleo e filtros', 'Freios', 'Suspensão dianteira', 'Pneus', 'Luzes', 'Bateria'];

function checklist(): FakeChecklistItem[] {
  return CHECKLIST.map((label) => ({ id: fakeId(), label, state: null }));
}

function historyUntil(status: FakeOrderStatus, startedHoursAgo: number): FakeHistoryEntry[] {
  const steps = STEPS.slice(0, STEPS.indexOf(status) + 1);
  return steps.map((step, index) => ({
    previousStatus: index === 0 ? null : (steps[index - 1] ?? null),
    newStatus: step,
    changedBy:
      index === 0 ? 'Ana Atendente' : index % 2 === 0 ? 'Ana Atendente' : FAKE_MECHANIC_NAME,
    note: null,
    changedAt: hoursFromNow(-startedHoursAgo + index * 2),
  }));
}

export function createServiceOrdersSeed(
  catalog: FakeCatalog,
  customers: readonly { readonly id: string }[],
): FakeServiceOrders {
  const [joana, carlos, fleet] = customers;
  if (!joana || !carlos || !fleet) {
    throw new Error('Service orders seed needs three customers.');
  }

  const price = (name: string): number =>
    catalog.services.find((item) => item.name === name)?.basePrice ??
    catalog.parts.find((item) => item.name === name)?.unitPrice ??
    0;
  const item = (name: string, kind: 'Service' | 'Part', quantity = 1): FakeOrderQuoteItem => ({
    name,
    kind,
    quantity,
    unitPrice: price(name),
  });

  const order = (
    number: number,
    status: FakeOrderStatus,
    customerId: string,
    plate: string,
    vehicle: string,
    summary: string,
    extra: Partial<FakeBoardOrder> = {},
  ): FakeBoardOrder => ({
    id: fakeId(),
    number,
    status,
    version: STEPS.indexOf(status) + 1,
    customerId,
    plate,
    vehicle,
    mileage: 40_000 + number,
    summary,
    complaint: summary,
    mechanicName: null,
    promisedAt: null,
    tags: [],
    quoteItems: [],
    quoteStatus: null,
    discount: 0,
    history: historyUntil(status, 30),
    checklist: checklist(),
    photos: [],
    diagnosis: '',
    sentToQuote: false,
    timeEntries: [],
    parts: [],
    documents: [],
    updatedAt: hoursFromNow(-1),
    ...extra,
  });

  const brakes = [
    item('Troca de pastilhas e discos', 'Service'),
    item('Pastilha de freio dianteira', 'Part'),
  ];
  return {
    orders: [
      order(3326, 'Received', joana.id, 'GHI7A04', 'Jeep Renegade 2020', 'Revisão 60 mil km', {
        promisedAt: hoursFromNow(48),
      }),
      order(
        3327,
        'InDiagnosis',
        carlos.id,
        'QRT2B19',
        'Fiat Argo 2021',
        'Ruído na suspensão dianteira',
        {
          complaint: 'Ruído na suspensão dianteira em lombadas',
          mechanicName: FAKE_MECHANIC_NAME,
          promisedAt: hoursFromNow(30),
        },
      ),
      order(3322, 'InDiagnosis', fleet.id, 'RSL1A23', 'Fiat Fiorino 2022', 'Revisão de frota', {
        mechanicName: 'Pedro Convidado',
        quoteItems: [item('Troca de óleo', 'Service'), item('Óleo 5W30 sintético', 'Part', 4)],
        quoteStatus: 'Pending',
        promisedAt: hoursFromNow(20),
      }),
      order(
        3324,
        'WaitingApproval',
        joana.id,
        'BRA2E19',
        'Chevrolet Onix Plus 2022',
        'Freios dianteiros',
        {
          tags: ['ReminderSent'],
          quoteItems: [...brakes, item('Alinhamento 3D', 'Service')],
          quoteStatus: 'Pending',
          discount: 20,
          promisedAt: hoursFromNow(26),
          photos: [
            {
              id: fakeId(),
              clientId: null,
              url: 'data:image/gif;base64,R0lGODlhAQABAAAAACw=',
              caption: 'Pastilha gasta',
              takenAt: hoursFromNow(-20),
            },
          ],
          documents: [{ kind: 'Quote', code: 'ORC1-3324-A7KD', issuedAt: hoursFromNow(-20) }],
        },
      ),
      order(3325, 'WaitingApproval', fleet.id, 'PLM3J22', 'Renault Kwid 2019', 'Embreagem', {
        quoteItems: [item('Diagnóstico eletrônico', 'Service')],
        quoteStatus: 'Pending',
      }),
      order(3318, 'InProgress', carlos.id, 'EXP9C88', 'Honda Civic 2018', 'Embreagem completa', {
        mechanicName: FAKE_MECHANIC_NAME,
        promisedAt: hoursFromNow(-6),
        quoteItems: [item('Diagnóstico eletrônico', 'Service')],
        quoteStatus: 'Approved',
        timeEntries: [
          {
            id: fakeId(),
            mechanicName: FAKE_MECHANIC_NAME,
            startedAt: hoursFromNow(-5),
            endedAt: hoursFromNow(-3),
          },
        ],
      }),
      order(
        3319,
        'InProgress',
        joana.id,
        'KLO3D55',
        'Hyundai HB20 2020',
        'Troca de óleo e filtros',
        {
          mechanicName: 'Pedro Convidado',
          promisedAt: hoursFromNow(5),
          quoteItems: [item('Troca de óleo', 'Service')],
          quoteStatus: 'Approved',
        },
      ),
      order(3315, 'InProgress', fleet.id, 'FMA1G33', 'Toyota Corolla 2017', 'Aguardando pastilha', {
        mechanicName: FAKE_MECHANIC_NAME,
        tags: ['MissingPart'],
        quoteItems: brakes,
        quoteStatus: 'Approved',
        parts: [
          {
            name: 'Pastilha de freio dianteira',
            quantity: 1,
            unit: 'jg',
            reserved: false,
            leadTimeDays: 3,
          },
          {
            name: 'Disco de freio dianteiro',
            quantity: 2,
            unit: 'un',
            reserved: true,
            leadTimeDays: null,
          },
        ],
      }),
      order(3310, 'Completed', joana.id, 'FJK4E21', 'VW Polo 2021', 'Revisão 40 mil km', {
        quoteItems: [item('Troca de óleo', 'Service')],
        quoteStatus: 'Approved',
        documents: [
          { kind: 'ServiceOrder', code: 'OS01-3310-QW8P', issuedAt: hoursFromNow(-30) },
          { kind: 'Receipt', code: 'REC1-3310-M2XT', issuedAt: hoursFromNow(-2) },
        ],
      }),
      order(3301, 'Delivered', fleet.id, 'ABC1234', 'Renault Master 2019', 'Troca de óleo', {
        quoteItems: [item('Troca de óleo', 'Service')],
        quoteStatus: 'Approved',
      }),
    ],
  };
}

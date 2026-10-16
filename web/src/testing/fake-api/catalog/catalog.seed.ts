import { fakeId } from '../fake-api/fake-ids';

export interface FakeCatalogService {
  id: string;
  name: string;
  description: string | null;
  basePrice: number;
  estimatedMinutes: number;
  commissionPercent: number;
  warrantyDays: number;
  usualParts: { partId: string; quantity: number }[];
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface FakeCatalogPart {
  id: string;
  code: string;
  name: string;
  type: 'Part' | 'Supply';
  unitOfMeasure: string;
  unitPrice: number;
  cost: number;
  stockQuantity: number;
  minimumStock: number;
  supplier: string | null;
  barcode: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface FakePackageItem {
  kind: 'Service' | 'Part';
  itemId: string;
  quantity: number;
}

export interface FakeCatalogPackage {
  id: string;
  name: string;
  description: string | null;
  items: FakePackageItem[];
  price: number;
  suggestion: { minMileage: number | null; maxMileage: number | null } | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export type FakePriceField = 'BasePrice' | 'PackagePrice' | 'Cost' | 'UnitPrice';

export interface FakePriceChange {
  kind: 'services' | 'packages' | 'parts';
  itemId: string;
  field: FakePriceField;
  oldValue: number | null;
  newValue: number;
  changedBy: string;
  changedAt: string;
}

export interface FakeCatalog {
  services: FakeCatalogService[];
  parts: FakeCatalogPart[];
  packages: FakeCatalogPackage[];
  priceHistory: FakePriceChange[];
}

const seededAt = '2026-09-01T12:00:00Z';
const repricedAt = '2026-09-20T15:30:00Z';
const seededBy = 'Administrador';

function service(
  name: string,
  minutes: number,
  price: number,
  commission: number,
  isActive = true,
): FakeCatalogService {
  return {
    id: fakeId(),
    name,
    description: null,
    basePrice: price,
    estimatedMinutes: minutes,
    commissionPercent: commission,
    warrantyDays: 90,
    usualParts: [],
    isActive,
    createdAt: seededAt,
    updatedAt: null,
  };
}

function part(
  code: string,
  name: string,
  type: 'Part' | 'Supply',
  unit: string,
  cost: number,
  price: number,
  stock: number,
  minimum: number,
  supplier: string,
  barcode: string | null,
): FakeCatalogPart {
  return {
    id: fakeId(),
    code,
    name,
    type,
    unitOfMeasure: unit,
    unitPrice: price,
    cost,
    stockQuantity: stock,
    minimumStock: minimum,
    supplier,
    barcode,
    isActive: true,
    createdAt: seededAt,
    updatedAt: null,
  };
}

export function createCatalogSeed(): FakeCatalog {
  const oil = part(
    'PEC-0101',
    'Óleo 5W30 sintético',
    'Supply',
    'L',
    32.24,
    52,
    48,
    30,
    'Lubrificantes Paulista',
    '7891234567895',
  );
  const oilFilter = part(
    'PEC-0115',
    'Filtro de óleo Polo TSI',
    'Part',
    'un',
    20.9,
    38,
    12,
    10,
    'Auto Peças Centro',
    null,
  );
  const airFilter = part(
    'PEC-0120',
    'Filtro de ar',
    'Part',
    'un',
    28,
    49,
    9,
    6,
    'Auto Peças Centro',
    null,
  );
  const pads = part(
    'PEC-0412',
    'Pastilha de freio dianteira',
    'Part',
    'jogo',
    112,
    189,
    2,
    8,
    'Distribuidora Freio Sul',
    '7890000000017',
  );
  const discs = part(
    'PEC-0415',
    'Disco de freio dianteiro',
    'Part',
    'par',
    210,
    349,
    4,
    2,
    'Distribuidora Freio Sul',
    null,
  );
  const spark = part(
    'PEC-0107',
    'Vela de ignição',
    'Part',
    'un',
    21.12,
    44,
    0,
    12,
    'Auto Peças Centro',
    null,
  );
  const coolant = part(
    'INS-0201',
    'Aditivo de radiador',
    'Supply',
    'L',
    18,
    29.9,
    20,
    10,
    'Lubrificantes Paulista',
    null,
  );

  const oilChange = service('Troca de óleo', 30, 60, 10);
  const alignment = service('Alinhamento 3D', 30, 90, 10);
  const balancing = service('Balanceamento (4 rodas)', 30, 80, 10);
  const diagnosis = service('Diagnóstico eletrônico', 30, 120, 15);
  const ac = service('Higienização do ar-condicionado', 45, 150, 12);
  const head = service('Retífica de cabeçote', 480, 1800, 8, false);
  const inspection = service('Inspeção de 32 itens', 60, 140, 10);
  const brakeLabor = service('Troca de pastilhas e discos', 90, 260, 12);
  oilChange.usualParts = [
    { partId: oil.id, quantity: 4 },
    { partId: oilFilter.id, quantity: 1 },
  ];
  brakeLabor.usualParts = [
    { partId: pads.id, quantity: 1 },
    { partId: discs.id, quantity: 1 },
  ];

  const revision: FakeCatalogPackage = {
    id: fakeId(),
    name: 'Revisão 40 mil km',
    description: 'Óleo, filtros, velas e inspeção de 32 itens',
    items: [
      { kind: 'Service', itemId: oilChange.id, quantity: 1 },
      { kind: 'Part', itemId: oil.id, quantity: 4 },
      { kind: 'Part', itemId: oilFilter.id, quantity: 1 },
      { kind: 'Part', itemId: airFilter.id, quantity: 1 },
      { kind: 'Part', itemId: spark.id, quantity: 4 },
      { kind: 'Service', itemId: inspection.id, quantity: 1 },
    ],
    price: 559,
    suggestion: { minMileage: 38_000, maxMileage: 42_000 },
    isActive: true,
    createdAt: seededAt,
    updatedAt: null,
  };
  const brakes: FakeCatalogPackage = {
    id: fakeId(),
    name: 'Kit freio dianteiro',
    description: 'Pastilhas, discos e mão de obra',
    items: [
      { kind: 'Part', itemId: pads.id, quantity: 1 },
      { kind: 'Part', itemId: discs.id, quantity: 1 },
      { kind: 'Service', itemId: brakeLabor.id, quantity: 1 },
    ],
    price: 740,
    suggestion: null,
    isActive: true,
    createdAt: seededAt,
    updatedAt: null,
  };
  const trip: FakeCatalogPackage = {
    id: fakeId(),
    name: 'Check-up de viagem',
    description: 'Fluidos, pneus, luzes e bateria',
    items: [
      { kind: 'Service', itemId: inspection.id, quantity: 1 },
      { kind: 'Part', itemId: coolant.id, quantity: 1 },
    ],
    price: 129.9,
    suggestion: null,
    isActive: true,
    createdAt: seededAt,
    updatedAt: null,
  };

  const services = [oilChange, alignment, balancing, diagnosis, ac, head, inspection, brakeLabor];
  const parts = [oil, oilFilter, airFilter, pads, discs, spark, coolant];
  const packages = [revision, brakes, trip];
  const created = (
    kind: FakePriceChange['kind'],
    itemId: string,
    field: FakePriceField,
    value: number,
  ): FakePriceChange => ({
    kind,
    itemId,
    field,
    oldValue: null,
    newValue: value,
    changedBy: seededBy,
    changedAt: seededAt,
  });
  return {
    services,
    parts,
    packages,
    priceHistory: [
      ...services.map((item) => created('services', item.id, 'BasePrice', item.basePrice)),
      ...parts.flatMap((item) => [
        created('parts', item.id, 'Cost', item.cost),
        created('parts', item.id, 'UnitPrice', item.id === pads.id ? 179 : item.unitPrice),
      ]),
      ...packages.map((item) => created('packages', item.id, 'PackagePrice', item.price)),
      {
        kind: 'parts',
        itemId: pads.id,
        field: 'UnitPrice',
        oldValue: 179,
        newValue: 189,
        changedBy: 'Gabi Gestora',
        changedAt: repricedAt,
      },
    ],
  };
}

import { messages } from '../../../app/core/i18n/messages.pt-BR';
import { documentKindOf } from '../../../app/shared/util/document';
import { onlyDigits } from '../../../app/shared/util/digits';
import { FakeCatalogPart } from '../catalog/catalog.seed';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import {
  accepted,
  created,
  fieldError,
  FakeRouter,
  ok,
  paginate,
  problem,
  readBody,
  readText,
  validation,
} from '../fake-api/fake-http';
import { fakeId, fakeNow } from '../fake-api/fake-ids';
import { authorize, FakePrincipal, FakeRole, isPrincipal } from '../fake-api/fake-session';

export const INVENTORY_PATHS = {
  stock: '/api/admin/inventory/stock',
  movements: '/api/admin/inventory/stock/:partId/movements',
  entries: '/api/admin/inventory/entries',
  exits: '/api/admin/inventory/exits',
  barcode: '/api/admin/inventory/barcodes/lookup',
  invoiceRead: '/api/admin/inventory/invoices/read',
  invoiceConfirm: '/api/admin/inventory/invoices/confirm',
  counts: '/api/admin/inventory/counts',
  count: '/api/admin/inventory/counts/:id',
  countLines: '/api/admin/inventory/counts/:id/lines',
  countAction: '/api/admin/inventory/counts/:id/:action',
  suppliers: '/api/admin/inventory/suppliers',
  supplier: '/api/admin/inventory/suppliers/:id',
  suggestions: '/api/admin/inventory/purchase-orders/suggestions',
  orders: '/api/admin/inventory/purchase-orders',
  sendOrder: '/api/admin/inventory/purchase-orders/:id/send',
} as const;

const DAY_MS = 24 * 60 * 60 * 1000;
const readers: readonly FakeRole[] = ['Admin', 'Manager', 'Attendant', 'Mechanic'];
const counters: readonly FakeRole[] = ['Admin', 'Manager', 'Attendant', 'Mechanic'];
const managers: readonly FakeRole[] = ['Admin', 'Manager'];
const text = messages.fake;

export interface FakeStockInfo {
  location: string;
  outflow30d: number;
  outflowPreviousMonth: number;
  lastOutflowAt: string | null;
}

export interface FakeMovement {
  id: string;
  partId: string;
  type: 'In' | 'Out' | 'Adjustment';
  quantity: number;
  balanceAfter: number;
  reason: string;
  userName: string;
  serviceOrderNumber: number | null;
  occurredAt: string;
}

export interface FakeCountLine {
  partId: string;
  systemQuantity: number;
  countedQuantity: number | null;
}

export interface FakeCount {
  id: string;
  location: string;
  status: 'Open' | 'PendingApproval' | 'Approved' | 'Rejected';
  openedBy: string;
  openedAt: string;
  submittedAt: string | null;
  decidedBy: string | null;
  decidedAt: string | null;
  rejectionReason: string | null;
  lines: FakeCountLine[];
}

export interface FakeSupplier {
  id: string;
  name: string;
  document: string | null;
  email: string | null;
  phone: string | null;
  leadTimeDays: number;
  isActive: boolean;
}

export interface FakePurchaseOrder {
  id: string;
  number: number;
  supplierId: string;
  status: 'Draft' | 'Queued' | 'Sent';
  createdAt: string;
  sentAt: string | null;
  lines: { partId: string; quantity: number; unitCost: number }[];
}

export interface FakeInventory {
  stock: Record<string, FakeStockInfo>;
  movements: FakeMovement[];
  counts: FakeCount[];
  suppliers: FakeSupplier[];
  orders: FakePurchaseOrder[];
}

const LOCATIONS: Readonly<Record<string, string>> = {
  'PEC-0101': 'C1-01',
  'PEC-0115': 'B2-04',
  'PEC-0120': 'B2-05',
  'PEC-0412': 'A3-02',
  'PEC-0415': 'A3-03',
  'PEC-0107': 'B1-05',
  'INS-0201': 'C1-02',
};

export function createInventorySeed(parts: readonly FakeCatalogPart[]): FakeInventory {
  const now = Date.now();
  const daysAgo = (days: number): string => new Date(now - days * DAY_MS).toISOString();
  const outflows: Readonly<Record<string, [number, number, number | null]>> = {
    'PEC-0101': [180, 150, 2],
    'PEC-0115': [18, 15, 3],
    'PEC-0120': [9, 11, 6],
    'PEC-0412': [22, 20, 1],
    'PEC-0415': [0, 2, 120],
    'PEC-0107': [40, 36, 4],
    'INS-0201': [6, 5, 10],
  };
  const stock: Record<string, FakeStockInfo> = {};
  for (const part of parts) {
    const [outflow30d, previous, lastDays] = outflows[part.code] ?? [0, 0, null];
    stock[part.id] = {
      location: (LOCATIONS[part.code] ?? 'D1-01').slice(0, 2),
      outflow30d,
      outflowPreviousMonth: previous,
      lastOutflowAt: lastDays === null ? null : daysAgo(lastDays),
    };
  }

  const pads = parts.find((part) => part.code === 'PEC-0412');
  return {
    stock,
    movements: pads
      ? [
          {
            id: fakeId(),
            partId: pads.id,
            type: 'In',
            quantity: 10,
            balanceAfter: 10,
            reason: 'Nota 4512 · Distribuidora Freio Sul',
            userName: 'Gabi Gestora',
            serviceOrderNumber: null,
            occurredAt: daysAgo(20),
          },
          {
            id: fakeId(),
            partId: pads.id,
            type: 'Out',
            quantity: 8,
            balanceAfter: 2,
            reason: 'Aplicada na OS',
            userName: 'Marcos Mecânico',
            serviceOrderNumber: 3021,
            occurredAt: daysAgo(1),
          },
        ]
      : [],
    counts: [],
    suppliers: [
      {
        id: fakeId(),
        name: 'Distribuidora Freio Sul',
        document: '11222333000181',
        email: 'vendas@freiosul.example.com',
        phone: '5133334444',
        leadTimeDays: 3,
        isActive: true,
      },
      {
        id: fakeId(),
        name: 'Auto Peças Centro',
        document: null,
        email: 'pedidos@autopecas.example.com',
        phone: null,
        leadTimeDays: 2,
        isActive: true,
      },
      {
        id: fakeId(),
        name: 'Lubrificantes Paulista',
        document: null,
        email: null,
        phone: '1140041234',
        leadTimeDays: 5,
        isActive: true,
      },
    ],
    orders: [],
  };
}

function isQuantityFor(type: 'Part' | 'Supply', quantity: unknown): quantity is number {
  if (typeof quantity !== 'number' || !Number.isFinite(quantity) || quantity <= 0) {
    return false;
  }

  return type === 'Part'
    ? Number.isInteger(quantity)
    : Math.abs(quantity * 1000 - Math.round(quantity * 1000)) < 1e-6;
}

function round(value: number): number {
  return Math.round(value * 1000) / 1000;
}

export function createInventoryFakeApi(state: FakeBackendState): FakeFeatureApi {
  const db = (): FakeInventory => state.database.inventory;
  const parts = (): FakeCatalogPart[] => state.database.catalog.parts;
  const users = (): FakeBackendState['database']['users'] => state.database.users;
  const nameOf = (principal: FakePrincipal): string =>
    users().find((user) => user.id === principal.id)?.name ?? messages.roles[principal.role];
  const info = (part: FakeCatalogPart): FakeStockInfo =>
    db().stock[part.id] ?? {
      location: 'D1',
      outflow30d: 0,
      outflowPreviousMonth: 0,
      lastOutflowAt: null,
    };

  const situationOf = (part: FakeCatalogPart): string => {
    const lastOutflow = info(part).lastOutflowAt;
    if (part.stockQuantity <= 0) {
      return 'OutOfStock';
    }

    if (part.stockQuantity < part.minimumStock) {
      return 'Reorder';
    }

    return !lastOutflow || Date.now() - Date.parse(lastOutflow) > 90 * DAY_MS ? 'Idle' : 'Ok';
  };

  const itemDto = (part: FakeCatalogPart): Record<string, unknown> => ({
    partId: part.id,
    code: part.code,
    name: part.name,
    type: part.type,
    unitOfMeasure: part.unitOfMeasure,
    location: info(part).location,
    stockQuantity: part.stockQuantity,
    minimumStock: part.minimumStock,
    outflow30d: info(part).outflow30d,
    unitCost: part.cost,
    lastOutflowAt: info(part).lastOutflowAt,
    barcode: part.barcode,
  });

  const suggestedOf = (part: FakeCatalogPart): number => {
    const target = Math.max(part.minimumStock * 2, info(part).outflow30d + part.minimumStock);
    const missing = target - part.stockQuantity;
    if (missing <= 0 || part.stockQuantity >= part.minimumStock) {
      return 0;
    }

    return part.type === 'Part' ? Math.ceil(missing) : Math.ceil(missing * 10) / 10;
  };

  const supplierFor = (part: FakeCatalogPart): FakeSupplier | undefined =>
    db().suppliers.find((supplier) => supplier.name === part.supplier);

  const move = (
    part: FakeCatalogPart,
    type: FakeMovement['type'],
    signedQuantity: number,
    reason: string,
    principal: FakePrincipal,
    serviceOrderNumber: number | null,
  ): FakeMovement => {
    part.stockQuantity = round(part.stockQuantity + signedQuantity);
    if (signedQuantity < 0 && type === 'Out') {
      db().stock[part.id] = { ...info(part), lastOutflowAt: fakeNow() };
    }

    const movement: FakeMovement = {
      id: fakeId(),
      partId: part.id,
      type,
      quantity: type === 'Adjustment' ? signedQuantity : Math.abs(signedQuantity),
      balanceAfter: part.stockQuantity,
      reason,
      userName: nameOf(principal),
      serviceOrderNumber,
      occurredAt: fakeNow(),
    };
    db().movements.push(movement);
    return movement;
  };

  const countDto = (count: FakeCount): Record<string, unknown> => ({
    ...count,
    lines: count.lines.map((line) => {
      const part = parts().find((candidate) => candidate.id === line.partId);
      return {
        ...line,
        code: part?.code ?? '',
        name: part?.name ?? '',
        type: part?.type ?? 'Part',
        unitOfMeasure: part?.unitOfMeasure ?? 'un',
      };
    }),
  });

  const orderDto = (order: FakePurchaseOrder): Record<string, unknown> => ({
    ...order,
    supplierName: db().suppliers.find((supplier) => supplier.id === order.supplierId)?.name ?? '',
    total:
      Math.round(order.lines.reduce((sum, line) => sum + line.quantity * line.unitCost * 100, 0)) /
      100,
    lines: order.lines.map((line) => {
      const part = parts().find((candidate) => candidate.id === line.partId);
      return { ...line, code: part?.code ?? '', name: part?.name ?? '' };
    }),
  });

  const readSupplier = (
    body: Readonly<Record<string, unknown>>,
  ): Omit<FakeSupplier, 'id' | 'isActive'> | ReturnType<typeof fieldError>[] => {
    const errors: ReturnType<typeof fieldError>[] = [];
    const name = readText(body, 'name').trim();
    const document = onlyDigits(readText(body, 'document'));
    const email = readText(body, 'email').trim();
    const phone = onlyDigits(readText(body, 'phone'));
    const leadTime = body['leadTimeDays'];
    if (name.length < 2 || name.length > 120) {
      errors.push(fieldError('name', 'inventory.invalidSupplierName', text.invalidCatalogName));
    }

    if (document && documentKindOf(document) !== 'Cnpj') {
      errors.push(fieldError('document', 'inventory.invalidSupplierDocument', text.invalidField));
    }

    if (email && !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email)) {
      errors.push(fieldError('email', 'common.invalidEmail', text.invalidField));
    }

    if (
      typeof leadTime !== 'number' ||
      !Number.isInteger(leadTime) ||
      leadTime < 0 ||
      leadTime > 365
    ) {
      errors.push(fieldError('leadTimeDays', 'inventory.invalidLeadTime', text.invalidField));
    }

    return errors.length > 0 || typeof leadTime !== 'number'
      ? errors
      : {
          name,
          document: document || null,
          email: email || null,
          phone: phone || null,
          leadTimeDays: leadTime,
        };
  };

  const findPart = (id: string): FakeCatalogPart | undefined =>
    parts().find((part) => part.id === id);

  const router = new FakeRouter()
    .on('GET', INVENTORY_PATHS.stock, (request) => {
      const principal = authorize(request, users(), readers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const term = (request.query.get('term') ?? '').toLowerCase();
      const situation = request.query.get('situation');
      const location = request.query.get('location');
      const all = parts().filter((part) => part.isActive);
      const filtered = all
        .filter(
          (part) =>
            !term ||
            part.name.toLowerCase().includes(term) ||
            part.code.toLowerCase().includes(term),
        )
        .filter((part) => !situation || situationOf(part) === situation)
        .filter((part) => !location || info(part).location === location)
        .sort((left, right) => left.code.localeCompare(right.code));
      const page = paginate(filtered.map(itemDto), request.query);
      if (!('body' in page)) {
        return page;
      }

      const below = all.filter((part) => part.stockQuantity < part.minimumStock);
      const idle = all.filter((part) => situationOf(part) === 'Idle');
      const outflow = all.reduce((sum, part) => sum + info(part).outflow30d, 0);
      const previous = all.reduce((sum, part) => sum + info(part).outflowPreviousMonth, 0);
      const stockValue = all.reduce((sum, part) => sum + part.stockQuantity * part.cost, 0);
      const monthlyCost = all.reduce((sum, part) => sum + info(part).outflow30d * part.cost, 0);
      return ok({
        ...(page.body as Record<string, unknown>),
        locations: [...new Set(all.map((part) => info(part).location))].sort(),
        kpis: {
          belowMinimum: below.length,
          suggestedOrderTotal:
            Math.round(below.reduce((sum, part) => sum + suggestedOf(part) * part.cost, 0) * 100) /
            100,
          outflowMonth: outflow,
          outflowMonthDeltaPercent:
            previous === 0 ? null : Math.round(((outflow - previous) / previous) * 100),
          turnoverDays: monthlyCost === 0 ? null : Math.round((stockValue / monthlyCost) * 30),
          turnoverGoalDays: 30,
          idleItems: idle.length,
          idleValue:
            Math.round(idle.reduce((sum, part) => sum + part.stockQuantity * part.cost, 0) * 100) /
            100,
          stockValue: Math.round(stockValue * 100) / 100,
          totalItems: all.length,
        },
      });
    })
    .on('GET', INVENTORY_PATHS.movements, (request, params) => {
      const principal = authorize(request, users(), readers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const part = findPart(params['partId'] ?? '');
      if (!part) {
        return problem(404, 'catalog.notFound', text.catalogItemNotFound);
      }

      return ok({
        item: itemDto(part),
        items: db()
          .movements.filter((movement) => movement.partId === part.id)
          .reverse()
          .sort((left, right) => right.occurredAt.localeCompare(left.occurredAt)),
      });
    })
    .on('POST', INVENTORY_PATHS.entries, (request) => {
      const principal = authorize(request, users(), managers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      const part = findPart(readText(body, 'partId'));
      if (!part) {
        return validation([fieldError('partId', 'catalog.notFound', text.catalogItemNotFound)]);
      }

      if (!isQuantityFor(part.type, body['quantity'])) {
        return validation([
          fieldError('quantity', 'inventory.fractionalPart', text.fractionalPart),
        ]);
      }

      const unitCost = body['unitCost'];
      if (typeof unitCost !== 'number' || unitCost < 0) {
        return validation([fieldError('unitCost', 'catalog.invalidPrice', text.invalidPrice)]);
      }

      const reason = readText(body, 'reason').trim() || text.manualEntry;
      return created(move(part, 'In', body['quantity'], reason, principal, null));
    })
    .on('POST', INVENTORY_PATHS.exits, (request) => {
      const principal = authorize(request, users(), managers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      const part = findPart(readText(body, 'partId'));
      if (!part) {
        return validation([fieldError('partId', 'catalog.notFound', text.catalogItemNotFound)]);
      }

      const quantity = body['quantity'];
      if (!isQuantityFor(part.type, quantity)) {
        return validation([
          fieldError('quantity', 'inventory.fractionalPart', text.fractionalPart),
        ]);
      }

      if (round(part.stockQuantity - quantity) < 0) {
        return problem(
          422,
          'inventory.insufficientStock',
          text.insufficientStock(part.stockQuantity),
        );
      }

      const order = body['serviceOrderNumber'];
      return created(
        move(
          part,
          'Out',
          -quantity,
          readText(body, 'reason').trim() || text.manualExit,
          principal,
          typeof order === 'number' ? order : null,
        ),
      );
    })
    .on('POST', INVENTORY_PATHS.barcode, (request) => {
      const principal = authorize(request, users(), readers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const barcode = onlyDigits(readText(readBody(request), 'barcode'));
      const part = parts().find((candidate) => candidate.barcode === barcode);
      return part
        ? ok(itemDto(part))
        : problem(404, 'inventory.unknownBarcode', text.unknownBarcode(barcode));
    })
    .on('POST', INVENTORY_PATHS.invoiceRead, (request) => {
      const principal = authorize(request, users(), managers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      const fileName = readText(body, 'fileName').toLowerCase();
      if (!/\.(xml|pdf)$/.test(fileName) || !readText(body, 'content')) {
        return validation([
          fieldError('file', 'inventory.invalidInvoiceFile', text.invalidInvoiceFile),
        ]);
      }

      const pads = parts().find((part) => part.code === 'PEC-0412');
      const discs = parts().find((part) => part.code === 'PEC-0415');
      const supplier = db().suppliers[0];
      return ok({
        supplierName: supplier?.name ?? 'Distribuidora Freio Sul',
        supplierDocument: supplier?.document ?? null,
        supplierId: supplier?.id ?? null,
        invoiceNumber: '4588',
        issuedAt: fakeNow(),
        lines: [
          {
            description: 'PASTILHA FREIO DIANT',
            barcode: pads?.barcode ?? null,
            partId: pads?.id ?? null,
            quantity: 8,
            unitCost: 110,
          },
          {
            description: 'DISCO FREIO VENT DIANT',
            barcode: null,
            partId: discs?.id ?? null,
            quantity: 2,
            unitCost: 205,
          },
          {
            description: 'FLUIDO FREIO DOT4 500ML',
            barcode: '7896543210988',
            partId: null,
            quantity: 6,
            unitCost: 21.5,
          },
        ],
      });
    })
    .on('POST', INVENTORY_PATHS.invoiceConfirm, (request) => {
      const principal = authorize(request, users(), managers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      const raw = Array.isArray(body['lines']) ? (body['lines'] as unknown[]) : [];
      const lines = raw.map((item) => {
        const line =
          typeof item === 'object' && item !== null ? (item as Record<string, unknown>) : {};
        const part = findPart(String(line['partId'] ?? ''));
        return part && isQuantityFor(part.type, line['quantity'])
          ? { part, quantity: Number(line['quantity']) }
          : null;
      });
      if (raw.length === 0 || lines.some((line) => line === null)) {
        return validation([
          fieldError('lines', 'inventory.invalidInvoiceLines', text.invalidInvoiceLines),
        ]);
      }

      const reason = text.invoiceEntry(readText(body, 'invoiceNumber'));
      for (const line of lines) {
        if (line) {
          move(line.part, 'In', line.quantity, reason, principal, null);
        }
      }

      return created({ lines: lines.length });
    })
    .on('GET', INVENTORY_PATHS.counts, (request) => {
      const principal = authorize(request, users(), counters);
      return isPrincipal(principal)
        ? ok({ items: [...db().counts].reverse().map(countDto) })
        : principal;
    })
    .on('POST', INVENTORY_PATHS.counts, (request) => {
      const principal = authorize(request, users(), counters);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const location = readText(readBody(request), 'location').trim();
      const inLocation = parts().filter(
        (part) => part.isActive && info(part).location === location,
      );
      if (inLocation.length === 0) {
        return validation([
          fieldError('location', 'inventory.unknownLocation', text.unknownLocation),
        ]);
      }

      if (
        db().counts.some(
          (count) =>
            count.location === location &&
            (count.status === 'Open' || count.status === 'PendingApproval'),
        )
      ) {
        return problem(409, 'inventory.countAlreadyOpen', text.countAlreadyOpen(location));
      }

      const count: FakeCount = {
        id: fakeId(),
        location,
        status: 'Open',
        openedBy: nameOf(principal),
        openedAt: fakeNow(),
        submittedAt: null,
        decidedBy: null,
        decidedAt: null,
        rejectionReason: null,
        lines: inLocation.map((part) => ({
          partId: part.id,
          systemQuantity: part.stockQuantity,
          countedQuantity: null,
        })),
      };
      db().counts.push(count);
      return created(countDto(count));
    })
    .on('GET', INVENTORY_PATHS.count, (request, params) => {
      const principal = authorize(request, users(), counters);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const count = db().counts.find((candidate) => candidate.id === params['id']);
      return count
        ? ok(countDto(count))
        : problem(404, 'inventory.countNotFound', text.countNotFound);
    })
    .on('PUT', INVENTORY_PATHS.countLines, (request, params) => {
      const principal = authorize(request, users(), counters);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const count = db().counts.find((candidate) => candidate.id === params['id']);
      if (!count) {
        return problem(404, 'inventory.countNotFound', text.countNotFound);
      }

      if (count.status !== 'Open') {
        return problem(409, 'inventory.countClosed', text.countClosed);
      }

      const raw = Array.isArray(readBody(request)['lines'])
        ? (readBody(request)['lines'] as unknown[])
        : [];
      for (const item of raw) {
        const entry =
          typeof item === 'object' && item !== null ? (item as Record<string, unknown>) : {};
        const line = count.lines.find((candidate) => candidate.partId === entry['partId']);
        const part = line ? findPart(line.partId) : undefined;
        const counted = entry['countedQuantity'];
        if (!line || !part) {
          continue;
        }

        if (counted === null) {
          line.countedQuantity = null;
        } else if (counted === 0 || isQuantityFor(part.type, counted)) {
          line.countedQuantity = Number(counted);
        } else {
          return validation([fieldError('lines', 'inventory.fractionalPart', text.fractionalPart)]);
        }
      }

      return ok(countDto(count));
    })
    .on('POST', INVENTORY_PATHS.countAction, (request, params) => {
      const action = params['action'];
      const principal = authorize(request, users(), action === 'submit' ? counters : managers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const count = db().counts.find((candidate) => candidate.id === params['id']);
      if (!count) {
        return problem(404, 'inventory.countNotFound', text.countNotFound);
      }

      if (action === 'submit') {
        if (count.status !== 'Open') {
          return problem(409, 'inventory.countClosed', text.countClosed);
        }

        if (count.lines.some((line) => line.countedQuantity === null)) {
          return problem(422, 'inventory.countIncomplete', text.countIncomplete);
        }

        Object.assign(count, { status: 'PendingApproval', submittedAt: fakeNow() });
        return ok(countDto(count));
      }

      if (count.status !== 'PendingApproval') {
        return problem(409, 'inventory.countNotPending', text.countNotPending);
      }

      if (action === 'reject') {
        const reason = readText(readBody(request), 'reason').trim();
        if (!reason) {
          return validation([fieldError('reason', 'common.required', text.required)]);
        }

        Object.assign(count, {
          status: 'Rejected',
          decidedBy: nameOf(principal),
          decidedAt: fakeNow(),
          rejectionReason: reason,
        });
        return ok(countDto(count));
      }

      for (const line of count.lines) {
        const part = findPart(line.partId);
        const difference = round(
          (line.countedQuantity ?? line.systemQuantity) - line.systemQuantity,
        );
        if (part && difference !== 0) {
          move(
            part,
            'Adjustment',
            difference,
            text.countAdjustment(count.location),
            principal,
            null,
          );
        }
      }

      Object.assign(count, {
        status: 'Approved',
        decidedBy: nameOf(principal),
        decidedAt: fakeNow(),
      });
      return ok(countDto(count));
    })
    .on('GET', INVENTORY_PATHS.suppliers, (request) => {
      const principal = authorize(request, users(), readers);
      return isPrincipal(principal)
        ? ok({ items: [...db().suppliers].sort((a, b) => a.name.localeCompare(b.name)) })
        : principal;
    })
    .on('POST', INVENTORY_PATHS.suppliers, (request) => {
      const principal = authorize(request, users(), managers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const input = readSupplier(readBody(request));
      if (Array.isArray(input)) {
        return validation(input);
      }

      if (
        input.document &&
        db().suppliers.some((supplier) => supplier.document === input.document)
      ) {
        return problem(409, 'inventory.duplicateSupplier', text.duplicateSupplier);
      }

      const supplier: FakeSupplier = { id: fakeId(), isActive: true, ...input };
      db().suppliers.push(supplier);
      return created(supplier);
    })
    .on('PUT', INVENTORY_PATHS.supplier, (request, params) => {
      const principal = authorize(request, users(), managers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const supplier = db().suppliers.find((candidate) => candidate.id === params['id']);
      if (!supplier) {
        return problem(404, 'inventory.supplierNotFound', text.supplierNotFound);
      }

      const input = readSupplier(readBody(request));
      if (Array.isArray(input)) {
        return validation(input);
      }

      Object.assign(supplier, input);
      return ok(supplier);
    })
    .on('GET', INVENTORY_PATHS.suggestions, (request) => {
      const principal = authorize(request, users(), managers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      return ok({
        items: parts()
          .filter((part) => part.isActive && suggestedOf(part) > 0)
          .map((part) => ({
            ...itemDto(part),
            supplierId: supplierFor(part)?.id ?? null,
            forecast30d: info(part).outflow30d,
            suggestedQuantity: suggestedOf(part),
          })),
      });
    })
    .on('GET', INVENTORY_PATHS.orders, (request) => {
      const principal = authorize(request, users(), managers);
      return isPrincipal(principal)
        ? ok({ items: [...db().orders].reverse().map(orderDto) })
        : principal;
    })
    .on('POST', INVENTORY_PATHS.orders, (request) => {
      const principal = authorize(request, users(), managers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      const supplier = db().suppliers.find(
        (candidate) => candidate.id === readText(body, 'supplierId'),
      );
      const raw = Array.isArray(body['lines']) ? (body['lines'] as unknown[]) : [];
      const lines = raw.map((item) => {
        const line =
          typeof item === 'object' && item !== null ? (item as Record<string, unknown>) : {};
        const part = findPart(String(line['partId'] ?? ''));
        const unitCost = line['unitCost'];
        return part && isQuantityFor(part.type, line['quantity']) && typeof unitCost === 'number'
          ? { partId: part.id, quantity: Number(line['quantity']), unitCost }
          : null;
      });
      const valid = lines.filter((line) => line !== null);
      if (!supplier || raw.length === 0 || valid.length !== raw.length) {
        return validation([
          fieldError('lines', 'inventory.invalidOrder', text.invalidPurchaseOrder),
        ]);
      }

      const order: FakePurchaseOrder = {
        id: fakeId(),
        number: 1001 + db().orders.length,
        supplierId: supplier.id,
        status: 'Draft',
        createdAt: fakeNow(),
        sentAt: null,
        lines: valid,
      };
      db().orders.push(order);
      return created(orderDto(order));
    })
    .on('POST', INVENTORY_PATHS.sendOrder, (request, params) => {
      const principal = authorize(request, users(), managers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const order = db().orders.find((candidate) => candidate.id === params['id']);
      if (!order) {
        return problem(404, 'inventory.orderNotFound', text.orderNotFound);
      }

      if (order.status !== 'Draft') {
        return problem(409, 'inventory.orderAlreadySent', text.orderAlreadySent);
      }

      Object.assign(order, { status: 'Queued', sentAt: fakeNow() });
      return accepted(orderDto(order));
    });

  return new FakeFeatureApi('inventory', router, state.scenarios);
}

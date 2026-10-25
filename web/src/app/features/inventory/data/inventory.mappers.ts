import { required } from '../../../core/data/contract-error';
import { Cents, centsFromDecimal } from '../../../shared/util/money';
import {
  CountLineDto,
  InvoiceDraftDto,
  PurchaseOrderDto,
  PurchaseSuggestionDto,
  StockCountDto,
  StockItemDto,
  StockKpisDto,
  StockMovementDto,
  StockOverviewDto,
  SupplierDto,
} from './inventory.contracts';
import {
  CountLine,
  InvoiceDraft,
  PurchaseOrder,
  PurchaseSuggestion,
  StockCount,
  StockItem,
  StockKpis,
  StockMovement,
  StockOverview,
  Supplier,
} from './inventory.models';

function money(value: number | undefined | null): Cents {
  return centsFromDecimal(value ?? 0);
}

export function toStockItem(dto: StockItemDto): StockItem {
  return {
    partId: required(dto.partId, 'partId'),
    code: required(dto.code, 'code'),
    name: required(dto.name, 'name'),
    type: required(dto.type, 'type'),
    unitOfMeasure: required(dto.unitOfMeasure, 'unitOfMeasure'),
    location: dto.location ?? null,
    stockQuantity: dto.stockQuantity ?? 0,
    minimumStock: dto.minimumStock ?? 0,
    outflow30d: dto.outflow30d ?? 0,
    unitCost: money(dto.unitCost),
    lastOutflowAt: dto.lastOutflowAt ?? null,
    barcode: dto.barcode ?? null,
  };
}

export function toStockKpis(dto: StockKpisDto | undefined): StockKpis {
  return {
    belowMinimum: dto?.belowMinimum ?? 0,
    suggestedOrderTotal: money(dto?.suggestedOrderTotal),
    outflowMonth: dto?.outflowMonth ?? 0,
    outflowMonthDeltaPercent: dto?.outflowMonthDeltaPercent ?? null,
    turnoverDays: dto?.turnoverDays ?? null,
    turnoverGoalDays: dto?.turnoverGoalDays ?? 30,
    idleItems: dto?.idleItems ?? 0,
    idleValue: money(dto?.idleValue),
    stockValue: money(dto?.stockValue),
    totalItems: dto?.totalItems ?? 0,
  };
}

export function toStockOverview(dto: StockOverviewDto): StockOverview {
  return {
    kpis: toStockKpis(dto.kpis),
    items: (dto.items ?? []).map(toStockItem),
    page: required(dto.page, 'page'),
    pageSize: required(dto.pageSize, 'pageSize'),
    totalItems: required(dto.totalItems, 'totalItems'),
    locations: [...(dto.locations ?? [])],
  };
}

export function toStockMovement(dto: StockMovementDto): StockMovement {
  return {
    id: required(dto.id, 'id'),
    type: required(dto.type, 'type'),
    quantity: required(dto.quantity, 'quantity'),
    balanceAfter: dto.balanceAfter ?? 0,
    reason: dto.reason ?? '',
    userName: dto.userName ?? '',
    serviceOrderNumber: dto.serviceOrderNumber ?? null,
    occurredAt: required(dto.occurredAt, 'occurredAt'),
  };
}

export function toInvoiceDraft(dto: InvoiceDraftDto): InvoiceDraft {
  return {
    supplierName: required(dto.supplierName, 'supplierName'),
    supplierDocument: dto.supplierDocument ?? null,
    supplierId: dto.supplierId ?? null,
    invoiceNumber: required(dto.invoiceNumber, 'invoiceNumber'),
    issuedAt: required(dto.issuedAt, 'issuedAt'),
    lines: (dto.lines ?? []).map((line) => ({
      description: line.description ?? '',
      barcode: line.barcode ?? null,
      partId: line.partId ?? null,
      quantity: line.quantity ?? 0,
      unitCost: money(line.unitCost),
    })),
  };
}

function toCountLine(dto: CountLineDto): CountLine {
  return {
    partId: required(dto.partId, 'partId'),
    code: required(dto.code, 'code'),
    name: required(dto.name, 'name'),
    type: required(dto.type, 'type'),
    unitOfMeasure: dto.unitOfMeasure ?? 'un',
    systemQuantity: dto.systemQuantity ?? 0,
    countedQuantity: dto.countedQuantity ?? null,
  };
}

export function toStockCount(dto: StockCountDto): StockCount {
  return {
    id: required(dto.id, 'id'),
    location: required(dto.location, 'location'),
    status: required(dto.status, 'status'),
    openedBy: dto.openedBy ?? '',
    openedAt: required(dto.openedAt, 'openedAt'),
    submittedAt: dto.submittedAt ?? null,
    decidedBy: dto.decidedBy ?? null,
    decidedAt: dto.decidedAt ?? null,
    rejectionReason: dto.rejectionReason ?? null,
    lines: (dto.lines ?? []).map(toCountLine),
  };
}

export function toSupplier(dto: SupplierDto): Supplier {
  return {
    id: required(dto.id, 'id'),
    name: required(dto.name, 'name'),
    document: dto.document ?? null,
    email: dto.email ?? null,
    phone: dto.phone ?? null,
    leadTimeDays: dto.leadTimeDays ?? 0,
    isActive: dto.isActive ?? true,
  };
}

export function toPurchaseSuggestion(dto: PurchaseSuggestionDto): PurchaseSuggestion {
  return {
    partId: required(dto.partId, 'partId'),
    code: required(dto.code, 'code'),
    name: required(dto.name, 'name'),
    type: required(dto.type, 'type'),
    unitOfMeasure: dto.unitOfMeasure ?? 'un',
    supplierId: dto.supplierId ?? null,
    stockQuantity: dto.stockQuantity ?? 0,
    minimumStock: dto.minimumStock ?? 0,
    forecast30d: dto.forecast30d ?? 0,
    suggestedQuantity: dto.suggestedQuantity ?? 0,
    unitCost: money(dto.unitCost),
  };
}

export function toPurchaseOrder(dto: PurchaseOrderDto): PurchaseOrder {
  return {
    id: required(dto.id, 'id'),
    number: required(dto.number, 'number'),
    supplierId: required(dto.supplierId, 'supplierId'),
    supplierName: dto.supplierName ?? '',
    status: required(dto.status, 'status'),
    total: money(dto.total),
    createdAt: required(dto.createdAt, 'createdAt'),
    sentAt: dto.sentAt ?? null,
    lines: (dto.lines ?? []).map((line) => ({
      partId: required(line.partId, 'partId'),
      code: line.code ?? '',
      name: line.name ?? '',
      quantity: line.quantity ?? 0,
      unitCost: money(line.unitCost),
    })),
  };
}

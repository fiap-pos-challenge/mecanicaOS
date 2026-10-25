import { Cents } from '../../../shared/util/money';
import { PartType } from '../../catalog/data/catalog.models';
import {
  CountStatusDto,
  MovementTypeDto,
  PurchaseOrderStatusDto,
  StockSituationDto,
} from './inventory.contracts';

export type StockSituation = StockSituationDto;
export type MovementType = MovementTypeDto;
export type CountStatus = CountStatusDto;
export type PurchaseOrderStatus = PurchaseOrderStatusDto;

export const STOCK_SITUATIONS: readonly StockSituation[] = ['Ok', 'Reorder', 'OutOfStock', 'Idle'];

export interface StockItem {
  readonly partId: string;
  readonly code: string;
  readonly name: string;
  readonly type: PartType;
  readonly unitOfMeasure: string;
  readonly location: string | null;
  readonly stockQuantity: number;
  readonly minimumStock: number;
  readonly outflow30d: number;
  readonly unitCost: Cents;
  readonly lastOutflowAt: string | null;
  readonly barcode: string | null;
}

export interface StockKpis {
  readonly belowMinimum: number;
  readonly suggestedOrderTotal: Cents;
  readonly outflowMonth: number;
  readonly outflowMonthDeltaPercent: number | null;
  readonly turnoverDays: number | null;
  readonly turnoverGoalDays: number;
  readonly idleItems: number;
  readonly idleValue: Cents;
  readonly stockValue: Cents;
  readonly totalItems: number;
}

export interface StockQuery {
  readonly page: number;
  readonly pageSize: number;
  readonly term: string;
  readonly situation: StockSituation | null;
  readonly location: string | null;
}

export interface StockOverview {
  readonly kpis: StockKpis;
  readonly items: readonly StockItem[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalItems: number;
  readonly locations: readonly string[];
}

export interface StockMovement {
  readonly id: string;
  readonly type: MovementType;
  readonly quantity: number;
  readonly balanceAfter: number;
  readonly reason: string;
  readonly userName: string;
  readonly serviceOrderNumber: number | null;
  readonly occurredAt: string;
}

export interface PartMovements {
  readonly item: StockItem;
  readonly movements: readonly StockMovement[];
}

export interface StockEntryInput {
  readonly partId: string;
  readonly quantity: number;
  readonly unitCost: Cents;
  readonly reason: string;
  readonly supplierId: string | null;
  readonly invoiceNumber: string | null;
}

export interface StockExitInput {
  readonly partId: string;
  readonly quantity: number;
  readonly reason: string;
  readonly serviceOrderNumber: number | null;
}

export interface InvoiceLine {
  readonly description: string;
  readonly barcode: string | null;
  readonly partId: string | null;
  readonly quantity: number;
  readonly unitCost: Cents;
}

export interface InvoiceDraft {
  readonly supplierName: string;
  readonly supplierDocument: string | null;
  readonly supplierId: string | null;
  readonly invoiceNumber: string;
  readonly issuedAt: string;
  readonly lines: readonly InvoiceLine[];
}

export interface InvoiceConfirmInput {
  readonly supplierId: string | null;
  readonly invoiceNumber: string;
  readonly lines: readonly {
    readonly partId: string;
    readonly quantity: number;
    readonly unitCost: Cents;
  }[];
}

export interface CountLine {
  readonly partId: string;
  readonly code: string;
  readonly name: string;
  readonly type: PartType;
  readonly unitOfMeasure: string;
  readonly systemQuantity: number;
  readonly countedQuantity: number | null;
}

export interface StockCount {
  readonly id: string;
  readonly location: string;
  readonly status: CountStatus;
  readonly openedBy: string;
  readonly openedAt: string;
  readonly submittedAt: string | null;
  readonly decidedBy: string | null;
  readonly decidedAt: string | null;
  readonly rejectionReason: string | null;
  readonly lines: readonly CountLine[];
}

export interface Supplier {
  readonly id: string;
  readonly name: string;
  readonly document: string | null;
  readonly email: string | null;
  readonly phone: string | null;
  readonly leadTimeDays: number;
  readonly isActive: boolean;
}

export type SupplierInput = Omit<Supplier, 'id' | 'isActive'>;

export interface PurchaseSuggestion {
  readonly partId: string;
  readonly code: string;
  readonly name: string;
  readonly type: PartType;
  readonly unitOfMeasure: string;
  readonly supplierId: string | null;
  readonly stockQuantity: number;
  readonly minimumStock: number;
  readonly forecast30d: number;
  readonly suggestedQuantity: number;
  readonly unitCost: Cents;
}

export interface PurchaseOrderLine {
  readonly partId: string;
  readonly code: string;
  readonly name: string;
  readonly quantity: number;
  readonly unitCost: Cents;
}

export interface PurchaseOrder {
  readonly id: string;
  readonly number: number;
  readonly supplierId: string;
  readonly supplierName: string;
  readonly status: PurchaseOrderStatus;
  readonly total: Cents;
  readonly createdAt: string;
  readonly sentAt: string | null;
  readonly lines: readonly PurchaseOrderLine[];
}

export interface PurchaseOrderInput {
  readonly supplierId: string;
  readonly lines: readonly {
    readonly partId: string;
    readonly quantity: number;
    readonly unitCost: Cents;
  }[];
}

import { PartTypeDto } from '../../catalog/data/catalog.contracts';

export type StockSituationDto = 'Ok' | 'Reorder' | 'OutOfStock' | 'Idle';
export type MovementTypeDto = 'In' | 'Out' | 'Adjustment';
export type CountStatusDto = 'Open' | 'PendingApproval' | 'Approved' | 'Rejected';
export type PurchaseOrderStatusDto = 'Draft' | 'Queued' | 'Sent';

export interface StockItemDto {
  readonly partId?: string;
  readonly code?: string;
  readonly name?: string;
  readonly type?: PartTypeDto;
  readonly unitOfMeasure?: string;
  readonly location?: string | null;
  readonly stockQuantity?: number;
  readonly minimumStock?: number;
  readonly outflow30d?: number;
  readonly unitCost?: number;
  readonly lastOutflowAt?: string | null;
  readonly barcode?: string | null;
}

export interface StockKpisDto {
  readonly belowMinimum?: number;
  readonly suggestedOrderTotal?: number;
  readonly outflowMonth?: number;
  readonly outflowMonthDeltaPercent?: number | null;
  readonly turnoverDays?: number | null;
  readonly turnoverGoalDays?: number;
  readonly idleItems?: number;
  readonly idleValue?: number;
  readonly stockValue?: number;
  readonly totalItems?: number;
}

export interface StockOverviewDto {
  readonly kpis?: StockKpisDto;
  readonly items?: readonly StockItemDto[] | null;
  readonly page?: number;
  readonly pageSize?: number;
  readonly totalItems?: number;
  readonly locations?: readonly string[] | null;
}

export interface StockMovementDto {
  readonly id?: string;
  readonly type?: MovementTypeDto;
  readonly quantity?: number;
  readonly balanceAfter?: number;
  readonly reason?: string;
  readonly userName?: string;
  readonly serviceOrderNumber?: number | null;
  readonly occurredAt?: string;
}

export interface StockMovementListDto {
  readonly item?: StockItemDto;
  readonly items?: readonly StockMovementDto[] | null;
}

export interface StockEntryInputDto {
  readonly partId: string;
  readonly quantity: number;
  readonly unitCost: number;
  readonly reason: string;
  readonly supplierId: string | null;
  readonly invoiceNumber: string | null;
}

export interface StockExitInputDto {
  readonly partId: string;
  readonly quantity: number;
  readonly reason: string;
  readonly serviceOrderNumber: number | null;
}

export interface InvoiceLineDto {
  readonly description?: string;
  readonly barcode?: string | null;
  readonly partId?: string | null;
  readonly quantity?: number;
  readonly unitCost?: number;
}

export interface InvoiceDraftDto {
  readonly supplierName?: string;
  readonly supplierDocument?: string | null;
  readonly supplierId?: string | null;
  readonly invoiceNumber?: string;
  readonly issuedAt?: string;
  readonly lines?: readonly InvoiceLineDto[] | null;
}

export interface InvoiceConfirmInputDto {
  readonly supplierId: string | null;
  readonly invoiceNumber: string;
  readonly lines: readonly {
    readonly partId: string;
    readonly quantity: number;
    readonly unitCost: number;
  }[];
}

export interface CountLineDto {
  readonly partId?: string;
  readonly code?: string;
  readonly name?: string;
  readonly type?: PartTypeDto;
  readonly unitOfMeasure?: string;
  readonly systemQuantity?: number;
  readonly countedQuantity?: number | null;
}

export interface StockCountDto {
  readonly id?: string;
  readonly location?: string;
  readonly status?: CountStatusDto;
  readonly openedBy?: string;
  readonly openedAt?: string;
  readonly submittedAt?: string | null;
  readonly decidedBy?: string | null;
  readonly decidedAt?: string | null;
  readonly rejectionReason?: string | null;
  readonly lines?: readonly CountLineDto[] | null;
}

export interface StockCountListDto {
  readonly items?: readonly StockCountDto[] | null;
}

export interface SupplierDto {
  readonly id?: string;
  readonly name?: string;
  readonly document?: string | null;
  readonly email?: string | null;
  readonly phone?: string | null;
  readonly leadTimeDays?: number;
  readonly isActive?: boolean;
}

export interface SupplierListDto {
  readonly items?: readonly SupplierDto[] | null;
}

export interface SupplierInputDto {
  readonly name: string;
  readonly document: string | null;
  readonly email: string | null;
  readonly phone: string | null;
  readonly leadTimeDays: number;
}

export interface PurchaseSuggestionDto {
  readonly partId?: string;
  readonly code?: string;
  readonly name?: string;
  readonly type?: PartTypeDto;
  readonly unitOfMeasure?: string;
  readonly supplierId?: string | null;
  readonly stockQuantity?: number;
  readonly minimumStock?: number;
  readonly forecast30d?: number;
  readonly suggestedQuantity?: number;
  readonly unitCost?: number;
}

export interface PurchaseSuggestionListDto {
  readonly items?: readonly PurchaseSuggestionDto[] | null;
}

export interface PurchaseOrderLineDto {
  readonly partId?: string;
  readonly code?: string;
  readonly name?: string;
  readonly quantity?: number;
  readonly unitCost?: number;
}

export interface PurchaseOrderDto {
  readonly id?: string;
  readonly number?: number;
  readonly supplierId?: string;
  readonly supplierName?: string;
  readonly status?: PurchaseOrderStatusDto;
  readonly total?: number;
  readonly createdAt?: string;
  readonly sentAt?: string | null;
  readonly lines?: readonly PurchaseOrderLineDto[] | null;
}

export interface PurchaseOrderListDto {
  readonly items?: readonly PurchaseOrderDto[] | null;
}

export interface PurchaseOrderInputDto {
  readonly supplierId: string;
  readonly lines: readonly {
    readonly partId: string;
    readonly quantity: number;
    readonly unitCost: number;
  }[];
}

interface Op<U extends string> {
  readonly url: U;
}

export interface StockOverviewData extends Op<'/api/admin/inventory/stock'> {
  readonly query: {
    readonly page: number;
    readonly pageSize: number;
    readonly term?: string;
    readonly situation?: StockSituationDto;
    readonly location?: string;
  };
}

export interface MovementsData extends Op<'/api/admin/inventory/stock/{partId}/movements'> {
  readonly path: { readonly partId: string };
}

export interface EntryData extends Op<'/api/admin/inventory/entries'> {
  readonly body: StockEntryInputDto;
}

export interface ExitData extends Op<'/api/admin/inventory/exits'> {
  readonly body: StockExitInputDto;
}

export interface BarcodeData extends Op<'/api/admin/inventory/barcodes/lookup'> {
  readonly body: { readonly barcode: string };
}

export interface InvoiceReadData extends Op<'/api/admin/inventory/invoices/read'> {
  readonly body: {
    readonly fileName: string;
    readonly contentType: string;
    readonly content: string;
  };
}

export interface InvoiceConfirmData extends Op<'/api/admin/inventory/invoices/confirm'> {
  readonly body: InvoiceConfirmInputDto;
}

export interface CountsData extends Op<'/api/admin/inventory/counts'> {
  readonly body?: { readonly location: string };
}

export interface CountData extends Op<'/api/admin/inventory/counts/{id}'> {
  readonly path: { readonly id: string };
}

export interface CountLinesData extends Op<'/api/admin/inventory/counts/{id}/lines'> {
  readonly path: { readonly id: string };
  readonly body: {
    readonly lines: readonly { readonly partId: string; readonly countedQuantity: number | null }[];
  };
}

export interface CountActionData extends Op<'/api/admin/inventory/counts/{id}/{action}'> {
  readonly path: { readonly id: string; readonly action: 'submit' | 'approve' | 'reject' };
  readonly body?: { readonly reason: string };
}

export interface SuppliersData extends Op<'/api/admin/inventory/suppliers'> {
  readonly body?: SupplierInputDto;
}

export interface SupplierData extends Op<'/api/admin/inventory/suppliers/{id}'> {
  readonly path: { readonly id: string };
  readonly body: SupplierInputDto;
}

export type SuggestionsData = Op<'/api/admin/inventory/purchase-orders/suggestions'>;

export interface PurchaseOrdersData extends Op<'/api/admin/inventory/purchase-orders'> {
  readonly body?: PurchaseOrderInputDto;
}

export interface SendPurchaseOrderData extends Op<'/api/admin/inventory/purchase-orders/{id}/send'> {
  readonly path: { readonly id: string };
}

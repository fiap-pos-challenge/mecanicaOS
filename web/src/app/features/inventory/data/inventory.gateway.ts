import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import { centsToDecimal } from '../../../shared/util/money';
import {
  BarcodeData,
  CountActionData,
  CountData,
  CountLinesData,
  CountsData,
  EntryData,
  ExitData,
  InvoiceConfirmData,
  InvoiceDraftDto,
  InvoiceReadData,
  MovementsData,
  PurchaseOrderDto,
  PurchaseOrderListDto,
  PurchaseOrdersData,
  PurchaseSuggestionListDto,
  SendPurchaseOrderData,
  StockCountDto,
  StockCountListDto,
  StockItemDto,
  StockMovementDto,
  StockMovementListDto,
  StockOverviewData,
  StockOverviewDto,
  SuggestionsData,
  SupplierData,
  SupplierDto,
  SupplierListDto,
  SuppliersData,
} from './inventory.contracts';
import {
  toInvoiceDraft,
  toPurchaseOrder,
  toPurchaseSuggestion,
  toStockCount,
  toStockItem,
  toStockMovement,
  toStockOverview,
  toSupplier,
} from './inventory.mappers';
import {
  InvoiceConfirmInput,
  InvoiceDraft,
  PartMovements,
  PurchaseOrder,
  PurchaseOrderInput,
  PurchaseSuggestion,
  StockCount,
  StockEntryInput,
  StockExitInput,
  StockItem,
  StockMovement,
  StockOverview,
  StockQuery,
  Supplier,
  SupplierInput,
} from './inventory.models';

export interface InvoiceFile {
  readonly fileName: string;
  readonly contentType: string;
  readonly content: string;
}

export abstract class InventoryGateway {
  abstract overview(query: StockQuery): Observable<StockOverview>;
  abstract movements(partId: string): Observable<PartMovements>;
  abstract registerEntry(input: StockEntryInput): Observable<StockMovement>;
  abstract registerExit(input: StockExitInput): Observable<StockMovement>;
  abstract findByBarcode(barcode: string): Observable<StockItem>;
  abstract readInvoice(file: InvoiceFile): Observable<InvoiceDraft>;
  abstract confirmInvoice(input: InvoiceConfirmInput): Observable<void>;
  abstract counts(): Observable<readonly StockCount[]>;
  abstract count(id: string): Observable<StockCount>;
  abstract openCount(location: string): Observable<StockCount>;
  abstract saveCount(
    id: string,
    lines: readonly { readonly partId: string; readonly countedQuantity: number | null }[],
  ): Observable<StockCount>;
  abstract submitCount(id: string): Observable<StockCount>;
  abstract approveCount(id: string): Observable<StockCount>;
  abstract rejectCount(id: string, reason: string): Observable<StockCount>;
  abstract suppliers(): Observable<readonly Supplier[]>;
  abstract createSupplier(input: SupplierInput): Observable<Supplier>;
  abstract updateSupplier(id: string, input: SupplierInput): Observable<Supplier>;
  abstract suggestions(): Observable<readonly PurchaseSuggestion[]>;
  abstract purchaseOrders(): Observable<readonly PurchaseOrder[]>;
  abstract createPurchaseOrder(input: PurchaseOrderInput): Observable<PurchaseOrder>;
  abstract sendPurchaseOrder(id: string): Observable<PurchaseOrder>;
}

@Injectable({ providedIn: 'root' })
export class HttpInventoryGateway extends InventoryGateway {
  private readonly api = inject(ApiClient);

  overview(query: StockQuery): Observable<StockOverview> {
    return this.api
      .send<StockOverviewData, StockOverviewDto>('GET', '/api/admin/inventory/stock', {
        query: {
          page: query.page,
          pageSize: query.pageSize,
          ...(query.term.trim() ? { term: query.term.trim() } : {}),
          ...(query.situation ? { situation: query.situation } : {}),
          ...(query.location ? { location: query.location } : {}),
        },
      })
      .pipe(map(toStockOverview));
  }

  movements(partId: string): Observable<PartMovements> {
    return this.api
      .send<MovementsData, StockMovementListDto>(
        'GET',
        '/api/admin/inventory/stock/{partId}/movements',
        { path: { partId } },
      )
      .pipe(
        map((dto) => ({
          item: toStockItem(dto.item ?? {}),
          movements: (dto.items ?? []).map(toStockMovement),
        })),
      );
  }

  registerEntry(input: StockEntryInput): Observable<StockMovement> {
    return this.api
      .send<EntryData, StockMovementDto>('POST', '/api/admin/inventory/entries', {
        body: { ...input, unitCost: centsToDecimal(input.unitCost) },
      })
      .pipe(map(toStockMovement));
  }

  registerExit(input: StockExitInput): Observable<StockMovement> {
    return this.api
      .send<ExitData, StockMovementDto>('POST', '/api/admin/inventory/exits', { body: input })
      .pipe(map(toStockMovement));
  }

  findByBarcode(barcode: string): Observable<StockItem> {
    return this.api
      .send<BarcodeData, StockItemDto>('POST', '/api/admin/inventory/barcodes/lookup', {
        body: { barcode },
      })
      .pipe(map(toStockItem));
  }

  readInvoice(file: InvoiceFile): Observable<InvoiceDraft> {
    return this.api
      .send<InvoiceReadData, InvoiceDraftDto>('POST', '/api/admin/inventory/invoices/read', {
        body: file,
      })
      .pipe(map(toInvoiceDraft));
  }

  confirmInvoice(input: InvoiceConfirmInput): Observable<void> {
    return this.api
      .send<InvoiceConfirmData, unknown>('POST', '/api/admin/inventory/invoices/confirm', {
        body: {
          ...input,
          lines: input.lines.map((line) => ({ ...line, unitCost: centsToDecimal(line.unitCost) })),
        },
      })
      .pipe(map(() => undefined));
  }

  counts(): Observable<readonly StockCount[]> {
    return this.api
      .send<CountsData, StockCountListDto>('GET', '/api/admin/inventory/counts')
      .pipe(map((dto) => (dto.items ?? []).map(toStockCount)));
  }

  count(id: string): Observable<StockCount> {
    return this.api
      .send<CountData, StockCountDto>('GET', '/api/admin/inventory/counts/{id}', { path: { id } })
      .pipe(map(toStockCount));
  }

  openCount(location: string): Observable<StockCount> {
    return this.api
      .send<CountsData, StockCountDto>('POST', '/api/admin/inventory/counts', {
        body: { location },
      })
      .pipe(map(toStockCount));
  }

  saveCount(
    id: string,
    lines: readonly { readonly partId: string; readonly countedQuantity: number | null }[],
  ): Observable<StockCount> {
    return this.api
      .send<CountLinesData, StockCountDto>('PUT', '/api/admin/inventory/counts/{id}/lines', {
        path: { id },
        body: { lines },
      })
      .pipe(map(toStockCount));
  }

  submitCount(id: string): Observable<StockCount> {
    return this.countAction(id, 'submit');
  }

  approveCount(id: string): Observable<StockCount> {
    return this.countAction(id, 'approve');
  }

  rejectCount(id: string, reason: string): Observable<StockCount> {
    return this.countAction(id, 'reject', reason);
  }

  suppliers(): Observable<readonly Supplier[]> {
    return this.api
      .send<SuppliersData, SupplierListDto>('GET', '/api/admin/inventory/suppliers')
      .pipe(map((dto) => (dto.items ?? []).map(toSupplier)));
  }

  createSupplier(input: SupplierInput): Observable<Supplier> {
    return this.api
      .send<SuppliersData, SupplierDto>('POST', '/api/admin/inventory/suppliers', { body: input })
      .pipe(map(toSupplier));
  }

  updateSupplier(id: string, input: SupplierInput): Observable<Supplier> {
    return this.api
      .send<SupplierData, SupplierDto>('PUT', '/api/admin/inventory/suppliers/{id}', {
        path: { id },
        body: input,
      })
      .pipe(map(toSupplier));
  }

  suggestions(): Observable<readonly PurchaseSuggestion[]> {
    return this.api
      .send<SuggestionsData, PurchaseSuggestionListDto>(
        'GET',
        '/api/admin/inventory/purchase-orders/suggestions',
      )
      .pipe(map((dto) => (dto.items ?? []).map(toPurchaseSuggestion)));
  }

  purchaseOrders(): Observable<readonly PurchaseOrder[]> {
    return this.api
      .send<PurchaseOrdersData, PurchaseOrderListDto>('GET', '/api/admin/inventory/purchase-orders')
      .pipe(map((dto) => (dto.items ?? []).map(toPurchaseOrder)));
  }

  createPurchaseOrder(input: PurchaseOrderInput): Observable<PurchaseOrder> {
    return this.api
      .send<PurchaseOrdersData, PurchaseOrderDto>('POST', '/api/admin/inventory/purchase-orders', {
        body: {
          supplierId: input.supplierId,
          lines: input.lines.map((line) => ({ ...line, unitCost: centsToDecimal(line.unitCost) })),
        },
      })
      .pipe(map(toPurchaseOrder));
  }

  sendPurchaseOrder(id: string): Observable<PurchaseOrder> {
    return this.api
      .send<SendPurchaseOrderData, PurchaseOrderDto>(
        'POST',
        '/api/admin/inventory/purchase-orders/{id}/send',
        { path: { id } },
      )
      .pipe(map(toPurchaseOrder));
  }

  private countAction(
    id: string,
    action: 'submit' | 'approve' | 'reject',
    reason?: string,
  ): Observable<StockCount> {
    return this.api
      .send<CountActionData, StockCountDto>('POST', '/api/admin/inventory/counts/{id}/{action}', {
        path: { id, action },
        ...(reason === undefined ? {} : { body: { reason } }),
      })
      .pipe(map(toStockCount));
  }
}

export function provideInventoryData(): Provider {
  return provideGateway(InventoryGateway, 'inventory', { http: HttpInventoryGateway });
}

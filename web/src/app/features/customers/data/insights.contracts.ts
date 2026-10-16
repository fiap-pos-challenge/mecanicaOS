export type CustomerSituation = 'Active' | 'OpenServiceOrder' | 'NoReturn';

export interface CustomerSummaryDto {
  readonly customerId?: string;
  readonly vehicleCount?: number;
  readonly lastVisitAt?: string | null;
  readonly totalSpent?: number;
  readonly averageTicket?: number;
  readonly serviceOrderCount?: number;
  readonly openServiceOrders?: number;
  readonly situation?: CustomerSituation;
  readonly fleet?: boolean;
}

export interface CustomerSummaryListDto {
  readonly items?: readonly CustomerSummaryDto[] | null;
}

export interface ServiceHistoryEntryDto {
  readonly number?: number;
  readonly description?: string | null;
  readonly total?: number;
  readonly status?: 'Delivered' | 'InProgress';
  readonly openedAt?: string;
  readonly closedAt?: string | null;
  readonly paymentMethod?: string | null;
}

export interface ServiceHistoryDto {
  readonly items?: readonly ServiceHistoryEntryDto[] | null;
}

export interface CustomerSummariesData {
  readonly url: '/api/admin/customers/summaries';
  readonly body: { readonly customerIds: readonly string[] };
}

export interface ServiceHistoryData {
  readonly url: '/api/admin/customers/{id}/service-history';
  readonly path: { readonly id: string };
}

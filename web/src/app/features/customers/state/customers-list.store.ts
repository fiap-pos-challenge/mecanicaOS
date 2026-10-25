import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { ApiError, isApiError, isFeaturePending, toApiError } from '../../../core/http/api-error';
import { DocumentKind } from '../../../shared/util/document';
import { CustomerInsightsGateway, MAX_SUMMARY_IDS } from '../data/customer-insights.gateway';
import { Customer, CustomerQuery, CustomerSort, SortDirection } from '../data/customer.models';
import { CustomersGateway } from '../data/customers.gateway';
import { CustomerSituation } from '../data/insights.contracts';
import { CustomerSummary } from '../data/insights.models';

export const LIST_PAGE_SIZE = 20;
export const SCAN_PAGE_SIZE = 100;

export interface CustomerRow {
  readonly customer: Customer;
  readonly summary: CustomerSummary | null;
}

export type CustomerListSort = 'nameAsc' | 'nameDesc' | 'newest' | 'oldest';

export interface CustomerListFilters {
  readonly term: string;
  readonly documentType: DocumentKind | null;
  readonly situation: Exclude<CustomerSituation, 'NoReturn'> | null;
  readonly fleet: boolean;
  readonly noReturn: boolean;
  readonly sort: CustomerListSort;
}

interface CustomersListState {
  readonly filters: CustomerListFilters;
  readonly page: number;
  readonly rows: readonly CustomerRow[];
  readonly totalItems: number;
  readonly loading: boolean;
  readonly error: ApiError | null;
  readonly truncated: boolean;
  readonly summariesPending: boolean;
}

export const DEFAULT_FILTERS: CustomerListFilters = {
  term: '',
  documentType: null,
  situation: null,
  fleet: false,
  noReturn: false,
  sort: 'nameAsc',
};

const sortMap: Readonly<
  Record<CustomerListSort, { sort: CustomerSort; direction: SortDirection }>
> = {
  nameAsc: { sort: 'Name', direction: 'Asc' },
  nameDesc: { sort: 'Name', direction: 'Desc' },
  newest: { sort: 'CreatedAt', direction: 'Desc' },
  oldest: { sort: 'CreatedAt', direction: 'Asc' },
};

export function needsSummaryFilter(filters: CustomerListFilters): boolean {
  return filters.situation !== null || filters.fleet || filters.noReturn;
}

export function matchesSummary(
  filters: CustomerListFilters,
  summary: CustomerSummary | null,
): boolean {
  if (!summary) {
    return false;
  }

  return (
    (filters.situation === null || summary.situation === filters.situation) &&
    (!filters.fleet || summary.fleet) &&
    (!filters.noReturn || summary.situation === 'NoReturn')
  );
}

export function toQuery(
  filters: CustomerListFilters,
  page: number,
  pageSize: number,
): CustomerQuery {
  return {
    term: filters.term,
    documentType: filters.documentType,
    page,
    pageSize,
    ...sortMap[filters.sort],
  };
}

export const CustomersListStore = signalStore(
  withState<CustomersListState>({
    filters: DEFAULT_FILTERS,
    page: 1,
    rows: [],
    totalItems: 0,
    loading: false,
    error: null,
    truncated: false,
    summariesPending: false,
  }),
  withComputed(({ filters }) => ({
    hasFilters: computed(() => {
      const current = filters();
      return (
        current.term.trim() !== '' ||
        current.documentType !== null ||
        needsSummaryFilter(current) ||
        current.sort !== DEFAULT_FILTERS.sort
      );
    }),
  })),
  withMethods((store) => {
    const customers = inject(CustomersGateway);
    const insights = inject(CustomerInsightsGateway);
    let request = 0;

    const withSummaries = async (items: readonly Customer[]): Promise<CustomerRow[]> => {
      patchState(store, { summariesPending: false });
      const summaries = await firstValueFrom(
        insights.summaries(items.map((item) => item.id)),
      ).catch((error: unknown) => {
        patchState(store, { summariesPending: isFeaturePending(error) });
        return new Map<string, CustomerSummary>();
      });
      return items.map((customer) => ({ customer, summary: summaries.get(customer.id) ?? null }));
    };

    const loadPage = async (
      filters: CustomerListFilters,
      page: number,
    ): Promise<Pick<CustomersListState, 'rows' | 'totalItems' | 'truncated'>> => {
      const result = await firstValueFrom(customers.query(toQuery(filters, page, LIST_PAGE_SIZE)));
      return {
        rows: await withSummaries(result.items),
        totalItems: result.totalItems,
        truncated: false,
      };
    };

    const loadFiltered = async (
      filters: CustomerListFilters,
      page: number,
    ): Promise<Pick<CustomersListState, 'rows' | 'totalItems' | 'truncated'>> => {
      const all: Customer[] = [];
      let total = 0;
      for (let scan = 1; all.length < MAX_SUMMARY_IDS; scan++) {
        const result = await firstValueFrom(
          customers.query(toQuery(filters, scan, SCAN_PAGE_SIZE)),
        );
        all.push(...result.items);
        total = result.totalItems;
        if (result.items.length < SCAN_PAGE_SIZE || all.length >= total) {
          break;
        }
      }

      const matching = (await withSummaries(all)).filter((row) =>
        matchesSummary(filters, row.summary),
      );
      return {
        rows: matching.slice((page - 1) * LIST_PAGE_SIZE, page * LIST_PAGE_SIZE),
        totalItems: matching.length,
        truncated: total > all.length,
      };
    };

    const load = async (): Promise<void> => {
      const current = ++request;
      const filters = store.filters();
      patchState(store, { loading: true, error: null });
      try {
        const result = needsSummaryFilter(filters)
          ? await loadFiltered(filters, store.page())
          : await loadPage(filters, store.page());
        if (current === request) {
          patchState(store, { ...result, loading: false });
        }
      } catch (error) {
        if (current === request) {
          patchState(store, {
            loading: false,
            rows: [],
            totalItems: 0,
            error: isApiError(error) ? error : toApiError(error, null),
          });
        }
      }
    };

    return {
      load,
      update(changes: Partial<CustomerListFilters>): Promise<void> {
        patchState(store, { filters: { ...store.filters(), ...changes }, page: 1 });
        return load();
      },
      goTo(page: number): Promise<void> {
        patchState(store, { page });
        return load();
      },
      clear(): Promise<void> {
        patchState(store, { filters: DEFAULT_FILTERS, page: 1 });
        return load();
      },
    };
  }),
);

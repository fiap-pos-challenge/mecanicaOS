export interface PageRequest {
  readonly page: number;
  readonly pageSize: number;
}

export interface Page<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalItems: number;
}

export const DEFAULT_PAGE_SIZE = 20;
export const MAX_PAGE_SIZE = 100;

export function pageRequest(page = 1, pageSize = DEFAULT_PAGE_SIZE): PageRequest {
  return {
    page: Math.max(1, Math.trunc(page)),
    pageSize: Math.min(MAX_PAGE_SIZE, Math.max(1, Math.trunc(pageSize))),
  };
}

export function totalPages(page: Pick<Page<unknown>, 'pageSize' | 'totalItems'>): number {
  return Math.max(1, Math.ceil(page.totalItems / page.pageSize));
}

export function mapPage<T, R>(page: Page<T>, map: (item: T) => R): Page<R> {
  return { ...page, items: page.items.map(map) };
}

import { HttpContext } from '@angular/common/http';
import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import { Page } from '../../../core/data/page';
import {
  CatalogActivationData,
  CatalogCollectionUrl,
  CatalogCreateData,
  CatalogItemData,
  CatalogKind,
  CatalogListData,
  CatalogPageDto,
  PriceHistoryData,
  PriceHistoryDto,
} from './catalog.contracts';
import { toCatalogInputDto, toCatalogItem, toCatalogPage, toPriceChange } from './catalog.mappers';
import { CatalogInputMap, CatalogItemMap, CatalogQuery, PriceChange } from './catalog.models';

export abstract class CatalogGateway {
  abstract list<K extends CatalogKind>(
    kind: K,
    query: CatalogQuery,
  ): Observable<Page<CatalogItemMap[K]>>;
  abstract get<K extends CatalogKind>(kind: K, id: string): Observable<CatalogItemMap[K]>;
  abstract create<K extends CatalogKind>(
    kind: K,
    input: CatalogInputMap[K],
  ): Observable<CatalogItemMap[K]>;
  abstract update<K extends CatalogKind>(
    kind: K,
    id: string,
    input: CatalogInputMap[K],
  ): Observable<CatalogItemMap[K]>;
  abstract setActive(kind: CatalogKind, id: string, active: boolean): Observable<void>;
  abstract priceHistory(kind: CatalogKind, id: string): Observable<readonly PriceChange[]>;
}

function collectionOf(kind: CatalogKind): CatalogCollectionUrl {
  return `/api/admin/${kind}`;
}

@Injectable({ providedIn: 'root' })
export class HttpCatalogGateway extends CatalogGateway {
  private readonly api = inject(ApiClient);

  protected context(): HttpContext {
    return new HttpContext();
  }

  list<K extends CatalogKind>(kind: K, query: CatalogQuery): Observable<Page<CatalogItemMap[K]>> {
    return this.api
      .send<CatalogListData, CatalogPageDto<unknown>>('GET', collectionOf(kind), {
        query: {
          page: query.page,
          pageSize: query.pageSize,
          ...(query.term.trim() ? { term: query.term.trim() } : {}),
          ...(query.status ? { status: query.status } : { includeInactive: true }),
        },
        context: this.context(),
      })
      .pipe(map((dto) => toCatalogPage(kind, dto)));
  }

  get<K extends CatalogKind>(kind: K, id: string): Observable<CatalogItemMap[K]> {
    return this.api
      .send<CatalogItemData, unknown>('GET', `${collectionOf(kind)}/{id}` as const, {
        path: { id },
        context: this.context(),
      })
      .pipe(map((dto) => toCatalogItem(kind, dto)));
  }

  create<K extends CatalogKind>(kind: K, input: CatalogInputMap[K]): Observable<CatalogItemMap[K]> {
    return this.api
      .send<CatalogCreateData, unknown>('POST', collectionOf(kind), {
        body: toCatalogInputDto(kind, input),
        context: this.context(),
      })
      .pipe(map((dto) => toCatalogItem(kind, dto)));
  }

  update<K extends CatalogKind>(
    kind: K,
    id: string,
    input: CatalogInputMap[K],
  ): Observable<CatalogItemMap[K]> {
    return this.api
      .send<CatalogItemData, unknown>('PUT', `${collectionOf(kind)}/{id}` as const, {
        path: { id },
        body: toCatalogInputDto(kind, input),
        context: this.context(),
      })
      .pipe(map((dto) => toCatalogItem(kind, dto)));
  }

  setActive(kind: CatalogKind, id: string, active: boolean): Observable<void> {
    return this.api
      .send<CatalogActivationData, unknown>(
        'POST',
        `${collectionOf(kind)}/{id}/{action}` as const,
        {
          path: { id, action: active ? 'reactivate' : 'deactivate' },
          context: this.context(),
        },
      )
      .pipe(map(() => undefined));
  }

  priceHistory(kind: CatalogKind, id: string): Observable<readonly PriceChange[]> {
    return this.api
      .send<PriceHistoryData, PriceHistoryDto>(
        'GET',
        `${collectionOf(kind)}/{id}/price-history` as const,
        { path: { id }, context: this.context() },
      )
      .pipe(map((dto) => (dto.items ?? []).map(toPriceChange)));
  }
}

export function provideCatalogData(): Provider {
  return provideGateway(CatalogGateway, 'catalog', {
    http: HttpCatalogGateway,
  });
}

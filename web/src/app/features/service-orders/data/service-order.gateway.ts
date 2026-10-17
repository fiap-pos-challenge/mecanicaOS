import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import {
  ServiceOrderCardDto,
  ServiceOrderDetailDto,
  ServiceOrderItemData,
  ServiceOrderListData,
  ServiceOrderOpenData,
  ServiceOrderTransitionData,
} from './service-order.contracts';
import { toServiceOrderCard, toServiceOrderDetail } from './service-order.mappers';
import {
  OpenOrderInput,
  ServiceOrderCard,
  ServiceOrderDetail,
  TransitionInput,
} from './service-order.models';

export const BOARD_PAGE_SIZE = 100;

export abstract class ServiceOrderGateway {
  abstract board(): Observable<readonly ServiceOrderCard[]>;
  abstract get(id: string): Observable<ServiceOrderDetail>;
  abstract open(input: OpenOrderInput): Observable<ServiceOrderDetail>;
  abstract transition(id: string, input: TransitionInput): Observable<ServiceOrderDetail>;
  abstract startTime(id: string): Observable<ServiceOrderDetail>;
  abstract stopTime(id: string): Observable<ServiceOrderDetail>;
}

@Injectable({ providedIn: 'root' })
export class HttpServiceOrderGateway extends ServiceOrderGateway {
  private readonly api = inject(ApiClient);

  board(): Observable<readonly ServiceOrderCard[]> {
    return this.api
      .send<ServiceOrderListData, { items?: readonly ServiceOrderCardDto[] }>(
        'GET',
        '/api/admin/service-orders',
        { query: { page: 1, pageSize: BOARD_PAGE_SIZE } },
      )
      .pipe(map((dto) => (dto.items ?? []).map(toServiceOrderCard)));
  }

  get(id: string): Observable<ServiceOrderDetail> {
    return this.item('GET', '/api/admin/service-orders/{id}', id);
  }

  open(input: OpenOrderInput): Observable<ServiceOrderDetail> {
    return this.api
      .send<ServiceOrderOpenData, ServiceOrderDetailDto>('POST', '/api/admin/service-orders', {
        body: {
          customerId: input.customerId,
          vehicleId: input.vehicleId,
          problemDescription: input.problemDescription.trim(),
          promisedAt: input.promisedAt,
          mileage: input.mileage,
        },
      })
      .pipe(map(toServiceOrderDetail));
  }

  transition(id: string, input: TransitionInput): Observable<ServiceOrderDetail> {
    return this.api
      .send<ServiceOrderTransitionData, ServiceOrderDetailDto>(
        'POST',
        '/api/admin/service-orders/{id}/transitions',
        { path: { id }, body: { to: input.to, version: input.version, reason: input.reason } },
      )
      .pipe(map(toServiceOrderDetail));
  }

  startTime(id: string): Observable<ServiceOrderDetail> {
    return this.item('POST', '/api/admin/service-orders/{id}/time-entries/start', id);
  }

  stopTime(id: string): Observable<ServiceOrderDetail> {
    return this.item('POST', '/api/admin/service-orders/{id}/time-entries/stop', id);
  }

  private item(
    method: 'GET' | 'POST',
    url: ServiceOrderItemData['url'],
    id: string,
  ): Observable<ServiceOrderDetail> {
    return this.api
      .send<ServiceOrderItemData, ServiceOrderDetailDto>(method, url, { path: { id } })
      .pipe(map(toServiceOrderDetail));
  }
}

export function provideServiceOrderData(): Provider {
  return provideGateway(ServiceOrderGateway, 'serviceOrders', { http: HttpServiceOrderGateway });
}

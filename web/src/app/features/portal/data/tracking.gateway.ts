import { HttpContext } from '@angular/common/http';
import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import { required } from '../../../core/data/contract-error';
import { SKIP_AUTH, SKIP_GLOBAL_ERROR_HANDLING } from '../../../core/http/http-context';
import { centsFromDecimal, Cents } from '../../../shared/util/money';
import { onlyDigits } from '../../../shared/util/digits';
import { ServiceOrderStatus } from '../../service-orders/data/service-order.models';

export interface TrackedOrder {
  readonly number: number;
  readonly status: ServiceOrderStatus;
  readonly problemDescription: string;
  readonly total: Cents;
  readonly vehicle: string;
  readonly plate: string;
  readonly quoteStatus: 'Pending' | 'Approved' | 'Rejected' | null;
  readonly openedAt: string;
  readonly deliveredAt: string | null;
}

interface LookupData {
  readonly url: '/api/public/service-orders/lookup';
  readonly body: { readonly number: number; readonly document: string };
}

interface LookupDto {
  readonly number?: number;
  readonly status?: ServiceOrderStatus;
  readonly problemDescription?: string;
  readonly totalAmount?: number;
  readonly vehicle?: { licensePlate?: string; brand?: string; model?: string; year?: number };
  readonly quote?: { status?: 'Pending' | 'Approved' | 'Rejected' } | null;
  readonly openedAt?: string;
  readonly deliveredAt?: string | null;
}

export function toTrackedOrder(dto: LookupDto): TrackedOrder {
  const vehicle = dto.vehicle ?? {};
  return {
    number: required(dto.number, 'number'),
    status: required(dto.status, 'status'),
    problemDescription: dto.problemDescription ?? '',
    total: centsFromDecimal(dto.totalAmount ?? 0),
    vehicle: [vehicle.brand, vehicle.model, vehicle.year || null].filter(Boolean).join(' '),
    plate: vehicle.licensePlate ?? '',
    quoteStatus: dto.quote?.status ?? null,
    openedAt: required(dto.openedAt, 'openedAt'),
    deliveredAt: dto.deliveredAt ?? null,
  };
}

export abstract class TrackingGateway {
  abstract lookup(number: number, document: string): Observable<TrackedOrder>;
}

@Injectable({ providedIn: 'root' })
export class HttpTrackingGateway extends TrackingGateway {
  private readonly api = inject(ApiClient);

  lookup(number: number, document: string): Observable<TrackedOrder> {
    return this.api
      .send<LookupData, LookupDto>('POST', '/api/public/service-orders/lookup', {
        body: { number, document: onlyDigits(document) },
        context: new HttpContext().set(SKIP_AUTH, true).set(SKIP_GLOBAL_ERROR_HANDLING, true),
      })
      .pipe(map(toTrackedOrder));
  }
}

export function provideTrackingData(): Provider {
  return provideGateway(TrackingGateway, 'portal', { http: HttpTrackingGateway });
}

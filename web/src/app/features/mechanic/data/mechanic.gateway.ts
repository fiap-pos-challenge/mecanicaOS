import { HttpContext } from '@angular/common/http';
import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import { required } from '../../../core/data/contract-error';
import { RETRY_ENABLED, SKIP_GLOBAL_ERROR_HANDLING } from '../../../core/http/http-context';
import { ServiceOrderStatus } from '../../service-orders/data/service-order.models';
import {
  AiSuggestedItem,
  ChecklistState,
  MechanicOrder,
  WorkAction,
  WorkPhoto,
} from './mechanic.models';

export interface MechanicOrderDto {
  readonly id?: string;
  readonly number?: number;
  readonly status?: ServiceOrderStatus;
  readonly vehicle?: string;
  readonly licensePlate?: string;
  readonly complaint?: string;
  readonly promisedAt?: string | null;
  readonly checklist?: readonly {
    readonly id?: string;
    readonly label?: string;
    readonly state?: ChecklistState | null;
  }[];
  readonly photos?: readonly {
    readonly id?: string;
    readonly url?: string;
    readonly caption?: string;
    readonly takenAt?: string;
  }[];
  readonly diagnosis?: string;
  readonly sentToQuote?: boolean;
  readonly timerRunning?: boolean;
}

export interface AiSuggestedItemDto {
  readonly kind?: 'Service' | 'Part';
  readonly itemId?: string;
  readonly name?: string;
  readonly confidence?: number;
  readonly reason?: string;
}

export const WORK_ACTION_URLS = {
  checklist: '/api/admin/service-orders/{id}/checklist',
  photo: '/api/admin/service-orders/{id}/photos',
  diagnosis: '/api/admin/service-orders/{id}/diagnosis',
  sendToQuote: '/api/admin/service-orders/{id}/send-to-quote',
  start: '/api/admin/service-orders/{id}/start-service',
  finish: '/api/admin/service-orders/{id}/finish-service',
} as const;

interface MechanicData {
  readonly url:
    | '/api/admin/service-orders/mine'
    | '/api/admin/service-orders/{id}/work'
    | '/api/admin/service-orders/{id}/diagnosis/suggestions'
    | (typeof WORK_ACTION_URLS)[keyof typeof WORK_ACTION_URLS];
  readonly path?: { readonly id: string };
  readonly body?: unknown;
}

export function toMechanicOrder(dto: MechanicOrderDto): MechanicOrder {
  return {
    id: required(dto.id, 'id'),
    number: required(dto.number, 'number'),
    status: required(dto.status, 'status'),
    vehicle: dto.vehicle ?? '',
    plate: required(dto.licensePlate, 'licensePlate'),
    complaint: dto.complaint ?? '',
    promisedAt: dto.promisedAt ?? null,
    checklist: (dto.checklist ?? []).map((item) => ({
      id: required(item.id, 'checklist.id'),
      label: required(item.label, 'checklist.label'),
      state: item.state ?? null,
    })),
    photos: (dto.photos ?? []).map((photo): WorkPhoto => ({
      id: required(photo.id, 'photos.id'),
      url: required(photo.url, 'photos.url'),
      caption: photo.caption ?? '',
      takenAt: required(photo.takenAt, 'photos.takenAt'),
      pending: false,
    })),
    diagnosis: dto.diagnosis ?? '',
    sentToQuote: dto.sentToQuote ?? false,
    timerRunning: dto.timerRunning ?? false,
  };
}

export function toSuggestion(dto: AiSuggestedItemDto): AiSuggestedItem {
  return {
    kind: required(dto.kind, 'kind'),
    itemId: required(dto.itemId, 'itemId'),
    name: required(dto.name, 'name'),
    confidence: dto.confidence ?? 0,
    reason: dto.reason ?? '',
  };
}

export function actionRequest(action: WorkAction): {
  readonly method: 'PUT' | 'POST';
  readonly body: unknown;
} {
  switch (action.kind) {
    case 'checklist':
      return {
        method: 'PUT',
        body: { items: action.items.map((item) => ({ id: item.id, state: item.state })) },
      };
    case 'photo':
      return {
        method: 'POST',
        body: {
          clientId: action.photo.id,
          fileName: action.photo.fileName,
          contentType: action.photo.contentType,
          dataUrl: action.photo.dataUrl,
        },
      };
    case 'diagnosis':
      return { method: 'PUT', body: { description: action.description } };
    case 'sendToQuote':
      return { method: 'POST', body: action.request };
    case 'start':
    case 'finish':
      return { method: 'POST', body: null };
  }
}

export abstract class MechanicGateway {
  abstract mine(): Observable<readonly MechanicOrder[]>;
  abstract work(id: string): Observable<MechanicOrder>;
  abstract suggest(id: string, description: string): Observable<readonly AiSuggestedItem[]>;
  abstract perform(id: string, action: WorkAction): Observable<MechanicOrder>;
}

@Injectable({ providedIn: 'root' })
export class HttpMechanicGateway extends MechanicGateway {
  private readonly api = inject(ApiClient);

  mine(): Observable<readonly MechanicOrder[]> {
    return this.api
      .send<MechanicData, { items?: readonly MechanicOrderDto[] }>(
        'GET',
        '/api/admin/service-orders/mine',
      )
      .pipe(map((dto) => (dto.items ?? []).map(toMechanicOrder)));
  }

  work(id: string): Observable<MechanicOrder> {
    return this.api
      .send<MechanicData, MechanicOrderDto>('GET', '/api/admin/service-orders/{id}/work', {
        path: { id },
      })
      .pipe(map(toMechanicOrder));
  }

  suggest(id: string, description: string): Observable<readonly AiSuggestedItem[]> {
    return this.api
      .send<MechanicData, { items?: readonly AiSuggestedItemDto[] }>(
        'POST',
        '/api/admin/service-orders/{id}/diagnosis/suggestions',
        { path: { id }, body: { description } },
      )
      .pipe(map((dto) => (dto.items ?? []).map(toSuggestion)));
  }

  perform(id: string, action: WorkAction): Observable<MechanicOrder> {
    const request = actionRequest(action);
    return this.api
      .send<MechanicData, MechanicOrderDto>(request.method, WORK_ACTION_URLS[action.kind], {
        path: { id },
        body: request.body,
        context: new HttpContext().set(SKIP_GLOBAL_ERROR_HANDLING, true).set(RETRY_ENABLED, false),
      })
      .pipe(map(toMechanicOrder));
  }
}

export function provideMechanicData(): Provider {
  return provideGateway(MechanicGateway, 'serviceOrders', { http: HttpMechanicGateway });
}

import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import { required } from '../../../core/data/contract-error';
import { cents } from '../../../shared/util/money';
import {
  AttentionItemDto,
  DashboardActionData,
  DashboardCardSettingDto,
  DashboardLayoutData,
  DashboardSnapshotData,
  DashboardSnapshotDto,
} from './dashboard.contracts';
import {
  AttentionAction,
  AttentionItem,
  DashboardLayout,
  DashboardPeriod,
  DashboardSnapshot,
  normalizeLayout,
} from './dashboard.models';

export abstract class DashboardGateway {
  abstract snapshot(period: DashboardPeriod): Observable<DashboardSnapshot>;
  abstract layout(): Observable<DashboardLayout>;
  abstract saveLayout(layout: DashboardLayout): Observable<DashboardLayout>;
  abstract act(itemId: string, action: AttentionAction): Observable<void>;
}

function toAttention(dto: AttentionItemDto): AttentionItem {
  return {
    id: required(dto.id, 'attention.id'),
    kind: required(dto.kind, 'attention.kind'),
    severity: dto.severity ?? 'info',
    title: required(dto.title, 'attention.title'),
    detail: dto.detail ?? '',
    action: dto.action ?? null,
    target: dto.target ?? null,
    aiSuggested: dto.aiSuggested ?? false,
  };
}

export function toSnapshot(dto: DashboardSnapshotDto): DashboardSnapshot {
  return {
    kpis: (dto.kpis ?? []).map((kpi) => ({
      id: required(kpi.id, 'kpis.id'),
      value: kpi.value ?? 0,
      previous: kpi.previous ?? null,
      detail: kpi.detail ?? null,
    })),
    dailyRevenue: (dto.dailyRevenue ?? []).map((day) => ({
      date: required(day.date, 'dailyRevenue.date'),
      services: cents(day.services ?? 0),
      parts: cents(day.parts ?? 0),
    })),
    topServices: (dto.topServices ?? []).map((service) => ({
      name: required(service.name, 'topServices.name'),
      quantity: service.quantity ?? 0,
    })),
    yard: (dto.yard ?? []).map((slice) => ({
      status: required(slice.status, 'yard.status'),
      count: slice.count ?? 0,
    })),
    attention: (dto.attention ?? []).map(toAttention),
  };
}

@Injectable({ providedIn: 'root' })
export class HttpDashboardGateway extends DashboardGateway {
  private readonly api = inject(ApiClient);

  snapshot(period: DashboardPeriod): Observable<DashboardSnapshot> {
    return this.api
      .send<DashboardSnapshotData, DashboardSnapshotDto>('GET', '/api/admin/dashboard', {
        query: { period },
      })
      .pipe(map(toSnapshot));
  }

  layout(): Observable<DashboardLayout> {
    return this.api
      .send<DashboardLayoutData, { cards?: readonly DashboardCardSettingDto[] }>(
        'GET',
        '/api/admin/dashboard/layout',
      )
      .pipe(map((dto) => normalizeLayout(dto.cards ?? [])));
  }

  saveLayout(layout: DashboardLayout): Observable<DashboardLayout> {
    return this.api
      .send<DashboardLayoutData, { cards?: readonly DashboardCardSettingDto[] }>(
        'PUT',
        '/api/admin/dashboard/layout',
        { body: { cards: layout.cards } },
      )
      .pipe(map((dto) => normalizeLayout(dto.cards ?? [])));
  }

  act(itemId: string, action: AttentionAction): Observable<void> {
    return this.api
      .send<DashboardActionData, unknown>('POST', '/api/admin/dashboard/attention/{id}/actions', {
        path: { id: itemId },
        body: { action },
      })
      .pipe(map(() => undefined));
  }
}

export function provideDashboardData(): Provider {
  return provideGateway(DashboardGateway, 'dashboard', { http: HttpDashboardGateway });
}

import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import { required } from '../../../core/data/contract-error';
import { cents } from '../../../shared/util/money';
import { FinancePeriod, FinanceSummary, PlatformOverview } from './finance.models';

interface FinanceData {
  readonly url: '/api/admin/finance';
  readonly query: { readonly period: FinancePeriod };
}

interface PlatformData {
  readonly url: '/api/admin/platform';
}

interface FinanceSummaryDto {
  readonly period?: FinancePeriod;
  readonly from?: string;
  readonly to?: string;
  readonly kpis?: {
    readonly received?: number;
    readonly receivable?: number;
    readonly receivableCount?: number;
    readonly commissions?: number;
    readonly averageTicket?: number;
    readonly orders?: number;
  };
  readonly byMethod?: readonly { method?: string; amount?: number; count?: number }[];
  readonly receipts?: readonly {
    serviceOrderId?: string;
    number?: number;
    customerName?: string;
    deliveredAt?: string;
    method?: string;
    amount?: number;
  }[];
  readonly receivables?: readonly {
    serviceOrderId?: string;
    number?: number;
    customerName?: string;
    completedAt?: string | null;
    amount?: number;
    daysWaiting?: number;
  }[];
  readonly commissions?: readonly {
    mechanicName?: string;
    orders?: number;
    services?: number;
    commission?: number;
  }[];
}

interface PlatformDto {
  readonly shopName?: string;
  readonly plan?: { name?: string; renewsAt?: string; users?: number; maxUsers?: number };
  readonly usage?: {
    customers?: number;
    vehicles?: number;
    serviceOrdersThisMonth?: number;
    documentsThisMonth?: number;
    activeAutomations?: number;
  };
  readonly modules?: readonly { id?: string; name?: string; enabled?: boolean }[];
}

export function toFinanceSummary(dto: FinanceSummaryDto): FinanceSummary {
  const kpis = dto.kpis ?? {};
  return {
    period: required(dto.period, 'period'),
    from: required(dto.from, 'from'),
    to: required(dto.to, 'to'),
    kpis: {
      received: cents(kpis.received ?? 0),
      receivable: cents(kpis.receivable ?? 0),
      receivableCount: kpis.receivableCount ?? 0,
      commissions: cents(kpis.commissions ?? 0),
      averageTicket: cents(kpis.averageTicket ?? 0),
      orders: kpis.orders ?? 0,
    },
    byMethod: (dto.byMethod ?? []).map((item) => ({
      method: required(item.method, 'byMethod.method'),
      amount: cents(item.amount ?? 0),
      count: item.count ?? 0,
    })),
    receipts: (dto.receipts ?? []).map((item) => ({
      serviceOrderId: required(item.serviceOrderId, 'receipts.serviceOrderId'),
      number: required(item.number, 'receipts.number'),
      customerName: item.customerName ?? '',
      deliveredAt: required(item.deliveredAt, 'receipts.deliveredAt'),
      method: item.method ?? '',
      amount: cents(item.amount ?? 0),
    })),
    receivables: (dto.receivables ?? []).map((item) => ({
      serviceOrderId: required(item.serviceOrderId, 'receivables.serviceOrderId'),
      number: required(item.number, 'receivables.number'),
      customerName: item.customerName ?? '',
      completedAt: item.completedAt ?? null,
      amount: cents(item.amount ?? 0),
      daysWaiting: item.daysWaiting ?? 0,
    })),
    commissions: (dto.commissions ?? []).map((item) => ({
      mechanicName: required(item.mechanicName, 'commissions.mechanicName'),
      orders: item.orders ?? 0,
      services: cents(item.services ?? 0),
      commission: cents(item.commission ?? 0),
    })),
  };
}

export function toPlatform(dto: PlatformDto): PlatformOverview {
  const plan = dto.plan ?? {};
  const usage = dto.usage ?? {};
  return {
    shopName: dto.shopName ?? '',
    plan: {
      name: required(plan.name, 'plan.name'),
      renewsAt: required(plan.renewsAt, 'plan.renewsAt'),
      users: plan.users ?? 0,
      maxUsers: plan.maxUsers ?? 0,
    },
    usage: {
      customers: usage.customers ?? 0,
      vehicles: usage.vehicles ?? 0,
      serviceOrdersThisMonth: usage.serviceOrdersThisMonth ?? 0,
      documentsThisMonth: usage.documentsThisMonth ?? 0,
      activeAutomations: usage.activeAutomations ?? 0,
    },
    modules: (dto.modules ?? []).map((module) => ({
      id: required(module.id, 'modules.id'),
      name: module.name ?? '',
      enabled: module.enabled ?? false,
    })),
  };
}

export abstract class FinanceGateway {
  abstract summary(period: FinancePeriod): Observable<FinanceSummary>;
}

export abstract class PlatformGateway {
  abstract overview(): Observable<PlatformOverview>;
}

@Injectable({ providedIn: 'root' })
export class HttpFinanceGateway extends FinanceGateway {
  private readonly api = inject(ApiClient);

  summary(period: FinancePeriod): Observable<FinanceSummary> {
    return this.api
      .send<FinanceData, FinanceSummaryDto>('GET', '/api/admin/finance', { query: { period } })
      .pipe(map(toFinanceSummary));
  }
}

@Injectable({ providedIn: 'root' })
export class HttpPlatformGateway extends PlatformGateway {
  private readonly api = inject(ApiClient);

  overview(): Observable<PlatformOverview> {
    return this.api
      .send<PlatformData, PlatformDto>('GET', '/api/admin/platform')
      .pipe(map(toPlatform));
  }
}

export function provideFinanceData(): Provider {
  return provideGateway(FinanceGateway, 'finance', { http: HttpFinanceGateway });
}

export function providePlatformData(): Provider {
  return provideGateway(PlatformGateway, 'platform', { http: HttpPlatformGateway });
}

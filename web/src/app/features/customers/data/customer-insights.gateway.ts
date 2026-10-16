import { HttpContext } from '@angular/common/http';
import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable, of } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import { required } from '../../../core/data/contract-error';
import { centsFromDecimal } from '../../../shared/util/money';
import {
  CustomerSummariesData,
  CustomerSummaryDto,
  CustomerSummaryListDto,
  ServiceHistoryData,
  ServiceHistoryDto,
  ServiceHistoryEntryDto,
} from './insights.contracts';
import { CustomerSummary, ServiceHistoryEntry } from './insights.models';

export const MAX_SUMMARY_IDS = 500;

export function toCustomerSummary(dto: CustomerSummaryDto): CustomerSummary {
  return {
    customerId: required(dto.customerId, 'customerId'),
    vehicleCount: dto.vehicleCount ?? 0,
    lastVisitAt: dto.lastVisitAt ?? null,
    totalSpent: centsFromDecimal(dto.totalSpent ?? 0),
    averageTicket: centsFromDecimal(dto.averageTicket ?? 0),
    serviceOrderCount: dto.serviceOrderCount ?? 0,
    openServiceOrders: dto.openServiceOrders ?? 0,
    situation: dto.situation ?? 'Active',
    fleet: dto.fleet ?? false,
  };
}

export function toServiceHistoryEntry(dto: ServiceHistoryEntryDto): ServiceHistoryEntry {
  return {
    number: required(dto.number, 'number'),
    description: required(dto.description, 'description'),
    total: centsFromDecimal(required(dto.total, 'total')),
    status: required(dto.status, 'status'),
    openedAt: required(dto.openedAt, 'openedAt'),
    closedAt: dto.closedAt ?? null,
    paymentMethod: dto.paymentMethod ?? null,
  };
}

export abstract class CustomerInsightsGateway {
  abstract summaries(
    customerIds: readonly string[],
  ): Observable<ReadonlyMap<string, CustomerSummary>>;
  abstract history(customerId: string): Observable<readonly ServiceHistoryEntry[]>;
}

@Injectable({ providedIn: 'root' })
export class HttpCustomerInsightsGateway extends CustomerInsightsGateway {
  private readonly api = inject(ApiClient);

  protected context(): HttpContext {
    return new HttpContext();
  }

  summaries(customerIds: readonly string[]): Observable<ReadonlyMap<string, CustomerSummary>> {
    if (customerIds.length === 0) {
      return of(new Map());
    }

    return this.api
      .send<CustomerSummariesData, CustomerSummaryListDto>(
        'POST',
        '/api/admin/customers/summaries',
        {
          body: { customerIds: customerIds.slice(0, MAX_SUMMARY_IDS) },
          context: this.context(),
        },
      )
      .pipe(
        map(
          (dto) =>
            new Map(
              (dto.items ?? [])
                .map(toCustomerSummary)
                .map((summary) => [summary.customerId, summary]),
            ),
        ),
      );
  }

  history(customerId: string): Observable<readonly ServiceHistoryEntry[]> {
    return this.api
      .send<ServiceHistoryData, ServiceHistoryDto>(
        'GET',
        '/api/admin/customers/{id}/service-history',
        {
          path: { id: customerId },
          context: this.context(),
        },
      )
      .pipe(map((dto) => (dto.items ?? []).map(toServiceHistoryEntry)));
  }
}

export function provideCustomerInsightsData(): Provider {
  return provideGateway(CustomerInsightsGateway, 'customerInsights', {
    http: HttpCustomerInsightsGateway,
  });
}

import { Cents } from '../../../shared/util/money';
import { CustomerSituation } from './insights.contracts';

export interface CustomerSummary {
  readonly customerId: string;
  readonly vehicleCount: number;
  readonly lastVisitAt: string | null;
  readonly totalSpent: Cents;
  readonly averageTicket: Cents;
  readonly serviceOrderCount: number;
  readonly openServiceOrders: number;
  readonly situation: CustomerSituation;
  readonly fleet: boolean;
}

export interface ServiceHistoryEntry {
  readonly number: number;
  readonly description: string;
  readonly total: Cents;
  readonly status: 'Delivered' | 'InProgress';
  readonly openedAt: string;
  readonly closedAt: string | null;
  readonly paymentMethod: string | null;
}

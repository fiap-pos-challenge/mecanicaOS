import { Cents } from '../../../shared/util/money';

export type FinancePeriod = 'today' | 'week' | 'month' | 'quarter';

export const FINANCE_PERIODS: readonly FinancePeriod[] = ['today', 'week', 'month', 'quarter'];

export interface FinanceKpis {
  readonly received: Cents;
  readonly receivable: Cents;
  readonly receivableCount: number;
  readonly commissions: Cents;
  readonly averageTicket: Cents;
  readonly orders: number;
}

export interface MethodTotal {
  readonly method: string;
  readonly amount: Cents;
  readonly count: number;
}

export interface Receipt {
  readonly serviceOrderId: string;
  readonly number: number;
  readonly customerName: string;
  readonly deliveredAt: string;
  readonly method: string;
  readonly amount: Cents;
}

export interface Receivable {
  readonly serviceOrderId: string;
  readonly number: number;
  readonly customerName: string;
  readonly completedAt: string | null;
  readonly amount: Cents;
  readonly daysWaiting: number;
}

export interface Commission {
  readonly mechanicName: string;
  readonly orders: number;
  readonly services: Cents;
  readonly commission: Cents;
}

export interface FinanceSummary {
  readonly period: FinancePeriod;
  readonly from: string;
  readonly to: string;
  readonly kpis: FinanceKpis;
  readonly byMethod: readonly MethodTotal[];
  readonly receipts: readonly Receipt[];
  readonly receivables: readonly Receivable[];
  readonly commissions: readonly Commission[];
}

export interface PlatformModule {
  readonly id: string;
  readonly name: string;
  readonly enabled: boolean;
}

export interface PlatformOverview {
  readonly shopName: string;
  readonly plan: {
    readonly name: string;
    readonly renewsAt: string;
    readonly users: number;
    readonly maxUsers: number;
  };
  readonly usage: {
    readonly customers: number;
    readonly vehicles: number;
    readonly serviceOrdersThisMonth: number;
    readonly documentsThisMonth: number;
    readonly activeAutomations: number;
  };
  readonly modules: readonly PlatformModule[];
}

import { ServiceOrderStatus } from '../../service-orders/data/service-order.models';
import {
  AttentionAction,
  AttentionKind,
  AttentionSeverity,
  AttentionTarget,
  DashboardCardId,
  DashboardPeriod,
  KpiId,
} from './dashboard.models';

export interface DashboardKpiDto {
  readonly id?: KpiId;
  readonly value?: number;
  readonly previous?: number | null;
  readonly detail?: number | null;
}

export interface DailyRevenueDto {
  readonly date?: string;
  readonly services?: number;
  readonly parts?: number;
}

export interface TopServiceDto {
  readonly name?: string;
  readonly quantity?: number;
}

export interface YardCountDto {
  readonly status?: ServiceOrderStatus;
  readonly count?: number;
}

export interface AttentionItemDto {
  readonly id?: string;
  readonly kind?: AttentionKind;
  readonly severity?: AttentionSeverity;
  readonly title?: string;
  readonly detail?: string;
  readonly action?: AttentionAction | null;
  readonly target?: AttentionTarget | null;
  readonly aiSuggested?: boolean;
}

export interface DashboardSnapshotDto {
  readonly kpis?: readonly DashboardKpiDto[];
  readonly dailyRevenue?: readonly DailyRevenueDto[];
  readonly topServices?: readonly TopServiceDto[];
  readonly yard?: readonly YardCountDto[];
  readonly attention?: readonly AttentionItemDto[];
}

export interface DashboardCardSettingDto {
  readonly id?: DashboardCardId;
  readonly visible?: boolean;
}

export interface DashboardSnapshotData {
  readonly url: '/api/admin/dashboard';
  readonly query: { readonly period: DashboardPeriod };
}

export interface DashboardLayoutData {
  readonly url: '/api/admin/dashboard/layout';
  readonly body?: { readonly cards: readonly DashboardCardSettingDto[] };
}

export interface DashboardActionData {
  readonly url: '/api/admin/dashboard/attention/{id}/actions';
  readonly path: { readonly id: string };
  readonly body: { readonly action: AttentionAction };
}

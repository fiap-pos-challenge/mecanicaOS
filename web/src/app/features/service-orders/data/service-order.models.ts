import { ServiceOrderStatus } from '../../../core/realtime/realtime.gateway';
import { Cents } from '../../../shared/util/money';

export type { ServiceOrderStatus };

export const SERVICE_ORDER_STATUSES: readonly ServiceOrderStatus[] = [
  'Received',
  'InDiagnosis',
  'WaitingApproval',
  'InProgress',
  'Completed',
  'Delivered',
];

export type ServiceOrderTag = 'Late' | 'MissingPart' | 'ReminderSent';

export type DocumentKind = 'ServiceOrder' | 'Quote' | 'Receipt';

export interface ServiceOrderCard {
  readonly id: string;
  readonly number: number;
  readonly status: ServiceOrderStatus;
  readonly version: number;
  readonly customerName: string;
  readonly plate: string;
  readonly vehicle: string;
  readonly summary: string;
  readonly mechanicName: string | null;
  readonly promisedAt: string | null;
  readonly total: Cents;
  readonly hasQuoteItems: boolean;
  readonly tags: readonly ServiceOrderTag[];
  readonly updatedAt: string;
}

export interface StatusEntry {
  readonly from: ServiceOrderStatus | null;
  readonly to: ServiceOrderStatus;
  readonly changedBy: string | null;
  readonly note: string | null;
  readonly changedAt: string;
}

export interface OrderQuoteItem {
  readonly name: string;
  readonly quantity: number;
  readonly total: Cents;
}

export interface OrderQuote {
  readonly id: string;
  readonly number: number;
  readonly version: number;
  readonly status: 'Pending' | 'Approved' | 'Rejected';
  readonly total: Cents;
  readonly items: readonly OrderQuoteItem[];
}

export interface ReservedPart {
  readonly name: string;
  readonly quantity: number;
  readonly unit: string;
  readonly reserved: boolean;
  readonly leadTimeDays: number | null;
}

export interface OrderPhoto {
  readonly id: string;
  readonly url: string;
  readonly caption: string;
  readonly takenAt: string;
}

export interface TimeEntry {
  readonly id: string;
  readonly mechanicName: string;
  readonly startedAt: string;
  readonly endedAt: string | null;
  readonly minutes: number;
}

export interface OrderDocument {
  readonly kind: DocumentKind;
  readonly code: string;
  readonly issuedAt: string;
}

export interface ServiceOrderDetail extends ServiceOrderCard {
  readonly problemDescription: string;
  readonly mileage: number;
  readonly history: readonly StatusEntry[];
  readonly quote: OrderQuote | null;
  readonly parts: readonly ReservedPart[];
  readonly photos: readonly OrderPhoto[];
  readonly timeEntries: readonly TimeEntry[];
  readonly documents: readonly OrderDocument[];
}

export interface OpenOrderInput {
  readonly customerId: string;
  readonly vehicleId: string;
  readonly problemDescription: string;
  readonly promisedAt: string | null;
  readonly mileage: number | null;
}

export const PROBLEM_MIN_LENGTH = 3;
export const PROBLEM_MAX_LENGTH = 1000;

export interface TransitionInput {
  readonly to: ServiceOrderStatus;
  readonly version: number;
  readonly reason: string | null;
}

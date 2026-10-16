import { Cents } from '../../../shared/util/money';
import { ProductivityPeriod, WeekDay } from './team-profile.contracts';

export type { ProductivityPeriod, WeekDay } from './team-profile.contracts';

export const WEEK_DAYS: readonly WeekDay[] = [
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
  'Sunday',
];

export const MAX_SPECIALTIES = 8;
export const MAX_COMMISSION_PERCENT = 50;
export const MAX_DISCOUNT_PERCENT = 30;

export interface WorkShift {
  readonly day: WeekDay;
  readonly start: string;
  readonly end: string;
}

export interface TeamProfile {
  readonly userId: string;
  readonly specialties: readonly string[];
  readonly commissionPercent: number;
  readonly discountLimitPercent: number;
  readonly schedule: readonly WorkShift[];
  readonly updatedAt: string | null;
}

export type TeamProfileInput = Omit<TeamProfile, 'userId' | 'updatedAt'>;

export interface Productivity {
  readonly userId: string;
  readonly serviceOrders: number;
  readonly hours: number;
  readonly occupancyPercent: number;
  readonly revenue: Cents;
  readonly reworkPercent: number;
  readonly weekly: readonly number[];
}

export interface ProductivityReport {
  readonly periodDays: ProductivityPeriod;
  readonly items: ReadonlyMap<string, Productivity>;
}

import { messages } from '../../../app/core/i18n/messages.pt-BR';
import { seedOf } from '../customers/insights.fake-api';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import { FakeShift, FakeTeamProfile, FakeWeekDay } from '../fake-api/fake-database';
import { fieldError, FakeRouter, ok, problem, readBody, validation } from '../fake-api/fake-http';
import { fakeNow } from '../fake-api/fake-ids';
import { authorize, FakeRole, isPrincipal } from '../fake-api/fake-session';

export const TEAM_PATHS = {
  profiles: '/api/admin/team/profiles/query',
  profile: '/api/admin/team/:userId/profile',
  productivity: '/api/admin/team/productivity',
} as const;

export const MAX_COMMISSION_PERCENT = 50;
export const MAX_DISCOUNT_PERCENT = 30;
export const MAX_SPECIALTIES = 8;
export const PRODUCTIVITY_PERIODS = [30, 90] as const;

const WEEK_DAYS: readonly FakeWeekDay[] = [
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
  'Sunday',
];
const TIME = /^([01]\d|2[0-3]):[0-5]\d$/;
const readers: readonly FakeRole[] = ['Admin', 'Manager'];
const text = messages.fake;

export interface FakeProductivity {
  readonly userId: string;
  readonly serviceOrders: number;
  readonly hours: number;
  readonly occupancyPercent: number;
  readonly revenue: number;
  readonly reworkPercent: number;
  readonly weekly: readonly number[];
}

export function productivityOf(userId: string, periodDays: number): FakeProductivity {
  const seed = seedOf(userId);
  const weeks = Math.round(periodDays / 7);
  const base = 3 + (seed % 4);
  const weekly = Array.from(
    { length: weeks },
    (_, index) => base + (seedOf(`${userId}:${index}`) % 4) - (index < weeks / 2 ? 1 : 0),
  );
  const serviceOrders = weekly.reduce((sum, value) => sum + value, 0);
  return {
    userId,
    serviceOrders,
    hours: Math.round(serviceOrders * (2.5 + (seed % 10) / 10)),
    occupancyPercent: 68 + (seed % 27),
    revenue: Math.round(serviceOrders * (720 + (seed % 180)) * 100) / 100,
    reworkPercent: Math.round(((seed % 50) / 10) * 10) / 10,
    weekly,
  };
}

function readIds(value: unknown): string[] | null {
  if (!Array.isArray(value) || value.length > 500) {
    return null;
  }

  const ids = value.filter((id): id is string => typeof id === 'string');
  return ids.length === value.length ? ids : null;
}

function readShifts(value: unknown): FakeShift[] | null {
  if (!Array.isArray(value)) {
    return null;
  }

  const shifts = value.map((item: unknown) => {
    const shift =
      typeof item === 'object' && item !== null ? (item as Record<string, unknown>) : {};
    const day = WEEK_DAYS.find((candidate) => candidate === shift['day']);
    const start = typeof shift['start'] === 'string' ? shift['start'] : '';
    const end = typeof shift['end'] === 'string' ? shift['end'] : '';
    return day && TIME.test(start) && TIME.test(end) && start < end ? { day, start, end } : null;
  });
  const valid = shifts.filter((shift): shift is FakeShift => shift !== null);
  const uniqueDays = new Set(valid.map((shift) => shift.day)).size === valid.length;
  return valid.length === shifts.length && uniqueDays ? valid : null;
}

function readSpecialties(value: unknown): string[] | null {
  if (!Array.isArray(value) || value.length > MAX_SPECIALTIES) {
    return null;
  }

  const items = value.map((item: unknown) => (typeof item === 'string' ? item.trim() : ''));
  return items.every((item) => item.length >= 2 && item.length <= 40) ? items : null;
}

function readPercent(value: unknown, max: number): number | null {
  return typeof value === 'number' && Number.isFinite(value) && value >= 0 && value <= max
    ? value
    : null;
}

export function createTeamFakeApi(state: FakeBackendState): FakeFeatureApi {
  const users = (): FakeBackendState['database']['users'] => state.database.users;
  const profileOf = (userId: string): FakeTeamProfile => {
    const existing = state.database.teamProfiles.find((profile) => profile.userId === userId);
    if (existing) {
      return existing;
    }

    const created: FakeTeamProfile = {
      userId,
      specialties: [],
      commissionPercent: 0,
      discountLimitPercent: 0,
      schedule: [],
      updatedAt: null,
    };
    state.database.teamProfiles.push(created);
    return created;
  };

  const router = new FakeRouter()
    .on('POST', TEAM_PATHS.profiles, (request) => {
      const principal = authorize(request, users(), readers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const ids = readIds(readBody(request)['userIds']);
      return ids
        ? ok({ items: ids.map(profileOf) })
        : validation([fieldError('userIds', 'common.validationFailed', text.invalidField)]);
    })
    .on('GET', TEAM_PATHS.profile, (request, params) => {
      const principal = authorize(request, users(), readers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      return ok(profileOf(params['userId'] ?? ''));
    })
    .on('PUT', TEAM_PATHS.profile, (request, params) => {
      const principal = authorize(request, users(), ['Admin']);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const userId = params['userId'] ?? '';
      const user = users().find((candidate) => candidate.id === userId);

      const body = readBody(request);
      const specialties = readSpecialties(body['specialties']);
      const commission = readPercent(body['commissionPercent'], MAX_COMMISSION_PERCENT);
      const discount = readPercent(body['discountLimitPercent'], MAX_DISCOUNT_PERCENT);
      const schedule = readShifts(body['schedule']);
      const errors = [
        ...(specialties
          ? []
          : [fieldError('specialties', 'team.invalidSpecialties', text.invalidSpecialties)]),
        ...(commission === null
          ? [fieldError('commissionPercent', 'team.invalidCommission', text.invalidCommission)]
          : []),
        ...(discount === null
          ? [
              fieldError(
                'discountLimitPercent',
                'team.invalidDiscountLimit',
                text.invalidDiscountLimit,
              ),
            ]
          : []),
        ...(schedule ? [] : [fieldError('schedule', 'team.invalidShift', text.invalidShift)]),
      ];
      if (
        errors.length > 0 ||
        !specialties ||
        commission === null ||
        discount === null ||
        !schedule
      ) {
        return validation(errors);
      }

      if (user?.role === 'Mechanic' && discount > 0) {
        return problem(422, 'team.mechanicDiscount', text.mechanicDiscount);
      }

      const profile = profileOf(userId);
      Object.assign(profile, {
        specialties,
        commissionPercent: commission,
        discountLimitPercent: discount,
        schedule,
        updatedAt: fakeNow(),
      });
      return ok(profile);
    })
    .on('POST', TEAM_PATHS.productivity, (request) => {
      const principal = authorize(request, users(), readers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      const ids = readIds(body['userIds']);
      const period = PRODUCTIVITY_PERIODS.find((days) => days === body['periodDays']);
      if (!ids || !period) {
        return validation([fieldError('periodDays', 'common.validationFailed', text.invalidField)]);
      }

      return ok({ periodDays: period, items: ids.map((id) => productivityOf(id, period)) });
    });

  return new FakeFeatureApi('team', router, state.scenarios);
}

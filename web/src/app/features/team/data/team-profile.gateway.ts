import { HttpContext } from '@angular/common/http';
import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable, of } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import { required } from '../../../core/data/contract-error';
import { centsFromDecimal } from '../../../shared/util/money';
import {
  ProductivityDto,
  ProductivityListDto,
  ProductivityPeriod,
  ProductivityQueryData,
  TeamProfileData,
  TeamProfileDto,
  TeamProfileListDto,
  TeamProfilesQueryData,
  UpdateTeamProfileData,
  WorkShiftDto,
} from './team-profile.contracts';
import {
  Productivity,
  ProductivityReport,
  TeamProfile,
  TeamProfileInput,
  WorkShift,
} from './team-profile.models';

export const MAX_TEAM_IDS = 500;

function toShift(dto: WorkShiftDto): WorkShift {
  return {
    day: required(dto.day, 'day'),
    start: required(dto.start, 'start'),
    end: required(dto.end, 'end'),
  };
}

export function toTeamProfile(dto: TeamProfileDto): TeamProfile {
  return {
    userId: required(dto.userId, 'userId'),
    specialties: [...(dto.specialties ?? [])],
    commissionPercent: dto.commissionPercent ?? 0,
    discountLimitPercent: dto.discountLimitPercent ?? 0,
    schedule: (dto.schedule ?? []).map(toShift),
    updatedAt: dto.updatedAt ?? null,
  };
}

export function toProductivity(dto: ProductivityDto): Productivity {
  return {
    userId: required(dto.userId, 'userId'),
    serviceOrders: dto.serviceOrders ?? 0,
    hours: dto.hours ?? 0,
    occupancyPercent: dto.occupancyPercent ?? 0,
    revenue: centsFromDecimal(dto.revenue ?? 0),
    reworkPercent: dto.reworkPercent ?? 0,
    weekly: [...(dto.weekly ?? [])],
  };
}

export abstract class TeamProfileGateway {
  abstract profiles(userIds: readonly string[]): Observable<ReadonlyMap<string, TeamProfile>>;
  abstract get(userId: string): Observable<TeamProfile>;
  abstract update(userId: string, input: TeamProfileInput): Observable<TeamProfile>;
  abstract productivity(
    userIds: readonly string[],
    periodDays: ProductivityPeriod,
  ): Observable<ProductivityReport>;
}

@Injectable({ providedIn: 'root' })
export class HttpTeamProfileGateway extends TeamProfileGateway {
  private readonly api = inject(ApiClient);

  protected context(): HttpContext {
    return new HttpContext();
  }

  profiles(userIds: readonly string[]): Observable<ReadonlyMap<string, TeamProfile>> {
    if (userIds.length === 0) {
      return of(new Map());
    }

    return this.api
      .send<TeamProfilesQueryData, TeamProfileListDto>('POST', '/api/admin/team/profiles/query', {
        body: { userIds: userIds.slice(0, MAX_TEAM_IDS) },
        context: this.context(),
      })
      .pipe(
        map(
          (dto) =>
            new Map(
              (dto.items ?? []).map(toTeamProfile).map((profile) => [profile.userId, profile]),
            ),
        ),
      );
  }

  get(userId: string): Observable<TeamProfile> {
    return this.api
      .send<TeamProfileData, TeamProfileDto>('GET', '/api/admin/team/{userId}/profile', {
        path: { userId },
        context: this.context(),
      })
      .pipe(map(toTeamProfile));
  }

  update(userId: string, input: TeamProfileInput): Observable<TeamProfile> {
    return this.api
      .send<UpdateTeamProfileData, TeamProfileDto>('PUT', '/api/admin/team/{userId}/profile', {
        path: { userId },
        body: {
          specialties: input.specialties,
          commissionPercent: input.commissionPercent,
          discountLimitPercent: input.discountLimitPercent,
          schedule: input.schedule,
        },
        context: this.context(),
      })
      .pipe(map(toTeamProfile));
  }

  productivity(
    userIds: readonly string[],
    periodDays: ProductivityPeriod,
  ): Observable<ProductivityReport> {
    if (userIds.length === 0) {
      return of({ periodDays, items: new Map() });
    }

    return this.api
      .send<ProductivityQueryData, ProductivityListDto>('POST', '/api/admin/team/productivity', {
        body: { userIds: userIds.slice(0, MAX_TEAM_IDS), periodDays },
        context: this.context(),
      })
      .pipe(
        map((dto) => ({
          periodDays,
          items: new Map((dto.items ?? []).map(toProductivity).map((item) => [item.userId, item])),
        })),
      );
  }
}

export function provideTeamProfileData(): Provider {
  return provideGateway(TeamProfileGateway, 'teamProfiles', {
    http: HttpTeamProfileGateway,
  });
}

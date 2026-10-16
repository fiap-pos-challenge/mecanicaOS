import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { ApiError, isFeaturePending, toApiError } from '../../../core/http/api-error';
import { MAX_PAGE_SIZE } from '../../../core/data/page';
import { Role } from '../../auth/data/auth.models';
import { MAX_TEAM_IDS, TeamProfileGateway } from '../data/team-profile.gateway';
import { Productivity, ProductivityPeriod, TeamProfile } from '../data/team-profile.models';
import { User, UserStatus } from '../data/user.models';
import { UsersGateway } from '../data/users.gateway';

export type TeamView = 'cards' | 'list';

export interface TeamFilters {
  readonly role: Role | null;
  readonly status: UserStatus | null;
}

export interface TeamMember {
  readonly user: User;
  readonly profile: TeamProfile | null;
  readonly productivity: Productivity | null;
}

interface TeamState {
  readonly filters: TeamFilters;
  readonly period: ProductivityPeriod;
  readonly view: TeamView;
  readonly users: readonly User[];
  readonly totalItems: number;
  readonly activeAdmins: number;
  readonly profiles: ReadonlyMap<string, TeamProfile>;
  readonly productivity: ReadonlyMap<string, Productivity>;
  readonly loading: boolean;
  readonly error: ApiError | null;
  readonly productivityFailed: boolean;
  readonly productionPending: boolean;
}

export const DEFAULT_TEAM_FILTERS: TeamFilters = { role: null, status: null };

export function producesServices(role: Role): boolean {
  return role === 'Mechanic';
}

const initialState: TeamState = {
  filters: DEFAULT_TEAM_FILTERS,
  period: 30,
  view: 'cards',
  users: [],
  totalItems: 0,
  activeAdmins: 0,
  profiles: new Map(),
  productivity: new Map(),
  loading: false,
  error: null,
  productivityFailed: false,
  productionPending: false,
};

export const TeamStore = signalStore(
  withState(initialState),
  withComputed(({ users, profiles, productivity, filters }) => ({
    members: computed<readonly TeamMember[]>(() =>
      users().map((user) => ({
        user,
        profile: profiles().get(user.id) ?? null,
        productivity: productivity().get(user.id) ?? null,
      })),
    ),
    hasFilters: computed(() => filters().role !== null || filters().status !== null),
  })),
  withMethods((store) => {
    const usersGateway = inject(UsersGateway);
    const profilesGateway = inject(TeamProfileGateway);
    let request = 0;

    const fetchUsers = async (filters: TeamFilters): Promise<{ users: User[]; total: number }> => {
      const users: User[] = [];
      let page = 1;
      let total: number;
      do {
        const result = await firstValueFrom(
          usersGateway.list({ page, pageSize: MAX_PAGE_SIZE, ...filters }),
        );
        users.push(...result.items);
        total = result.totalItems;
        page++;
      } while (users.length < Math.min(total, MAX_TEAM_IDS));

      return { users, total };
    };

    const countActiveAdmins = async (): Promise<number> =>
      (
        await firstValueFrom(
          usersGateway.list({ page: 1, pageSize: 1, role: 'Admin', status: 'Active' }),
        )
      ).totalItems;

    const loadProductivity = async (
      users: readonly User[],
      period: ProductivityPeriod,
    ): Promise<ReadonlyMap<string, Productivity> | null> => {
      const ids = users.filter((user) => producesServices(user.role)).map((user) => user.id);
      try {
        return (await firstValueFrom(profilesGateway.productivity(ids, period))).items;
      } catch (error) {
        patchState(store, { productionPending: isFeaturePending(error) });
        return null;
      }
    };

    const loadProfiles = async (
      users: readonly User[],
    ): Promise<ReadonlyMap<string, TeamProfile>> => {
      try {
        return await firstValueFrom(profilesGateway.profiles(users.map((user) => user.id)));
      } catch (error) {
        patchState(store, { productionPending: isFeaturePending(error) });
        return new Map();
      }
    };

    return {
      async load(): Promise<void> {
        const current = ++request;
        patchState(store, { loading: true, error: null });
        try {
          const [{ users, total }, activeAdmins] = await Promise.all([
            fetchUsers(store.filters()),
            countActiveAdmins(),
          ]);
          const [profiles, productivity] = await Promise.all([
            loadProfiles(users),
            loadProductivity(users, store.period()),
          ]);
          if (current !== request) {
            return;
          }

          patchState(store, {
            users,
            totalItems: total,
            activeAdmins,
            profiles,
            productivity: productivity ?? new Map(),
            productivityFailed: productivity === null,
            loading: false,
          });
        } catch (error) {
          if (current === request) {
            patchState(store, { loading: false, error: toApiError(error, null) });
          }
        }
      },
      async update(changes: Partial<TeamFilters>): Promise<void> {
        patchState(store, { filters: { ...store.filters(), ...changes } });
        await this.load();
      },
      async clear(): Promise<void> {
        patchState(store, { filters: DEFAULT_TEAM_FILTERS });
        await this.load();
      },
      async setPeriod(period: ProductivityPeriod): Promise<void> {
        patchState(store, { period });
        const productivity = await loadProductivity(store.users(), period);
        patchState(store, {
          productivity: productivity ?? new Map(),
          productivityFailed: productivity === null,
        });
      },
      setView(view: TeamView): void {
        patchState(store, { view });
      },
    };
  }),
);

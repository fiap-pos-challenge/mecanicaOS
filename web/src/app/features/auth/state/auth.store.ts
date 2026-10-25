import { computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import {
  patchState,
  signalStore,
  withComputed,
  withHooks,
  withMethods,
  withState,
} from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { AuthTokenStore } from '../../../core/auth/auth-token.store';
import { AppRole, hasPermission, homeFor, Permission } from '../../../core/auth/permissions';
import { SessionEndReason, SessionService } from '../../../core/auth/session.service';
import { DraftStore } from '../../../core/drafts/draft.store';
import { AuthGateway } from '../data/auth.gateway';
import { AuthSession, Credentials, CurrentUser } from '../data/auth.models';
import { TwoFactorGateway } from '../data/two-factor.gateway';

export type AuthStatus = 'anonymous' | 'twoFactor' | 'authenticated';

export type LoginOutcome = 'authenticated' | 'twoFactor';

interface AuthState {
  readonly user: CurrentUser | null;
  readonly status: AuthStatus;
  readonly challengeId: string | null;
  readonly returnUrl: string | null;
}

const initialState: AuthState = {
  user: null,
  status: 'anonymous',
  challengeId: null,
  returnUrl: null,
};

export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed(({ user, status }) => ({
    role: computed<AppRole | null>(() => user()?.role ?? null),
    isAuthenticated: computed(() => status() === 'authenticated'),
  })),
  withMethods((store) => {
    const auth = inject(AuthGateway);
    const twoFactor = inject(TwoFactorGateway);
    const tokens = inject(AuthTokenStore);
    const session = inject(SessionService);
    const drafts = inject(DraftStore);
    const router = inject(Router);

    const loadUser = async (authSession: AuthSession): Promise<CurrentUser> => {
      tokens.set(authSession);
      try {
        return await firstValueFrom(auth.me());
      } catch (error) {
        tokens.clear();
        throw error;
      }
    };

    const completeLogin = async (authSession: AuthSession): Promise<LoginOutcome> => {
      const user = await loadUser(authSession);
      const ownership = drafts.claim(user.id);
      const target = (ownership === 'changed' ? null : store.returnUrl()) ?? homeFor(user.role);
      patchState(store, { user, status: 'authenticated', challengeId: null, returnUrl: null });
      session.acknowledge();
      await router.navigateByUrl(target);
      return 'authenticated';
    };

    return {
      can(permission: Permission): boolean {
        return hasPermission(store.role(), permission);
      },
      home(): string {
        return homeFor(store.role());
      },
      rememberReturnUrl(url: string): void {
        patchState(store, { returnUrl: url });
      },
      async login(credentials: Credentials): Promise<LoginOutcome> {
        const result = await firstValueFrom(auth.login(credentials));
        if (result.kind === 'twoFactor') {
          patchState(store, { status: 'twoFactor', challengeId: result.challengeId });
          await router.navigateByUrl('/login/verify');
          return 'twoFactor';
        }

        return completeLogin(result.session);
      },
      async verifyTwoFactor(code: string): Promise<LoginOutcome> {
        const challengeId = store.challengeId();
        if (challengeId === null) {
          await router.navigateByUrl('/login');
          return 'twoFactor';
        }

        return completeLogin(await firstValueFrom(twoFactor.verify(challengeId, code)));
      },
      cancelTwoFactor(): void {
        patchState(store, { status: 'anonymous', challengeId: null });
      },
      async restore(): Promise<boolean> {
        const renewed = await firstValueFrom(session.renew());
        if (renewed === null) {
          return false;
        }

        try {
          const user = await loadUser(renewed);
          drafts.claim(user.id);
          patchState(store, { user, status: 'authenticated' });
          return true;
        } catch {
          return false;
        }
      },
      logout(reason: Exclude<SessionEndReason, 'expired'> = 'logout'): void {
        session.logout(reason);
      },
      reset(reason: SessionEndReason): void {
        patchState(store, {
          user: null,
          status: 'anonymous',
          challengeId: null,
          returnUrl: reason === 'expired' ? router.url : null,
        });
      },
    };
  }),
  withHooks((store) => {
    const session = inject(SessionService);
    return {
      onInit(): void {
        session.ended$.subscribe((reason) => store.reset(reason));
      },
    };
  }),
);

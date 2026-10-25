import { computed, inject, InjectionToken } from '@angular/core';
import {
  patchState,
  signalStore,
  withComputed,
  withHooks,
  withMethods,
  withState,
} from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { ApiError, isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { PortalGateway } from '../data/portal.gateway';
import {
  ChargeStatus,
  PixCharge,
  PortalOrder,
  portalSteps,
  portalTotal,
} from '../data/portal.models';

export const PORTAL_POLL_MS = new InjectionToken<number>('PORTAL_POLL_MS', {
  providedIn: 'root',
  factory: () => 3000,
});

export const MAX_PIX_POLLS = 100;

export type PortalPhase =
  'checking' | 'confirm' | 'unavailable' | 'locked' | 'ready' | 'pending' | 'error';

export interface PortalNotice {
  readonly tone: 'ok' | 'bad' | 'info';
  readonly text: string;
}

interface PortalState {
  readonly phase: PortalPhase;
  readonly token: string;
  readonly attemptsLeft: number;
  readonly digitsError: string | null;
  readonly session: string | null;
  readonly order: PortalOrder | null;
  readonly selected: ReadonlySet<string>;
  readonly busy: boolean;
  readonly notice: PortalNotice | null;
  readonly cardNotice: PortalNotice | null;
  readonly pix: PixCharge | null;
  readonly pixStatus: ChargeStatus | null;
}

const text = messages.portal;

export const THROTTLED_CODES: readonly string[] = ['common.tooManyRequests', 'portal.tryAgain'];

function isThrottled(error: unknown): error is ApiError {
  return isApiError(error) && (error.status === 429 || THROTTLED_CODES.includes(error.code));
}

function lockOrUnavailable(error: unknown): PortalPhase {
  if (isFeaturePending(error)) {
    return 'pending';
  }

  if (isApiError(error) && error.status === 423) {
    return 'locked';
  }

  return isApiError(error) && [401, 404, 410].includes(error.status) ? 'unavailable' : 'error';
}

export const PortalStore = signalStore(
  withState<PortalState>({
    phase: 'checking',
    token: '',
    attemptsLeft: 3,
    digitsError: null,
    session: null,
    order: null,
    selected: new Set<string>(),
    busy: false,
    notice: null,
    cardNotice: null,
    pix: null,
    pixStatus: null,
  }),
  withComputed((store) => ({
    steps: computed(() => {
      const order = store.order();
      return order ? portalSteps(order) : [];
    }),
    totals: computed(() => {
      const quote = store.order()?.quote;
      return quote ? portalTotal(quote.items, store.selected(), quote.discount) : null;
    }),
    canAnswer: computed(() => {
      const order = store.order();
      return order?.status === 'WaitingApproval' && order.quote?.status === 'Pending';
    }),
  })),
  withMethods((store) => {
    const gateway = inject(PortalGateway);
    const pollMs = inject(PORTAL_POLL_MS);
    let timer: ReturnType<typeof setTimeout> | null = null;
    let polls = 0;

    const stopPolling = (): void => {
      if (timer !== null) {
        clearTimeout(timer);
        timer = null;
      }
    };

    const setOrder = (order: PortalOrder): void => {
      patchState(store, {
        order,
        selected: new Set(order.quote?.items.map((item) => item.id) ?? []),
      });
    };

    const fail = (error: unknown): void => {
      if (isApiError(error) && error.status === 401) {
        patchState(store, { phase: 'confirm', session: null, order: null });
        return;
      }

      patchState(store, {
        notice: { tone: 'bad', text: isApiError(error) ? error.detail : messages.errors.unknown },
      });
    };

    const reload = async (): Promise<void> => {
      const session = store.session();
      if (session) {
        setOrder(await firstValueFrom(gateway.order(session)));
      }
    };

    const poll = (): void => {
      stopPolling();
      timer = setTimeout(() => void check(), pollMs);
    };

    const check = async (): Promise<void> => {
      const session = store.session();
      const pix = store.pix();
      if (!session || !pix) {
        return;
      }

      try {
        const status = await firstValueFrom(gateway.charge(session, pix.paymentId));
        patchState(store, { pixStatus: status });
        polls += 1;
        if (status === 'Paid') {
          patchState(store, { notice: { tone: 'ok', text: text.pixPaid } });
          await reload();
        } else if (status === 'Pending' && polls < MAX_PIX_POLLS) {
          poll();
        }
      } catch (error) {
        fail(error);
      }
    };

    return {
      async start(token: string): Promise<void> {
        stopPolling();
        patchState(store, {
          phase: 'checking',
          token,
          session: null,
          order: null,
          notice: null,
          pix: null,
        });
        try {
          const attemptsLeft = await firstValueFrom(gateway.access(token));
          patchState(store, { phase: 'confirm', attemptsLeft });
        } catch (error) {
          patchState(store, { phase: lockOrUnavailable(error) });
        }
      },

      async confirm(digits: string): Promise<void> {
        if (!/^\d{3}$/.test(digits)) {
          patchState(store, { digitsError: text.digitsInvalid });
          return;
        }

        patchState(store, { busy: true, digitsError: null });
        try {
          const result = await firstValueFrom(gateway.confirm(store.token(), digits));
          if (result.kind === 'wrongDigits') {
            if (result.attemptsLeft <= 0) {
              patchState(store, { phase: 'locked', attemptsLeft: 0, digitsError: null });
              return;
            }

            patchState(store, {
              attemptsLeft: result.attemptsLeft,
              digitsError: text.wrongDigits(result.attemptsLeft),
            });
            return;
          }

          patchState(store, { session: result.session });
          await reload();
          patchState(store, { phase: 'ready' });
        } catch (error) {
          if (isThrottled(error)) {
            patchState(store, { digitsError: error.detail });
            return;
          }

          patchState(store, { phase: lockOrUnavailable(error) });
        } finally {
          patchState(store, { busy: false });
        }
      },

      toggle(itemId: string, checked: boolean): void {
        const selected = new Set(store.selected());
        if (checked) {
          selected.add(itemId);
        } else {
          selected.delete(itemId);
        }

        patchState(store, { selected });
      },

      async approve(): Promise<boolean> {
        const session = store.session();
        if (!session) {
          return false;
        }

        if (store.selected().size === 0) {
          patchState(store, { notice: { tone: 'bad', text: text.approveNone } });
          return false;
        }

        patchState(store, { busy: true, notice: null });
        try {
          setOrder(await firstValueFrom(gateway.approve(session, [...store.selected()])));
          patchState(store, { notice: { tone: 'ok', text: text.approved } });
          return true;
        } catch (error) {
          if (isApiError(error) && error.status === 409) {
            await reload();
            patchState(store, { notice: { tone: 'info', text: text.alreadyAnswered } });
          } else {
            fail(error);
          }

          return false;
        } finally {
          patchState(store, { busy: false });
        }
      },

      async reject(reason: string): Promise<boolean> {
        const session = store.session();
        if (!session) {
          return false;
        }

        patchState(store, { busy: true, notice: null });
        try {
          setOrder(await firstValueFrom(gateway.reject(session, reason)));
          patchState(store, { notice: { tone: 'ok', text: text.rejected } });
          return true;
        } catch (error) {
          if (isApiError(error) && error.status === 409) {
            await reload();
            patchState(store, { notice: { tone: 'info', text: text.alreadyAnswered } });
          } else {
            fail(error);
          }

          return false;
        } finally {
          patchState(store, { busy: false });
        }
      },

      async payPix(): Promise<void> {
        const session = store.session();
        if (!session) {
          return;
        }

        patchState(store, { busy: true, notice: null });
        try {
          const pix = await firstValueFrom(gateway.pix(session));
          polls = 0;
          patchState(store, { pix, pixStatus: 'Pending' });
          poll();
        } catch (error) {
          fail(error);
        } finally {
          patchState(store, { busy: false });
        }
      },

      check,

      async payCard(): Promise<string | null> {
        const session = store.session();
        if (!session) {
          return null;
        }

        try {
          return await firstValueFrom(gateway.card(session));
        } catch (error) {
          fail(error);
          return null;
        }
      },

      cardReturned(result: string | null): void {
        if (result === 'declined') {
          patchState(store, { cardNotice: { tone: 'bad', text: text.cardDeclined } });
        } else if (result === 'returned') {
          patchState(store, { cardNotice: { tone: 'info', text: text.cardReturned } });
        }
      },

      stop: stopPolling,
    };
  }),
  withHooks({
    onDestroy(store) {
      store.stop();
    },
  }),
);

export type PortalStore = InstanceType<typeof PortalStore>;

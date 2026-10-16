import { computed, DestroyRef, inject, InjectionToken } from '@angular/core';
import {
  patchState,
  signalStore,
  withComputed,
  withHooks,
  withMethods,
  withState,
} from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { RealtimeGateway } from '../../../core/realtime/realtime.gateway';
import { ServiceOrderGateway } from '../data/service-order.gateway';
import { ServiceOrderCard, ServiceOrderStatus } from '../data/service-order.models';
import {
  CardFilters,
  ChangedField,
  changedFields,
  checkTransition,
  filterCards,
  mechanicsOf,
  NO_FILTERS,
  TransitionBlock,
} from '../data/service-order.rules';

export const SERVICE_ORDER_POLL_MS = new InjectionToken<number>('SERVICE_ORDER_POLL_MS', {
  providedIn: 'root',
  factory: () => 15_000,
});

export const VERSION_CONFLICT_CODE = 'serviceOrders.versionConflict';

export type BoardLoad = 'loading' | 'ready' | 'pending' | 'error';

export interface OrderChange {
  readonly kind: 'conflict' | 'remote';
  readonly number: number;
  readonly before: ServiceOrderCard;
  readonly after: ServiceOrderCard;
  readonly fields: readonly ChangedField[];
}

export type MoveResult =
  | { readonly kind: 'moved' }
  | { readonly kind: 'blocked'; readonly block: TransitionBlock }
  | { readonly kind: 'needsReason' }
  | { readonly kind: 'conflict' }
  | { readonly kind: 'failed' };

interface BoardState {
  readonly loadState: BoardLoad;
  readonly cards: readonly ServiceOrderCard[];
  readonly filters: CardFilters;
  readonly moving: string | null;
  readonly changes: readonly OrderChange[];
  readonly error: string | null;
  readonly now: number;
}

export function errorText(error: unknown): string {
  return isApiError(error) ? error.detail : messages.errors.unknown;
}

export const BoardStore = signalStore(
  withState<BoardState>({
    loadState: 'loading',
    cards: [],
    filters: NO_FILTERS,
    moving: null,
    changes: [],
    error: null,
    now: Date.now(),
  }),
  withComputed((store) => ({
    visible: computed(() => filterCards(store.cards(), store.filters(), store.now())),
    mechanics: computed(() => mechanicsOf(store.cards())),
  })),
  withMethods((store) => {
    const gateway = inject(ServiceOrderGateway);

    const replace = (card: ServiceOrderCard): void => {
      patchState(store, {
        cards: store.cards().map((item) => (item.id === card.id ? card : item)),
      });
    };

    const noteChange = (
      kind: OrderChange['kind'],
      before: ServiceOrderCard,
      after: ServiceOrderCard,
    ): void => {
      const fields = changedFields(before, after);
      if (kind === 'remote' && fields.length === 0) {
        return;
      }

      patchState(store, {
        changes: [
          { kind, number: after.number, before, after, fields },
          ...store.changes().filter((change) => change.number !== after.number),
        ],
      });
    };

    const reloadCard = async (id: string, kind: OrderChange['kind']): Promise<void> => {
      const before = store.cards().find((card) => card.id === id);
      try {
        const after = await firstValueFrom(gateway.get(id));
        if (before) {
          noteChange(kind, before, after);
          replace(after);
        } else {
          patchState(store, { cards: [after, ...store.cards()] });
        }
      } catch (error) {
        patchState(store, { error: errorText(error) });
      }
    };

    return {
      async load(): Promise<void> {
        patchState(store, { loadState: 'loading', error: null, now: Date.now() });
        try {
          const cards = await firstValueFrom(gateway.board());
          patchState(store, { cards, loadState: 'ready' });
        } catch (error) {
          patchState(store, { loadState: isFeaturePending(error) ? 'pending' : 'error' });
        }
      },

      async refresh(): Promise<void> {
        if (store.loadState() !== 'ready') {
          return;
        }

        try {
          const cards = await firstValueFrom(gateway.board());
          const previous = new Map(store.cards().map((card) => [card.id, card]));
          for (const card of cards) {
            const before = previous.get(card.id);
            if (before && before.version !== card.version) {
              noteChange('remote', before, card);
            }
          }

          patchState(store, { cards, now: Date.now() });
        } catch {
          return;
        }
      },

      reloadCard,

      setFilters(changes: Partial<CardFilters>): void {
        patchState(store, { filters: { ...store.filters(), ...changes } });
      },

      async move(
        id: string,
        to: ServiceOrderStatus,
        reason: string | null = null,
      ): Promise<MoveResult> {
        const card = store.cards().find((item) => item.id === id);
        if (!card) {
          return { kind: 'failed' };
        }

        const check = checkTransition(card, to);
        if (!check.allowed) {
          return { kind: 'blocked', block: check.block };
        }

        if (check.needsReason && (reason === null || reason.trim().length === 0)) {
          return { kind: 'needsReason' };
        }

        patchState(store, { moving: id, error: null });
        try {
          const updated = await firstValueFrom(
            gateway.transition(id, { to, version: card.version, reason }),
          );
          replace(updated);
          return { kind: 'moved' };
        } catch (error) {
          if (isApiError(error) && error.code === VERSION_CONFLICT_CODE) {
            await reloadCard(id, 'conflict');
            return { kind: 'conflict' };
          }

          patchState(store, { error: errorText(error) });
          return { kind: 'failed' };
        } finally {
          patchState(store, { moving: null });
        }
      },

      dismissChange(number: number): void {
        patchState(store, {
          changes: store.changes().filter((change) => change.number !== number),
        });
      },

      dismissError(): void {
        patchState(store, { error: null });
      },
    };
  }),
  withHooks({
    onInit(store) {
      const realtime = inject(RealtimeGateway);
      const pollMs = inject(SERVICE_ORDER_POLL_MS);
      const destroyRef = inject(DestroyRef);
      void realtime.connect();
      const subscription = realtime.events$.subscribe((event) => {
        if (event.type === 'serviceOrderStatusChanged') {
          void store.reloadCard(event.serviceOrderId, 'remote');
        }
      });
      const timer = setInterval(() => {
        if (realtime.status() !== 'connected') {
          void store.refresh();
        }
      }, pollMs);
      destroyRef.onDestroy(() => {
        subscription.unsubscribe();
        clearInterval(timer);
      });
      void store.load();
    },
  }),
);

export type BoardStore = InstanceType<typeof BoardStore>;

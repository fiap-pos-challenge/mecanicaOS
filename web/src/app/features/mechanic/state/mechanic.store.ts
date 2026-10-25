import { DOCUMENT } from '@angular/common';
import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { SessionStateRegistry } from '../../../core/auth/session-state.registry';
import {
  isApiError,
  isFeaturePending,
  NETWORK_ERROR_CODE,
  TIMEOUT_ERROR_CODE,
} from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { OfflineQueueStorage } from '../../../core/pwa/offline-queue.storage';
import { AuthStore } from '../../auth/state/auth.store';
import { MechanicGateway } from '../data/mechanic.gateway';
import { AiSuggestedItem, MechanicOrder, QueuedAction, WorkAction } from '../data/mechanic.models';

export type MechanicLoad = 'loading' | 'ready' | 'pending' | 'error';

export interface SyncFailure {
  readonly id: string;
  readonly text: string;
}

export function isOfflineError(error: unknown): boolean {
  return (
    isApiError(error) &&
    (error.status === 0 || error.code === NETWORK_ERROR_CODE || error.code === TIMEOUT_ERROR_CODE)
  );
}

export function applyAction(order: MechanicOrder, action: WorkAction, now: string): MechanicOrder {
  switch (action.kind) {
    case 'checklist':
      return { ...order, checklist: action.items };
    case 'photo':
      return {
        ...order,
        photos: [
          ...order.photos,
          {
            id: action.photo.id,
            url: action.photo.dataUrl,
            caption: action.photo.fileName,
            takenAt: now,
            pending: true,
          },
        ],
      };
    case 'diagnosis':
      return { ...order, diagnosis: action.description };
    case 'sendToQuote':
      return { ...order, diagnosis: action.request.description, sentToQuote: true };
    case 'start':
      return order.status === 'Received'
        ? { ...order, status: 'InDiagnosis' }
        : { ...order, timerRunning: true };
    case 'finish':
      return { ...order, status: 'Completed', timerRunning: false };
  }
}

export const MECHANIC_QUEUE_KEY = 'mechanic';

const COALESCED: readonly WorkAction['kind'][] = ['checklist', 'diagnosis'];

let nextAction = 0;

@Injectable()
export class MechanicStore {
  private readonly gateway = inject(MechanicGateway);
  private readonly window = inject(DOCUMENT).defaultView;
  private readonly storage = inject(OfflineQueueStorage);
  private readonly owner = inject(AuthStore).user()?.id ?? null;

  readonly orders = signal<Readonly<Record<string, MechanicOrder>>>({});
  readonly mineIds = signal<readonly string[]>([]);
  readonly listState = signal<MechanicLoad>('loading');
  readonly online = signal(this.window?.navigator.onLine ?? true);
  readonly queue = signal<readonly QueuedAction[]>([]);
  readonly failures = signal<readonly SyncFailure[]>([]);
  readonly syncing = signal(false);
  readonly restored: Promise<void>;
  readonly mine = computed(() =>
    this.mineIds().flatMap((id) => {
      const order = this.orders()[id];
      return order ? [order] : [];
    }),
  );
  readonly pendingByOrder = computed(() =>
    this.queue().reduce<Record<string, number>>(
      (counts, item) => ({ ...counts, [item.orderId]: (counts[item.orderId] ?? 0) + 1 }),
      {},
    ),
  );

  constructor() {
    this.window?.addEventListener('online', () => {
      this.online.set(true);
      void this.flush();
    });
    this.window?.addEventListener('offline', () => this.online.set(false));
    inject(SessionStateRegistry).register(() => this.reset());
    this.restored = this.restore();
  }

  async loadMine(): Promise<void> {
    await this.restored;
    if (this.mineIds().length === 0) {
      this.listState.set('loading');
    }

    try {
      const orders = await firstValueFrom(this.gateway.mine());
      this.orders.update((current) => ({
        ...current,
        ...Object.fromEntries(orders.map((order) => [order.id, this.withQueued(order)])),
      }));
      this.mineIds.set(orders.map((order) => order.id));
      this.listState.set('ready');
    } catch (error) {
      if (isOfflineError(error) && this.mineIds().length > 0) {
        this.online.set(false);
        this.listState.set('ready');
      } else {
        this.listState.set(isFeaturePending(error) ? 'pending' : 'error');
      }
    }
  }

  async loadOrder(id: string): Promise<MechanicOrder> {
    await this.restored;
    const cached = this.orders()[id];
    if (cached && (!this.online() || this.queue().some((item) => item.orderId === id))) {
      return cached;
    }

    try {
      const order = this.withQueued(await firstValueFrom(this.gateway.work(id)));
      this.orders.update((current) => ({ ...current, [id]: order }));
      return order;
    } catch (error) {
      if (cached && isOfflineError(error)) {
        this.online.set(false);
        return cached;
      }

      throw error;
    }
  }

  async suggest(id: string, description: string): Promise<readonly AiSuggestedItem[]> {
    return firstValueFrom(this.gateway.suggest(id, description));
  }

  async perform(orderId: string, action: WorkAction): Promise<'done' | 'queued' | 'failed'> {
    const order = this.orders()[orderId];
    if (!order) {
      return 'failed';
    }

    this.put(applyAction(order, action, new Date().toISOString()));
    const queued: QueuedAction = {
      id: `action-${Date.now()}-${++nextAction}`,
      orderId,
      orderNumber: order.number,
      action,
      createdAt: new Date().toISOString(),
    };
    if (!this.online() || this.queue().length > 0) {
      this.enqueue(queued);
      void this.flush();
      return 'queued';
    }

    return this.execute(queued);
  }

  async flush(): Promise<void> {
    if (this.syncing() || !this.online()) {
      return;
    }

    this.syncing.set(true);
    try {
      for (const item of this.queue()) {
        const result = await this.execute(item, true);
        if (result === 'queued') {
          return;
        }
      }
    } finally {
      this.syncing.set(false);
    }
  }

  dismissFailure(id: string): void {
    this.failures.update((items) => items.filter((item) => item.id !== id));
  }

  reset(): void {
    this.orders.set({});
    this.mineIds.set([]);
    this.queue.set([]);
    this.failures.set([]);
    this.listState.set('loading');
  }

  label(item: QueuedAction): string {
    return messages.mechanic.sync.item(
      item.orderNumber,
      messages.mechanic.sync.labels[item.action.kind],
    );
  }

  private async execute(
    item: QueuedAction,
    fromQueue = false,
  ): Promise<'done' | 'queued' | 'failed'> {
    try {
      const updated = await firstValueFrom(this.gateway.perform(item.orderId, item.action));
      if (fromQueue) {
        this.updateQueue((items) => items.filter((queued) => queued.id !== item.id));
      }

      this.put(this.withQueued(updated));
      return 'done';
    } catch (error) {
      if (isOfflineError(error)) {
        this.online.set(false);
        if (!fromQueue) {
          this.enqueue(item);
        }

        return 'queued';
      }

      if (fromQueue) {
        this.updateQueue((items) => items.filter((queued) => queued.id !== item.id));
      }

      this.failures.update((items) => [
        ...items,
        {
          id: item.id,
          text: messages.mechanic.sync.failed(
            this.label(item),
            isApiError(error) ? error.detail : messages.errors.unknown,
          ),
        },
      ]);
      return 'failed';
    }
  }

  private async restore(): Promise<void> {
    if (this.owner === null) {
      return;
    }

    const stored = await this.storage
      .load<QueuedAction>(MECHANIC_QUEUE_KEY, this.owner)
      .catch(() => []);
    if (stored.length > 0) {
      this.queue.update((items) => [
        ...stored.filter((item) => !items.some((queued) => queued.id === item.id)),
        ...items,
      ]);
      this.orders.update((current) =>
        Object.fromEntries(
          Object.entries(current).map(([id, order]) => [id, this.withQueued(order)]),
        ),
      );
      void this.flush();
    }
  }

  private updateQueue(change: (items: readonly QueuedAction[]) => readonly QueuedAction[]): void {
    this.queue.update(change);
    if (this.owner !== null) {
      void this.storage.save(MECHANIC_QUEUE_KEY, this.owner, this.queue()).catch(() => undefined);
    }
  }

  private enqueue(item: QueuedAction): void {
    this.updateQueue((items) => [
      ...items.filter(
        (queued) =>
          !(
            queued.orderId === item.orderId &&
            queued.action.kind === item.action.kind &&
            COALESCED.includes(item.action.kind)
          ),
      ),
      item,
    ]);
  }

  private put(order: MechanicOrder): void {
    this.orders.update((current) => ({ ...current, [order.id]: order }));
  }

  private withQueued(order: MechanicOrder): MechanicOrder {
    return this.queue()
      .filter((item) => item.orderId === order.id)
      .reduce((current, item) => {
        const action = item.action;
        if (action.kind === 'photo') {
          const photoId = action.photo.id;
          if (current.photos.some((photo) => photo.id === photoId)) {
            return current;
          }
        }

        return applyAction(current, action, item.createdAt);
      }, order);
  }
}

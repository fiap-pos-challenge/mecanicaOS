import { DOCUMENT } from '@angular/common';
import { inject, Injectable, InjectionToken } from '@angular/core';

export const OFFLINE_DATABASE = 'oficina-offline';
const QUEUE_STORE = 'queues';
const DATABASE_VERSION = 1;

interface StoredQueue {
  readonly owner: string;
  readonly items: readonly unknown[];
}

export const OFFLINE_INDEXED_DB = new InjectionToken<IDBFactory | null>('OFFLINE_INDEXED_DB', {
  providedIn: 'root',
  factory: () => inject(DOCUMENT).defaultView?.indexedDB ?? null,
});

function request<T>(operation: IDBRequest<T>): Promise<T> {
  return new Promise((resolve, reject) => {
    operation.onsuccess = () => resolve(operation.result);
    operation.onerror = () => reject(operation.error);
  });
}

@Injectable({ providedIn: 'root' })
export class OfflineQueueStorage {
  private readonly factory = inject(OFFLINE_INDEXED_DB);
  private database: Promise<IDBDatabase> | null = null;

  async load<T>(key: string, owner: string): Promise<readonly T[]> {
    const stored = await this.run<StoredQueue | undefined>('readonly', (store) => store.get(key));
    if (!stored) {
      return [];
    }

    if (stored.owner !== owner) {
      await this.run('readwrite', (store) => store.delete(key));
      return [];
    }

    return stored.items as readonly T[];
  }

  async save<T>(key: string, owner: string, items: readonly T[]): Promise<void> {
    if (items.length === 0) {
      await this.run('readwrite', (store) => store.delete(key));
      return;
    }

    await this.run('readwrite', (store) => store.put({ owner, items }, key));
  }

  async clear(): Promise<void> {
    const factory = this.factory;
    if (!factory) {
      return;
    }

    const database = this.database;
    this.database = null;
    if (database) {
      (await database.catch(() => null))?.close();
    }

    await request(factory.deleteDatabase(OFFLINE_DATABASE)).catch(() => undefined);
  }

  private async run<T>(
    mode: IDBTransactionMode,
    operation: (store: IDBObjectStore) => IDBRequest<T>,
  ): Promise<T | undefined> {
    const database = await this.open();
    if (!database) {
      return undefined;
    }

    return request(operation(database.transaction(QUEUE_STORE, mode).objectStore(QUEUE_STORE)));
  }

  private open(): Promise<IDBDatabase | null> {
    const factory = this.factory;
    if (!factory) {
      return Promise.resolve(null);
    }

    this.database ??= new Promise<IDBDatabase>((resolve, reject) => {
      const opening = factory.open(OFFLINE_DATABASE, DATABASE_VERSION);
      opening.onupgradeneeded = () => opening.result.createObjectStore(QUEUE_STORE);
      opening.onsuccess = () => resolve(opening.result);
      opening.onerror = () => reject(opening.error);
    });
    return this.database;
  }
}

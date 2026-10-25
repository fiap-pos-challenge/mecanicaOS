import { computed, Injectable, signal } from '@angular/core';

export interface StoredSession {
  readonly accessToken: string;
  readonly expiresAt: string;
}

@Injectable({ providedIn: 'root' })
export class AuthTokenStore {
  private readonly session = signal<StoredSession | null>(null);

  readonly token = computed(() => this.session()?.accessToken ?? null);
  readonly expiresAt = computed(() => this.session()?.expiresAt ?? null);

  set(session: StoredSession): void {
    this.session.set(session);
  }

  clear(): void {
    this.session.set(null);
  }

  isExpired(now: Date = new Date()): boolean {
    const expiresAt = this.expiresAt();
    return expiresAt === null || new Date(expiresAt).getTime() <= now.getTime();
  }
}

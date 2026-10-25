import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { finalize, Observable, shareReplay, Subject, tap } from 'rxjs';
import { AuthTokenStore, StoredSession } from './auth-token.store';
import { SessionGateway } from './session.gateway';
import { SessionStateRegistry } from './session-state.registry';

export const LOGIN_ROUTE = '/login';

export type SessionEndReason = 'expired' | 'inactivity' | 'logout';

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly tokens = inject(AuthTokenStore);
  private readonly router = inject(Router);
  private readonly gateway = inject(SessionGateway);
  private readonly registry = inject(SessionStateRegistry);
  private readonly endedSubject = new Subject<SessionEndReason>();
  private renewal: Observable<StoredSession | null> | null = null;

  readonly endReason = signal<SessionEndReason | null>(null);
  readonly ended$ = this.endedSubject.asObservable();

  renew(): Observable<StoredSession | null> {
    this.renewal ??= this.gateway.refresh().pipe(
      tap((session) => {
        if (session) {
          this.tokens.set(session);
        }
      }),
      finalize(() => (this.renewal = null)),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    return this.renewal;
  }

  expire(): void {
    this.end('expired');
  }

  logout(reason: Exclude<SessionEndReason, 'expired'> = 'logout'): void {
    this.gateway.logout().subscribe();
    this.end(reason);
  }

  acknowledge(): void {
    this.endReason.set(null);
  }

  private end(reason: SessionEndReason): void {
    this.tokens.clear();
    if (reason !== 'expired') {
      this.registry.resetAll();
    }

    this.endReason.set(reason);
    this.endedSubject.next(reason);
    void this.router.navigateByUrl(LOGIN_ROUTE);
  }
}

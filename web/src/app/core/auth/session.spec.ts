import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthTokenStore } from './auth-token.store';
import { Observable, of, Subject } from 'rxjs';
import { StoredSession } from './auth-token.store';
import { SessionGateway } from './session.gateway';
import { LOGIN_ROUTE, SessionService } from './session.service';
import { SessionStateRegistry } from './session-state.registry';

describe('AuthTokenStore', () => {
  it('keeps the token only in memory and checks expiry', () => {
    const store = TestBed.inject(AuthTokenStore);
    expect(store.isExpired()).toBe(true);

    store.set({ accessToken: 'abc', expiresAt: '2026-10-04T12:00:00Z' });

    expect(store.token()).toBe('abc');
    expect(store.isExpired(new Date('2026-10-04T11:59:59Z'))).toBe(false);
    expect(store.isExpired(new Date('2026-10-04T12:00:00Z'))).toBe(true);
    store.clear();
    expect(store.token()).toBeNull();
    expect(store.expiresAt()).toBeNull();
  });
});

describe('SessionService', () => {
  class FakeSessionGateway extends SessionGateway {
    readonly refreshes: Subject<StoredSession | null>[] = [];
    logouts = 0;

    refresh(): Observable<StoredSession | null> {
      const subject = new Subject<StoredSession | null>();
      this.refreshes.push(subject);
      return subject;
    }

    logout(): Observable<void> {
      this.logouts += 1;
      return of(undefined);
    }
  }

  function setup(): {
    session: SessionService;
    gateway: FakeSessionGateway;
    tokens: AuthTokenStore;
    navigate: ReturnType<typeof vi.spyOn>;
  } {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: SessionGateway, useClass: FakeSessionGateway }],
    });
    return {
      session: TestBed.inject(SessionService),
      gateway: TestBed.inject(SessionGateway) as FakeSessionGateway,
      tokens: TestBed.inject(AuthTokenStore),
      navigate: vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true),
    };
  }

  it('shares one renewal between concurrent callers and stores the new token', () => {
    const { session, gateway, tokens } = setup();
    const results: (StoredSession | null)[] = [];

    session.renew().subscribe((value) => results.push(value));
    session.renew().subscribe((value) => results.push(value));
    gateway.refreshes[0]?.next({ accessToken: 'new', expiresAt: '2099-01-01T00:00:00Z' });
    gateway.refreshes[0]?.complete();

    expect(gateway.refreshes).toHaveLength(1);
    expect(results.map((value) => value?.accessToken)).toEqual(['new', 'new']);
    expect(tokens.token()).toBe('new');
    session.renew().subscribe();
    expect(gateway.refreshes).toHaveLength(2);
  });

  it('keeps the token untouched when renewal fails', () => {
    const { session, gateway, tokens } = setup();
    tokens.set({ accessToken: 'old', expiresAt: '2099-01-01T00:00:00Z' });

    session.renew().subscribe();
    gateway.refreshes[0]?.next(null);
    gateway.refreshes[0]?.complete();

    expect(tokens.token()).toBe('old');
  });

  it('expires the session keeping the per-session state', () => {
    const { session, tokens, navigate } = setup();
    const reset = vi.fn();
    TestBed.inject(SessionStateRegistry).register(reset);
    const reasons: string[] = [];
    session.ended$.subscribe((reason) => reasons.push(reason));
    tokens.set({ accessToken: 'abc', expiresAt: '2099-01-01T00:00:00Z' });

    session.expire();

    expect(tokens.token()).toBeNull();
    expect(session.endReason()).toBe('expired');
    expect(reasons).toEqual(['expired']);
    expect(reset).not.toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith(LOGIN_ROUTE);
    session.acknowledge();
    expect(session.endReason()).toBeNull();
  });

  it('logs out clearing every registered state and revoking the session', () => {
    const { session, gateway } = setup();
    const reset = vi.fn();
    const unregister = TestBed.inject(SessionStateRegistry).register(reset);

    session.logout();
    session.logout('inactivity');
    unregister();
    session.logout();

    expect(reset).toHaveBeenCalledTimes(2);
    expect(gateway.logouts).toBe(3);
    expect(session.endReason()).toBe('logout');
  });
});

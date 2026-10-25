import { TestBed } from '@angular/core/testing';
import type { HubConnection } from '@microsoft/signalr';
import { AuthTokenStore } from '../auth/auth-token.store';
import { API_ORIGIN } from '../config/app-environment';
import { PendingRealtimeGateway } from './pending-realtime.gateway';
import { RealtimeEvent } from './realtime.gateway';
import {
  HUB_CONNECTION_FACTORY,
  HubConnectionFactory,
  SignalRRealtimeGateway,
} from './signalr-realtime.gateway';

class FakeHubConnection {
  readonly handlers = new Map<string, (event: RealtimeEvent) => void>();
  reconnecting: () => void = () => undefined;
  reconnected: () => void = () => undefined;
  closed: () => void = () => undefined;
  start = vi.fn(async () => undefined);
  stop = vi.fn(async () => undefined);

  on(name: string, handler: (event: RealtimeEvent) => void): void {
    this.handlers.set(name, handler);
  }

  onreconnecting(handler: () => void): void {
    this.reconnecting = handler;
  }

  onreconnected(handler: () => void): void {
    this.reconnected = handler;
  }

  onclose(handler: () => void): void {
    this.closed = handler;
  }
}

describe('SignalRRealtimeGateway', () => {
  function setup(): {
    gateway: SignalRRealtimeGateway;
    hub: FakeHubConnection;
    factory: ReturnType<typeof vi.fn>;
  } {
    const hub = new FakeHubConnection();
    const factory = vi.fn<HubConnectionFactory>(async () => hub as unknown as HubConnection);
    TestBed.configureTestingModule({
      providers: [
        { provide: HUB_CONNECTION_FACTORY, useValue: factory },
        { provide: API_ORIGIN, useValue: 'https://oficina.test' },
      ],
    });
    return { gateway: TestBed.inject(SignalRRealtimeGateway), hub, factory };
  }

  it('connects to the hub with the in-memory token', async () => {
    const { gateway, hub, factory } = setup();
    TestBed.inject(AuthTokenStore).set({ accessToken: 'abc', expiresAt: '2099-01-01T00:00:00Z' });

    await gateway.connect();

    expect(factory.mock.calls[0]?.[0]).toBe('https://oficina.test/api/hubs/realtime');
    expect(factory.mock.calls[0]?.[1]()).toBe('abc');
    expect(hub.start).toHaveBeenCalled();
    expect(gateway.status()).toBe('connected');
  });

  it('sends empty token without session and disconnects before connecting', async () => {
    const { gateway, factory } = setup();
    await gateway.disconnect();
    expect(factory).not.toHaveBeenCalled();
    await gateway.connect();

    expect(factory.mock.calls[0]?.[1]()).toBe('');
  });

  it('emits hub events and tracks reconnection', async () => {
    const { gateway, hub } = setup();
    await gateway.connect();
    const events: RealtimeEvent[] = [];
    gateway.events$.subscribe((event) => events.push(event));
    const event: RealtimeEvent = {
      type: 'exportReady',
      exportId: '1',
      occurredAt: '2026-10-04T12:00:00Z',
    };

    hub.handlers.get('event')?.(event);
    hub.reconnecting();
    expect(gateway.status()).toBe('connecting');
    hub.reconnected();
    expect(gateway.status()).toBe('connected');
    hub.closed();
    expect(gateway.status()).toBe('disconnected');
    expect(events).toEqual([event]);
    await gateway.disconnect();
    expect(hub.stop).toHaveBeenCalled();
  });

  it('goes back to disconnected when the hub fails to start', async () => {
    const { gateway, hub } = setup();
    hub.start.mockRejectedValueOnce(new Error('offline'));

    await expect(gateway.connect()).rejects.toThrow('offline');
    expect(gateway.status()).toBe('disconnected');
  });

  it('builds a real hub connection by default', async () => {
    const connection = await TestBed.inject(HUB_CONNECTION_FACTORY)(
      'http://localhost/api/hubs/realtime',
      () => '',
    );

    expect(connection.state).toBe('Disconnected');
  });
});

describe('PendingRealtimeGateway', () => {
  it('stays disconnected without events while the hub does not exist', async () => {
    const gateway = TestBed.inject(PendingRealtimeGateway);
    const events: RealtimeEvent[] = [];
    gateway.events$.subscribe((event) => events.push(event));

    await gateway.connect();
    await gateway.disconnect();

    expect(gateway.status()).toBe('disconnected');
    expect(events).toEqual([]);
  });
});

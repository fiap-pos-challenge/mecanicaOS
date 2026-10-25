import { inject, Injectable, InjectionToken, signal } from '@angular/core';
import type { HubConnection } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthTokenStore } from '../auth/auth-token.store';
import { API_ORIGIN, APP_ENVIRONMENT } from '../config/app-environment';
import {
  REALTIME_EVENT_NAME,
  RealtimeEvent,
  RealtimeGateway,
  RealtimeStatus,
} from './realtime.gateway';

export type HubConnectionFactory = (
  url: string,
  accessToken: () => string,
) => Promise<HubConnection>;

export const HUB_CONNECTION_FACTORY = new InjectionToken<HubConnectionFactory>(
  'HUB_CONNECTION_FACTORY',
  {
    providedIn: 'root',
    factory: () => async (url, accessToken) => {
      const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');
      return new HubConnectionBuilder()
        .withUrl(url, { accessTokenFactory: accessToken })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.None)
        .build();
    },
  },
);

@Injectable({ providedIn: 'root' })
export class SignalRRealtimeGateway extends RealtimeGateway {
  private readonly tokens = inject(AuthTokenStore);
  private readonly factory = inject(HUB_CONNECTION_FACTORY);
  private readonly url = `${inject(API_ORIGIN)}${inject(APP_ENVIRONMENT).realtimeHubPath}`;
  private readonly subject = new Subject<RealtimeEvent>();
  private readonly state = signal<RealtimeStatus>('disconnected');
  private connection: Promise<HubConnection> | null = null;

  readonly status = this.state.asReadonly();
  readonly events$ = this.subject.asObservable();

  async connect(): Promise<void> {
    this.state.set('connecting');
    try {
      await (await this.hub()).start();
      this.state.set('connected');
    } catch (error) {
      this.state.set('disconnected');
      throw error;
    }
  }

  async disconnect(): Promise<void> {
    await (await this.connection)?.stop();
    this.state.set('disconnected');
  }

  private hub(): Promise<HubConnection> {
    this.connection ??= this.factory(this.url, () => this.tokens.token() ?? '').then((hub) => {
      hub.on(REALTIME_EVENT_NAME, (event: RealtimeEvent) => this.subject.next(event));
      hub.onreconnecting(() => this.state.set('connecting'));
      hub.onreconnected(() => this.state.set('connected'));
      hub.onclose(() => this.state.set('disconnected'));
      return hub;
    });
    return this.connection;
  }
}

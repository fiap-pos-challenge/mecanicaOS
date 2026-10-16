import { Injectable, signal } from '@angular/core';
import { EMPTY } from 'rxjs';
import { RealtimeGateway, RealtimeStatus } from './realtime.gateway';

@Injectable({ providedIn: 'root' })
export class PendingRealtimeGateway extends RealtimeGateway {
  readonly status = signal<RealtimeStatus>('disconnected').asReadonly();
  readonly events$ = EMPTY;

  async connect(): Promise<void> {
    return Promise.resolve();
  }

  async disconnect(): Promise<void> {
    return Promise.resolve();
  }
}

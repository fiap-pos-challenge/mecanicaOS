import { Signal } from '@angular/core';
import { Observable } from 'rxjs';

export type ServiceOrderStatus =
  'Received' | 'InDiagnosis' | 'WaitingApproval' | 'InProgress' | 'Completed' | 'Delivered';

export type RealtimeEvent =
  | {
      readonly type: 'serviceOrderStatusChanged';
      readonly serviceOrderId: string;
      readonly number: number;
      readonly status: ServiceOrderStatus;
      readonly occurredAt: string;
    }
  | {
      readonly type: 'paymentConfirmed';
      readonly serviceOrderId: string;
      readonly number: number;
      readonly amount: number;
      readonly occurredAt: string;
    }
  | {
      readonly type: 'exportReady';
      readonly exportId: string;
      readonly occurredAt: string;
    }
  | {
      readonly type: 'notification';
      readonly notification: Readonly<Record<string, unknown>>;
      readonly occurredAt: string;
    };

export type RealtimeStatus = 'disconnected' | 'connecting' | 'connected';

export const REALTIME_EVENT_NAME = 'event';

export abstract class RealtimeGateway {
  abstract readonly status: Signal<RealtimeStatus>;
  abstract readonly events$: Observable<RealtimeEvent>;
  abstract connect(): Promise<void>;
  abstract disconnect(): Promise<void>;
}

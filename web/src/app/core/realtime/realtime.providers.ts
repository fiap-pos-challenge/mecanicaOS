import { Provider } from '@angular/core';
import { provideGateway } from '../config/provide-gateway';
import { PendingRealtimeGateway } from './pending-realtime.gateway';
import { RealtimeGateway } from './realtime.gateway';
import { SignalRRealtimeGateway } from './signalr-realtime.gateway';

export function provideRealtime(): Provider {
  return provideGateway(RealtimeGateway, 'realtime', {
    http: SignalRRealtimeGateway,
    pending: PendingRealtimeGateway,
  });
}

import { RealtimeEvent } from '../../../app/core/realtime/realtime.gateway';

export function sampleRealtimeEvents(now: Date): readonly RealtimeEvent[] {
  const occurredAt = now.toISOString();
  return [
    {
      type: 'serviceOrderStatusChanged',
      serviceOrderId: '00000000-0000-4000-9000-000000000001',
      number: 1042,
      status: 'InProgress',
      occurredAt,
    },
    {
      type: 'paymentConfirmed',
      serviceOrderId: '00000000-0000-4000-9000-000000000002',
      number: 1038,
      amount: 189_90,
      occurredAt,
    },
    {
      type: 'serviceOrderStatusChanged',
      serviceOrderId: '00000000-0000-4000-9000-000000000003',
      number: 1045,
      status: 'WaitingApproval',
      occurredAt,
    },
    { type: 'exportReady', exportId: '00000000-0000-4000-9000-000000000004', occurredAt },
  ];
}

import { StatusTone } from '../../../shared/ui/status-pill/status-pill';
import {
  CountStatus,
  MovementType,
  PurchaseOrderStatus,
  StockSituation,
} from '../data/inventory.models';

export const SITUATION_TONES: Readonly<Record<StockSituation, StatusTone>> = {
  Ok: 'ok',
  Reorder: 'warn',
  OutOfStock: 'bad',
  Idle: 'neutral',
};

export const COUNT_TONES: Readonly<Record<CountStatus, StatusTone>> = {
  Open: 'info',
  PendingApproval: 'warn',
  Approved: 'ok',
  Rejected: 'bad',
};

export const ORDER_TONES: Readonly<Record<PurchaseOrderStatus, StatusTone>> = {
  Draft: 'neutral',
  Queued: 'info',
  Sent: 'ok',
};

export const MOVEMENT_TONES: Readonly<Record<MovementType, StatusTone>> = {
  In: 'ok',
  Out: 'info',
  Adjustment: 'violet',
};

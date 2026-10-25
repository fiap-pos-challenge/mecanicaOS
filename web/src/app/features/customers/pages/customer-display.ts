import { StatusTone } from '../../../shared/ui/status-pill/status-pill';
import { CustomerSituation } from '../data/insights.contracts';

export const SITUATION_TONES: Readonly<Record<CustomerSituation, StatusTone>> = {
  Active: 'ok',
  OpenServiceOrder: 'warn',
  NoReturn: 'info',
};

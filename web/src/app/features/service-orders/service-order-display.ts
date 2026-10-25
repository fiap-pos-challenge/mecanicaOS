import { messages } from '../../core/i18n/messages.pt-BR';
import { StatusTone } from '../../shared/ui/status-pill/status-pill';
import { formatBrl } from '../../shared/util/money';
import { ServiceOrderCard, ServiceOrderStatus, ServiceOrderTag } from './data/service-order.models';
import { ChangedField } from './data/service-order.rules';

const text = messages.serviceOrders;

export const STATUS_TONES: Readonly<Record<ServiceOrderStatus, StatusTone>> = {
  Received: 'neutral',
  InDiagnosis: 'violet',
  WaitingApproval: 'warn',
  InProgress: 'info',
  Completed: 'ok',
  Delivered: 'neutral',
};

export const TAG_TONES: Readonly<Record<ServiceOrderTag, StatusTone>> = {
  Late: 'bad',
  MissingPart: 'warn',
  ReminderSent: 'info',
};

const dateFormatter = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit',
  month: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
});

const dayFormatter = new Intl.DateTimeFormat('pt-BR', {
  weekday: 'long',
  day: '2-digit',
  month: '2-digit',
});

export function formatWhen(iso: string | null): string {
  return iso === null ? '—' : dateFormatter.format(new Date(iso));
}

export function formatDay(key: string): string {
  const [year = 0, month = 1, day = 1] = key.split('-').map(Number);
  return dayFormatter.format(new Date(year, month - 1, day));
}

export function fieldValue(card: ServiceOrderCard, field: ChangedField): string {
  switch (field) {
    case 'status':
      return text.statuses[card.status];
    case 'mechanicName':
      return card.mechanicName ?? text.unassigned;
    case 'promisedAt':
      return formatWhen(card.promisedAt);
    case 'total':
      return formatBrl(card.total);
  }
}

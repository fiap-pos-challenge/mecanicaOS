import { messages } from '../../core/i18n/messages.pt-BR';
import { StatusTone } from '../../shared/ui/status-pill/status-pill';
import { formatBrl } from '../../shared/util/money';
import {
  DeliveryStatus,
  DiscountValue,
  QuoteDelivery,
  QuoteOption,
  QuoteStatus,
} from './data/quote.models';

const text = messages.quotes;

export type QuoteStage = QuoteStatus | 'Draft';

export type DeliveryStage = DeliveryStatus | 'Expired';

const quantityFormatter = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 3 });
const percentFormatter = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 });
const dateFormatter = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});

export const STAGE_TONES: Readonly<Record<QuoteStage, StatusTone>> = {
  Draft: 'neutral',
  Pending: 'info',
  Approved: 'ok',
  Rejected: 'bad',
};

export const DELIVERY_TONES: Readonly<Record<DeliveryStage, StatusTone>> = {
  Queued: 'neutral',
  Sent: 'info',
  Opened: 'ok',
  Failed: 'bad',
  Expired: 'warn',
};

export function quoteStage(status: QuoteStatus, delivery: DeliveryStatus | null): QuoteStage {
  return status === 'Pending' && delivery === null ? 'Draft' : status;
}

export function deliveryStage(delivery: QuoteDelivery, now = Date.now()): DeliveryStage {
  return delivery.status !== 'Failed' &&
    delivery.status !== 'Opened' &&
    Date.parse(delivery.expiresAt) <= now
    ? 'Expired'
    : delivery.status;
}

export function formatQuantity(value: number): string {
  return quantityFormatter.format(value);
}

export function formatDateTime(iso: string): string {
  return dateFormatter.format(new Date(iso));
}

export function formatDiscount(value: DiscountValue): string {
  return value.kind === 'Percent'
    ? `${percentFormatter.format(value.basisPoints / 100)}%`
    : formatBrl(value.amount);
}

export function formatBasisPoints(basisPoints: number): string {
  return `${percentFormatter.format(basisPoints / 100)}%`;
}

export function parseDecimal(value: string): number | null {
  const normalized = value.trim().replace(/\./g, '').replace(',', '.');
  if (!/^\d+(\.\d+)?$/.test(normalized)) {
    return null;
  }

  return Number(normalized);
}

export function optionDetail(option: QuoteOption): string | null {
  if (option.detail === null) {
    return null;
  }

  switch (option.kind) {
    case 'Package':
      return text.builder.packageItems(option.detail);
    case 'Service':
      return text.builder.minutes(option.detail);
    case 'Part':
      return option.detail;
  }
}

export function stockLabel(option: QuoteOption): string | null {
  const stock = option.stock;
  if (!stock) {
    return null;
  }

  return stock.available > 0
    ? text.builder.inStock(formatQuantity(stock.available))
    : text.builder.outOfStock(stock.leadTimeDays);
}

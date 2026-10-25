import { required } from '../../../core/data/contract-error';

export type NotificationType =
  | 'ServiceOrderChanged'
  | 'QuoteAnswered'
  | 'PaymentConfirmed'
  | 'StockLow'
  | 'ExportReady'
  | 'AutomationFailed'
  | 'DiscountApproval';

export const NOTIFICATION_TYPES: readonly NotificationType[] = [
  'ServiceOrderChanged',
  'QuoteAnswered',
  'PaymentConfirmed',
  'StockLow',
  'ExportReady',
  'AutomationFailed',
  'DiscountApproval',
];

export type NotificationChannel = 'inApp' | 'email' | 'whatsApp';

export const NOTIFICATION_CHANNELS: readonly NotificationChannel[] = ['inApp', 'email', 'whatsApp'];

export interface AppNotification {
  readonly id: string;
  readonly type: NotificationType;
  readonly title: string;
  readonly body: string;
  readonly createdAt: string;
  readonly read: boolean;
  readonly link: string | null;
  readonly aiSuggested: boolean;
}

export interface NotificationPreference {
  readonly type: NotificationType;
  readonly inApp: boolean;
  readonly email: boolean;
  readonly whatsApp: boolean;
}

export interface NotificationDto {
  readonly id?: string;
  readonly type?: NotificationType;
  readonly title?: string;
  readonly body?: string;
  readonly createdAt?: string;
  readonly read?: boolean;
  readonly link?: string | null;
  readonly aiSuggested?: boolean;
}

export interface NotificationsData {
  readonly url:
    | '/api/admin/notifications'
    | '/api/admin/notifications/read-all'
    | '/api/admin/notifications/{id}/read';
  readonly path?: { readonly id: string };
}

export interface NotificationPreferencesData {
  readonly url: '/api/admin/notifications/preferences';
  readonly body?: { readonly items: readonly NotificationPreference[] };
}

export function safeLink(link: string | null | undefined): string | null {
  return link && link.startsWith('/') && !link.startsWith('//') && !link.includes('\\')
    ? link
    : null;
}

export function toNotification(dto: NotificationDto): AppNotification {
  return {
    id: required(dto.id, 'id'),
    type: required(dto.type, 'type'),
    title: required(dto.title, 'title'),
    body: dto.body ?? '',
    createdAt: required(dto.createdAt, 'createdAt'),
    read: dto.read ?? false,
    link: safeLink(dto.link),
    aiSuggested: dto.aiSuggested ?? false,
  };
}

export function normalizePreferences(
  items: readonly Partial<NotificationPreference>[],
): readonly NotificationPreference[] {
  return NOTIFICATION_TYPES.map((type) => {
    const found = items.find((item) => item.type === type);
    return {
      type,
      inApp: found?.inApp ?? true,
      email: found?.email ?? false,
      whatsApp: found?.whatsApp ?? false,
    };
  });
}

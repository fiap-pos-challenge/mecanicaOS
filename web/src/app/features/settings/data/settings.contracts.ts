import { BackendRole } from '../../../core/auth/permissions';
import { DiscountRuleDto, PaymentSettingsDto } from '../../quotes/data/quote.contracts';
import {
  AuditArea,
  BusinessHours,
  FiscalIntegration,
  MessageTemplate,
  SecuritySettings,
  SendWindow,
  WorkshopAddress,
  WorkshopPlan,
} from './settings.models';

export const REAUTH_HEADER = 'X-Reauthentication';

export interface WorkshopDto {
  readonly name?: string;
  readonly document?: string;
  readonly address?: Partial<WorkshopAddress>;
  readonly logoUrl?: string | null;
  readonly hours?: readonly Partial<BusinessHours>[];
  readonly plan?: Partial<WorkshopPlan>;
}

export interface DiscountSettingsDto {
  readonly rules?: readonly DiscountRuleDto[];
  readonly limits?: readonly { readonly role?: BackendRole; readonly percent?: number }[];
}

export interface MessageSettingsDto {
  readonly templates?: readonly Partial<MessageTemplate>[];
  readonly window?: Partial<SendWindow>;
}

export interface SessionDto {
  readonly id?: string;
  readonly userName?: string;
  readonly device?: string;
  readonly lastSeenAt?: string;
  readonly current?: boolean;
}

export interface AuditEntryDto {
  readonly id?: string;
  readonly occurredAt?: string;
  readonly actorName?: string;
  readonly action?: string;
  readonly target?: string;
  readonly detail?: string;
}

export interface AuditPageDto {
  readonly items?: readonly AuditEntryDto[];
  readonly page?: number;
  readonly pageSize?: number;
  readonly totalItems?: number;
}

export interface ReauthenticationDto {
  readonly reauthToken?: string;
}

export interface SettingsData {
  readonly url:
    | '/api/admin/settings/workshop'
    | '/api/admin/settings/discounts'
    | '/api/admin/settings/payments'
    | '/api/admin/settings/messages'
    | '/api/admin/settings/security'
    | '/api/admin/settings/sessions'
    | '/api/admin/settings/fiscal';
  readonly body?: unknown;
}

export interface LogoData {
  readonly url: '/api/admin/settings/workshop/logo';
  readonly body: {
    readonly fileName: string;
    readonly contentType: string;
    readonly dataUrl: string;
  };
}

export interface SessionData {
  readonly url: '/api/admin/settings/sessions/{id}';
  readonly path: { readonly id: string };
}

export interface AuditData {
  readonly url: '/api/admin/settings/audit/search';
  readonly body: {
    readonly area: AuditArea;
    readonly from: string | null;
    readonly to: string | null;
    readonly actor: string;
    readonly page: number;
    readonly pageSize: number;
  };
}

export interface ReauthenticationData {
  readonly url: '/api/auth/reauthenticate';
  readonly body: { readonly password: string } | { readonly code: string };
}

export type SecuritySettingsDto = Partial<SecuritySettings>;

export type PaymentConfigurationDto = PaymentSettingsDto;

export interface FiscalDto {
  readonly integrations?: readonly Partial<FiscalIntegration>[];
}

import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { BACKEND_ROLES } from '../../../core/auth/permissions';
import { provideGateway } from '../../../core/config/provide-gateway';
import { required } from '../../../core/data/contract-error';
import { Page } from '../../../core/data/page';
import { toBasisPoints } from '../../quotes/data/quote.math';
import { toDiscountDto, toDiscountRule, toPaymentSettings } from '../../quotes/data/quote.mappers';
import {
  AuditData,
  AuditEntryDto,
  AuditPageDto,
  DiscountSettingsDto,
  FiscalDto,
  LogoData,
  MessageSettingsDto,
  PaymentConfigurationDto,
  REAUTH_HEADER,
  ReauthenticationData,
  ReauthenticationDto,
  SecuritySettingsDto,
  SessionData,
  SessionDto,
  SettingsData,
  WorkshopDto,
} from './settings.contracts';
import {
  ActiveSession,
  AuditEntry,
  AuditFilter,
  DiscountSettings,
  FiscalIntegration,
  MessageSettings,
  PaymentConfiguration,
  SecuritySettings,
  WEEKDAYS,
  Workshop,
} from './settings.models';

export const AUDIT_PAGE_SIZE = 20;

export type Reauthentication = { readonly password: string } | { readonly code: string };

export interface LogoUpload {
  readonly fileName: string;
  readonly contentType: string;
  readonly dataUrl: string;
}

export function toWorkshop(dto: WorkshopDto): Workshop {
  const address = dto.address ?? {};
  const plan = dto.plan ?? {};
  return {
    name: required(dto.name, 'name'),
    document: required(dto.document, 'document'),
    address: {
      zipCode: address.zipCode ?? '',
      street: address.street ?? '',
      number: address.number ?? '',
      district: address.district ?? '',
      city: address.city ?? '',
      state: address.state ?? '',
    },
    logoUrl: dto.logoUrl ?? null,
    hours: WEEKDAYS.map((weekday) => {
      const day = (dto.hours ?? []).find((item) => item.weekday === weekday);
      return {
        weekday,
        closed: day?.closed ?? true,
        opens: day?.opens ?? '08:00',
        closes: day?.closes ?? '18:00',
      };
    }),
    plan: {
      name: plan.name ?? '',
      renewsAt: plan.renewsAt ?? '',
      users: plan.users ?? 0,
      maxUsers: plan.maxUsers ?? 0,
    },
  };
}

export function toDiscountSettings(dto: DiscountSettingsDto): DiscountSettings {
  const limits = dto.limits ?? [];
  return {
    rules: (dto.rules ?? []).map(toDiscountRule),
    limits: Object.fromEntries(
      BACKEND_ROLES.map((role) => [role, limits.find((item) => item.role === role)?.percent ?? 0]),
    ) as DiscountSettings['limits'],
  };
}

export function fromDiscountSettings(settings: DiscountSettings): DiscountSettingsDto {
  return {
    rules: settings.rules.map((rule) => ({
      id: rule.id,
      name: rule.name.trim(),
      condition: rule.condition,
      isActive: rule.isActive,
      ...toDiscountDto(rule.value),
    })),
    limits: BACKEND_ROLES.map((role) => ({ role, percent: settings.limits[role] })),
  };
}

export function toPaymentConfiguration(dto: PaymentConfigurationDto): PaymentConfiguration {
  return {
    ...toPaymentSettings(dto),
    fees: (dto.fees ?? []).map((fee) => ({
      method: required(fee.method, 'fees.method'),
      feeBasisPoints: toBasisPoints(fee.feePercent ?? 0),
      settlementDays: fee.settlementDays ?? 0,
    })),
  };
}

export function fromPaymentConfiguration(settings: PaymentConfiguration): PaymentConfigurationDto {
  return {
    methods: settings.methods,
    maxInstallments: settings.credit.maxInstallments,
    interestFreeInstallments: settings.credit.interestFreeInstallments,
    monthlyInterestPercent: settings.credit.monthlyInterestBasisPoints / 100,
    fees: settings.fees.map((fee) => ({
      method: fee.method,
      feePercent: fee.feeBasisPoints / 100,
      settlementDays: fee.settlementDays,
    })),
  };
}

export function toMessageSettings(dto: MessageSettingsDto): MessageSettings {
  const window = dto.window ?? {};
  return {
    templates: (dto.templates ?? []).map((template) => ({
      kind: required(template.kind, 'templates.kind'),
      channel: required(template.channel, 'templates.channel'),
      body: template.body ?? '',
    })),
    window: {
      from: window.from ?? '08:00',
      to: window.to ?? '18:00',
      weekdays: (window.weekdays ?? []).filter((day) => WEEKDAYS.includes(day)),
    },
  };
}

export function toSecurity(dto: SecuritySettingsDto): SecuritySettings {
  return {
    requireTwoFactorForAll: dto.requireTwoFactorForAll ?? false,
    idleMinutes: dto.idleMinutes ?? 30,
    lockoutAttempts: dto.lockoutAttempts ?? 5,
    lockoutMinutes: dto.lockoutMinutes ?? 15,
  };
}

function toSession(dto: SessionDto): ActiveSession {
  return {
    id: required(dto.id, 'sessions.id'),
    userName: required(dto.userName, 'sessions.userName'),
    device: dto.device ?? '',
    lastSeenAt: required(dto.lastSeenAt, 'sessions.lastSeenAt'),
    current: dto.current ?? false,
  };
}

function toAuditEntry(dto: AuditEntryDto): AuditEntry {
  return {
    id: required(dto.id, 'audit.id'),
    occurredAt: required(dto.occurredAt, 'audit.occurredAt'),
    actorName: dto.actorName ?? '',
    action: required(dto.action, 'audit.action'),
    target: dto.target ?? '',
    detail: dto.detail ?? '',
  };
}

export abstract class SettingsGateway {
  abstract workshop(): Observable<Workshop>;
  abstract saveWorkshop(workshop: Workshop, reauth: string): Observable<Workshop>;
  abstract uploadLogo(file: LogoUpload, reauth: string): Observable<string>;
  abstract discounts(): Observable<DiscountSettings>;
  abstract saveDiscounts(settings: DiscountSettings, reauth: string): Observable<DiscountSettings>;
  abstract payments(): Observable<PaymentConfiguration>;
  abstract savePayments(
    settings: PaymentConfiguration,
    reauth: string,
  ): Observable<PaymentConfiguration>;
  abstract messages(): Observable<MessageSettings>;
  abstract saveMessages(settings: MessageSettings, reauth: string): Observable<MessageSettings>;
  abstract security(): Observable<SecuritySettings>;
  abstract saveSecurity(settings: SecuritySettings, reauth: string): Observable<SecuritySettings>;
  abstract sessions(): Observable<readonly ActiveSession[]>;
  abstract endSession(id: string, reauth: string): Observable<void>;
  abstract audit(filter: AuditFilter): Observable<Page<AuditEntry>>;
  abstract fiscal(): Observable<readonly FiscalIntegration[]>;
  abstract reauthenticate(proof: Reauthentication): Observable<string>;
}

@Injectable({ providedIn: 'root' })
export class HttpSettingsGateway extends SettingsGateway {
  private readonly api = inject(ApiClient);

  workshop(): Observable<Workshop> {
    return this.get<WorkshopDto>('/api/admin/settings/workshop').pipe(map(toWorkshop));
  }

  saveWorkshop(workshop: Workshop, reauth: string): Observable<Workshop> {
    const body = {
      name: workshop.name.trim(),
      document: workshop.document,
      address: workshop.address,
      hours: workshop.hours,
    };
    return this.put<WorkshopDto>('/api/admin/settings/workshop', body, reauth).pipe(
      map(toWorkshop),
    );
  }

  uploadLogo(file: LogoUpload, reauth: string): Observable<string> {
    return this.api
      .send<LogoData, { logoUrl?: string }>('POST', '/api/admin/settings/workshop/logo', {
        body: file,
        headers: { [REAUTH_HEADER]: reauth },
      })
      .pipe(map((dto) => required(dto.logoUrl, 'logoUrl')));
  }

  discounts(): Observable<DiscountSettings> {
    return this.get<DiscountSettingsDto>('/api/admin/settings/discounts').pipe(
      map(toDiscountSettings),
    );
  }

  saveDiscounts(settings: DiscountSettings, reauth: string): Observable<DiscountSettings> {
    return this.put<DiscountSettingsDto>(
      '/api/admin/settings/discounts',
      fromDiscountSettings(settings),
      reauth,
    ).pipe(map(toDiscountSettings));
  }

  payments(): Observable<PaymentConfiguration> {
    return this.get<PaymentConfigurationDto>('/api/admin/settings/payments').pipe(
      map(toPaymentConfiguration),
    );
  }

  savePayments(settings: PaymentConfiguration, reauth: string): Observable<PaymentConfiguration> {
    return this.put<PaymentConfigurationDto>(
      '/api/admin/settings/payments',
      fromPaymentConfiguration(settings),
      reauth,
    ).pipe(map(toPaymentConfiguration));
  }

  messages(): Observable<MessageSettings> {
    return this.get<MessageSettingsDto>('/api/admin/settings/messages').pipe(
      map(toMessageSettings),
    );
  }

  saveMessages(settings: MessageSettings, reauth: string): Observable<MessageSettings> {
    return this.put<MessageSettingsDto>('/api/admin/settings/messages', settings, reauth).pipe(
      map(toMessageSettings),
    );
  }

  security(): Observable<SecuritySettings> {
    return this.get<SecuritySettingsDto>('/api/admin/settings/security').pipe(map(toSecurity));
  }

  saveSecurity(settings: SecuritySettings, reauth: string): Observable<SecuritySettings> {
    return this.put<SecuritySettingsDto>('/api/admin/settings/security', settings, reauth).pipe(
      map(toSecurity),
    );
  }

  sessions(): Observable<readonly ActiveSession[]> {
    return this.get<{ items?: readonly SessionDto[] }>('/api/admin/settings/sessions').pipe(
      map((dto) => (dto.items ?? []).map(toSession)),
    );
  }

  endSession(id: string, reauth: string): Observable<void> {
    return this.api
      .send<SessionData, unknown>('DELETE', '/api/admin/settings/sessions/{id}', {
        path: { id },
        headers: { [REAUTH_HEADER]: reauth },
      })
      .pipe(map(() => undefined));
  }

  audit(filter: AuditFilter): Observable<Page<AuditEntry>> {
    return this.api
      .send<AuditData, AuditPageDto>('POST', '/api/admin/settings/audit/search', {
        body: {
          area: filter.area,
          from: filter.from,
          to: filter.to,
          actor: filter.actor.trim(),
          page: filter.page,
          pageSize: AUDIT_PAGE_SIZE,
        },
      })
      .pipe(
        map((dto) => ({
          items: (dto.items ?? []).map(toAuditEntry),
          page: dto.page ?? filter.page,
          pageSize: dto.pageSize ?? AUDIT_PAGE_SIZE,
          totalItems: dto.totalItems ?? 0,
        })),
      );
  }

  fiscal(): Observable<readonly FiscalIntegration[]> {
    return this.get<FiscalDto>('/api/admin/settings/fiscal').pipe(
      map((dto) =>
        (dto.integrations ?? []).map((item) => ({
          kind: required(item.kind, 'integrations.kind'),
          status: item.status ?? 'NotConfigured',
          provider: item.provider ?? null,
        })),
      ),
    );
  }

  reauthenticate(proof: Reauthentication): Observable<string> {
    return this.api
      .send<ReauthenticationData, ReauthenticationDto>('POST', '/api/auth/reauthenticate', {
        body: proof,
      })
      .pipe(map((dto) => required(dto.reauthToken, 'reauthToken')));
  }

  private get<T>(url: SettingsData['url']): Observable<T> {
    return this.api.send<SettingsData, T>('GET', url);
  }

  private put<T>(url: SettingsData['url'], body: unknown, reauth: string): Observable<T> {
    return this.api.send<SettingsData, T>('PUT', url, {
      body,
      headers: { [REAUTH_HEADER]: reauth },
    });
  }
}

export function provideSettingsData(): Provider {
  return provideGateway(SettingsGateway, 'settings', { http: HttpSettingsGateway });
}

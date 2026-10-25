import { BackendRole } from '../../../core/auth/permissions';
import { isValidCnpj } from '../../../shared/util/document';
import { DiscountRule, PaymentMethod, PaymentSettings } from '../../quotes/data/quote.models';

export type Weekday =
  'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday' | 'Sunday';

export const WEEKDAYS: readonly Weekday[] = [
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
  'Sunday',
];

export interface BusinessHours {
  readonly weekday: Weekday;
  readonly closed: boolean;
  readonly opens: string;
  readonly closes: string;
}

export interface WorkshopAddress {
  readonly zipCode: string;
  readonly street: string;
  readonly number: string;
  readonly district: string;
  readonly city: string;
  readonly state: string;
}

export interface WorkshopPlan {
  readonly name: string;
  readonly renewsAt: string;
  readonly users: number;
  readonly maxUsers: number;
}

export interface Workshop {
  readonly name: string;
  readonly document: string;
  readonly address: WorkshopAddress;
  readonly logoUrl: string | null;
  readonly hours: readonly BusinessHours[];
  readonly plan: WorkshopPlan;
}

export type WorkshopProblem = 'name' | 'document' | 'address' | 'hours';

export type DiscountLimits = Readonly<Record<BackendRole, number>>;

export interface DiscountSettings {
  readonly rules: readonly DiscountRule[];
  readonly limits: DiscountLimits;
}

export type DiscountProblem = 'ruleName' | 'ruleValue' | 'limit';

export interface PaymentFee {
  readonly method: PaymentMethod;
  readonly feeBasisPoints: number;
  readonly settlementDays: number;
}

export interface PaymentConfiguration extends PaymentSettings {
  readonly fees: readonly PaymentFee[];
}

export type PaymentProblem = 'methods' | 'installments' | 'interest' | 'fees';

export type MessageKind = 'QuoteSent' | 'StatusChanged' | 'QuoteReminder' | 'Receipt';

export const MESSAGE_KINDS: readonly MessageKind[] = [
  'QuoteSent',
  'StatusChanged',
  'QuoteReminder',
  'Receipt',
];

export type MessageChannel = 'WhatsApp' | 'Email';

export interface MessageTemplate {
  readonly kind: MessageKind;
  readonly channel: MessageChannel;
  readonly body: string;
}

export interface SendWindow {
  readonly from: string;
  readonly to: string;
  readonly weekdays: readonly Weekday[];
}

export interface MessageSettings {
  readonly templates: readonly MessageTemplate[];
  readonly window: SendWindow;
}

export const MESSAGE_VARIABLES = ['cliente', 'veiculo', 'placa', 'total', 'link', 'prazo'] as const;

export type MessageVariable = (typeof MESSAGE_VARIABLES)[number];

export const MAX_TEMPLATE = 1000;

export type MessageProblem = 'empty' | 'tooLong' | 'unknownVariable' | 'window';

export interface SecuritySettings {
  readonly requireTwoFactorForAll: boolean;
  readonly idleMinutes: number;
  readonly lockoutAttempts: number;
  readonly lockoutMinutes: number;
}

export type SecurityProblem = 'idle' | 'attempts' | 'lockout';

export interface ActiveSession {
  readonly id: string;
  readonly userName: string;
  readonly device: string;
  readonly lastSeenAt: string;
  readonly current: boolean;
}

export interface AuditEntry {
  readonly id: string;
  readonly occurredAt: string;
  readonly actorName: string;
  readonly action: string;
  readonly target: string;
  readonly detail: string;
}

export type AuditArea = 'all' | 'discounts' | 'payments' | 'security' | 'customers' | 'stock';

export const AUDIT_AREAS: readonly AuditArea[] = [
  'all',
  'discounts',
  'payments',
  'security',
  'customers',
  'stock',
];

export interface AuditFilter {
  readonly area: AuditArea;
  readonly from: string | null;
  readonly to: string | null;
  readonly actor: string;
  readonly page: number;
}

export type FiscalKind = 'NFSe' | 'NFe' | 'NFCe';

export type FiscalStatus = 'NotConfigured' | 'Planned' | 'Active';

export interface FiscalIntegration {
  readonly kind: FiscalKind;
  readonly status: FiscalStatus;
  readonly provider: string | null;
}

const TIME = /^([01]\d|2[0-3]):[0-5]\d$/;

export function isTime(value: string): boolean {
  return TIME.test(value);
}

export function validateWorkshop(workshop: Workshop): readonly WorkshopProblem[] {
  const problems: WorkshopProblem[] = [];
  const name = workshop.name.trim();
  if (name.length < 3 || name.length > 120) {
    problems.push('name');
  }

  if (!isValidCnpj(workshop.document)) {
    problems.push('document');
  }

  const address = workshop.address;
  if (
    !/^\d{8}$/.test(address.zipCode.replace(/\D/g, '')) ||
    [address.street, address.number, address.city].some((value) => value.trim() === '') ||
    !/^[A-Z]{2}$/.test(address.state)
  ) {
    problems.push('address');
  }

  if (
    workshop.hours.some(
      (day) =>
        !day.closed && (!isTime(day.opens) || !isTime(day.closes) || day.opens >= day.closes),
    )
  ) {
    problems.push('hours');
  }

  return problems;
}

export function validateDiscounts(settings: DiscountSettings): readonly DiscountProblem[] {
  const problems = new Set<DiscountProblem>();
  for (const rule of settings.rules) {
    const name = rule.name.trim();
    if (name.length < 3 || name.length > 60) {
      problems.add('ruleName');
    }

    const value = rule.value;
    if (
      (value.kind === 'Percent' && (value.basisPoints <= 0 || value.basisPoints > 10_000)) ||
      (value.kind === 'Amount' && value.amount <= 0)
    ) {
      problems.add('ruleValue');
    }
  }

  if (Object.values(settings.limits).some((limit) => limit < 0 || limit > 100)) {
    problems.add('limit');
  }

  return [...problems];
}

export function validatePayments(settings: PaymentConfiguration): readonly PaymentProblem[] {
  const problems: PaymentProblem[] = [];
  const credit = settings.credit;
  if (settings.methods.length === 0) {
    problems.push('methods');
  }

  if (
    credit.maxInstallments < 1 ||
    credit.maxInstallments > 24 ||
    credit.interestFreeInstallments < 1 ||
    credit.interestFreeInstallments > credit.maxInstallments
  ) {
    problems.push('installments');
  }

  if (credit.monthlyInterestBasisPoints < 0 || credit.monthlyInterestBasisPoints > 1_000) {
    problems.push('interest');
  }

  if (
    settings.fees.some(
      (fee) =>
        fee.feeBasisPoints < 0 ||
        fee.feeBasisPoints > 2_000 ||
        !Number.isInteger(fee.settlementDays) ||
        fee.settlementDays < 0 ||
        fee.settlementDays > 60,
    )
  ) {
    problems.push('fees');
  }

  return problems;
}

export function templateVariables(body: string): readonly string[] {
  return [...body.matchAll(/\{([^}]*)\}/g)].map((match) => match[1] ?? '');
}

export function unknownVariables(body: string): readonly string[] {
  return templateVariables(body).filter(
    (name) => !MESSAGE_VARIABLES.some((variable) => variable === name),
  );
}

export function validateMessages(settings: MessageSettings): readonly MessageProblem[] {
  const problems = new Set<MessageProblem>();
  for (const template of settings.templates) {
    const body = template.body.trim();
    if (body.length === 0) {
      problems.add('empty');
    } else if (body.length > MAX_TEMPLATE) {
      problems.add('tooLong');
    } else if (unknownVariables(body).length > 0) {
      problems.add('unknownVariable');
    }
  }

  const window = settings.window;
  if (
    !isTime(window.from) ||
    !isTime(window.to) ||
    window.from >= window.to ||
    window.weekdays.length === 0
  ) {
    problems.add('window');
  }

  return [...problems];
}

export function renderTemplate(
  body: string,
  values: Readonly<Record<MessageVariable, string>>,
): string {
  return body.replace(/\{([^}]*)\}/g, (match, name: string) =>
    MESSAGE_VARIABLES.some((variable) => variable === name)
      ? values[name as MessageVariable]
      : match,
  );
}

export function validateSecurity(settings: SecuritySettings): readonly SecurityProblem[] {
  const problems: SecurityProblem[] = [];
  if (
    !Number.isInteger(settings.idleMinutes) ||
    settings.idleMinutes < 5 ||
    settings.idleMinutes > 60
  ) {
    problems.push('idle');
  }

  if (
    !Number.isInteger(settings.lockoutAttempts) ||
    settings.lockoutAttempts < 3 ||
    settings.lockoutAttempts > 10
  ) {
    problems.push('attempts');
  }

  if (
    !Number.isInteger(settings.lockoutMinutes) ||
    settings.lockoutMinutes < 5 ||
    settings.lockoutMinutes > 120
  ) {
    problems.push('lockout');
  }

  return problems;
}

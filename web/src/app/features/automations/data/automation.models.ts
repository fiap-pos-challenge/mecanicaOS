import { messages } from '../../../core/i18n/messages.pt-BR';
import { SERVICE_ORDER_STATUSES } from '../../service-orders/data/service-order.models';

export type RuleTrigger =
  | 'QuoteSent'
  | 'ServiceOrderStatusChanged'
  | 'StockBelowMinimum'
  | 'ServiceOrderDelivered'
  | 'VehicleMaintenanceDue'
  | 'PaymentOverdue'
  | 'PaymentReceived';

export const RULE_TRIGGERS: readonly RuleTrigger[] = [
  'QuoteSent',
  'ServiceOrderStatusChanged',
  'StockBelowMinimum',
  'ServiceOrderDelivered',
  'VehicleMaintenanceDue',
  'PaymentOverdue',
  'PaymentReceived',
];

export type ConditionField =
  'orderStatus' | 'quoteStatus' | 'amount' | 'daysOverdue' | 'customerConsent' | 'partStock';

export type ConditionOperator = 'equals' | 'notEquals' | 'greaterThan' | 'lessThan';

export type ActionKind =
  'SendWhatsApp' | 'SendEmail' | 'NotifyTeam' | 'CreatePurchaseOrder' | 'RegisterPayment';

export const ACTION_KINDS: readonly ActionKind[] = [
  'SendWhatsApp',
  'SendEmail',
  'NotifyTeam',
  'CreatePurchaseOrder',
  'RegisterPayment',
];

export type WaitUnit = 'minutes' | 'hours' | 'days';

export const WAIT_UNITS: readonly WaitUnit[] = ['minutes', 'hours', 'days'];

export const QUOTE_STATUSES = ['Pending', 'Approved', 'Rejected'] as const;

export interface RuleCondition {
  readonly field: ConditionField;
  readonly operator: ConditionOperator;
  readonly value: string;
}

export interface RuleAction {
  readonly kind: ActionKind;
  readonly message: string | null;
}

export interface RuleWait {
  readonly amount: number;
  readonly unit: WaitUnit;
}

export interface RuleDefinition {
  readonly name: string;
  readonly trigger: RuleTrigger;
  readonly conditions: readonly RuleCondition[];
  readonly wait: RuleWait | null;
  readonly actions: readonly RuleAction[];
}

export interface AutomationRule {
  readonly id: string;
  readonly enabled: boolean;
  readonly definition: RuleDefinition;
  readonly runs24h: number;
  readonly lastRunAt: string | null;
}

export type RuleProblem =
  | 'name'
  | 'noActions'
  | 'tooManyConditions'
  | 'tooManyActions'
  | 'conditionField'
  | 'conditionOperator'
  | 'conditionValue'
  | 'waitAmount'
  | 'messageMissing'
  | 'messageTooLong'
  | 'messagePlaceholder'
  | 'actionTrigger';

type FieldKind = 'enum' | 'number' | 'boolean';

const FIELD_KIND: Readonly<Record<ConditionField, FieldKind>> = {
  orderStatus: 'enum',
  quoteStatus: 'enum',
  amount: 'number',
  daysOverdue: 'number',
  customerConsent: 'boolean',
  partStock: 'number',
};

const OPERATORS: Readonly<Record<FieldKind, readonly ConditionOperator[]>> = {
  enum: ['equals', 'notEquals'],
  number: ['equals', 'greaterThan', 'lessThan'],
  boolean: ['equals'],
};

const TRIGGER_FIELDS: Readonly<Record<RuleTrigger, readonly ConditionField[]>> = {
  QuoteSent: ['quoteStatus', 'amount', 'customerConsent'],
  ServiceOrderStatusChanged: ['orderStatus', 'customerConsent'],
  StockBelowMinimum: ['partStock'],
  ServiceOrderDelivered: ['amount', 'customerConsent'],
  VehicleMaintenanceDue: ['customerConsent'],
  PaymentOverdue: ['amount', 'daysOverdue', 'customerConsent'],
  PaymentReceived: ['amount'],
};

const ACTION_TRIGGERS: Readonly<Partial<Record<ActionKind, readonly RuleTrigger[]>>> = {
  CreatePurchaseOrder: ['StockBelowMinimum'],
  RegisterPayment: ['PaymentReceived'],
};

const CUSTOMER_MESSAGES: readonly ActionKind[] = ['SendWhatsApp', 'SendEmail'];

export const MESSAGE_ACTIONS: readonly ActionKind[] = [...CUSTOMER_MESSAGES, 'NotifyTeam'];

export const PLACEHOLDERS = ['cliente', 'os', 'veiculo', 'valor', 'link'];

export const MAX_ITEMS = 5;

export const MAX_MESSAGE = 500;

const WAIT_LIMIT: Readonly<Record<WaitUnit, number>> = {
  minutes: 60 * 24 * 60,
  hours: 24 * 60,
  days: 60,
};

export function fieldsFor(trigger: RuleTrigger): readonly ConditionField[] {
  return TRIGGER_FIELDS[trigger];
}

export function operatorsFor(field: ConditionField): readonly ConditionOperator[] {
  return OPERATORS[FIELD_KIND[field]];
}

export function actionsFor(trigger: RuleTrigger): readonly ActionKind[] {
  return ACTION_KINDS.filter((kind) => ACTION_TRIGGERS[kind]?.includes(trigger) ?? true);
}

export function valuesFor(field: ConditionField): readonly string[] | null {
  switch (field) {
    case 'orderStatus':
      return SERVICE_ORDER_STATUSES;
    case 'quoteStatus':
      return QUOTE_STATUSES;
    case 'customerConsent':
      return ['true', 'false'];
    default:
      return null;
  }
}

export function needsMessage(kind: ActionKind): boolean {
  return MESSAGE_ACTIONS.includes(kind);
}

function validValue(condition: RuleCondition): boolean {
  const values = valuesFor(condition.field);
  if (values) {
    return values.includes(condition.value);
  }

  return /^\d+([.,]\d{1,2})?$/.test(condition.value.trim());
}

export function validateRule(rule: RuleDefinition): readonly RuleProblem[] {
  const problems = new Set<RuleProblem>();
  const name = rule.name.trim();
  if (name.length < 3 || name.length > 80) {
    problems.add('name');
  }

  if (rule.actions.length === 0) {
    problems.add('noActions');
  }

  if (rule.conditions.length > MAX_ITEMS) {
    problems.add('tooManyConditions');
  }

  if (rule.actions.length > MAX_ITEMS) {
    problems.add('tooManyActions');
  }

  for (const condition of rule.conditions) {
    if (!TRIGGER_FIELDS[rule.trigger].includes(condition.field)) {
      problems.add('conditionField');
    } else if (!operatorsFor(condition.field).includes(condition.operator)) {
      problems.add('conditionOperator');
    } else if (!validValue(condition)) {
      problems.add('conditionValue');
    }
  }

  if (
    rule.wait &&
    (!Number.isInteger(rule.wait.amount) ||
      rule.wait.amount < 1 ||
      rule.wait.amount > WAIT_LIMIT[rule.wait.unit])
  ) {
    problems.add('waitAmount');
  }

  for (const action of rule.actions) {
    if (!actionsFor(rule.trigger).includes(action.kind)) {
      problems.add('actionTrigger');
    }

    if (needsMessage(action.kind)) {
      const message = action.message?.trim() ?? '';
      if (message.length === 0) {
        problems.add('messageMissing');
      } else if (message.length > MAX_MESSAGE) {
        problems.add('messageTooLong');
      } else if (
        [...message.matchAll(/\{([^}]*)\}/g)].some(
          (match) => !PLACEHOLDERS.includes(match[1] ?? ''),
        )
      ) {
        problems.add('messagePlaceholder');
      }
    }
  }

  return [...problems];
}

export type RuleTemplate =
  | 'quoteReminder'
  | 'statusNotice'
  | 'restock'
  | 'postSale'
  | 'preventive'
  | 'collection'
  | 'paymentWriteOff';

export const RULE_TEMPLATES: readonly RuleTemplate[] = [
  'quoteReminder',
  'statusNotice',
  'restock',
  'postSale',
  'preventive',
  'collection',
  'paymentWriteOff',
];

export function templateRule(template: RuleTemplate): RuleDefinition {
  const text = messages.automations.templates;
  const name = text.names[template];
  const team: RuleAction = { kind: 'NotifyTeam', message: text.messages.team };
  switch (template) {
    case 'quoteReminder':
      return {
        name,
        trigger: 'QuoteSent',
        conditions: [{ field: 'quoteStatus', operator: 'equals', value: 'Pending' }],
        wait: { amount: 2, unit: 'days' },
        actions: [{ kind: 'SendWhatsApp', message: text.messages.quoteReminder }],
      };
    case 'statusNotice':
      return {
        name,
        trigger: 'ServiceOrderStatusChanged',
        conditions: [{ field: 'customerConsent', operator: 'equals', value: 'true' }],
        wait: null,
        actions: [{ kind: 'SendWhatsApp', message: text.messages.statusNotice }],
      };
    case 'restock':
      return {
        name,
        trigger: 'StockBelowMinimum',
        conditions: [],
        wait: null,
        actions: [{ kind: 'CreatePurchaseOrder', message: null }, team],
      };
    case 'postSale':
      return {
        name,
        trigger: 'ServiceOrderDelivered',
        conditions: [],
        wait: { amount: 3, unit: 'days' },
        actions: [{ kind: 'SendWhatsApp', message: text.messages.postSale }],
      };
    case 'preventive':
      return {
        name,
        trigger: 'VehicleMaintenanceDue',
        conditions: [{ field: 'customerConsent', operator: 'equals', value: 'true' }],
        wait: null,
        actions: [{ kind: 'SendWhatsApp', message: text.messages.preventive }],
      };
    case 'collection':
      return {
        name,
        trigger: 'PaymentOverdue',
        conditions: [{ field: 'daysOverdue', operator: 'greaterThan', value: '3' }],
        wait: null,
        actions: [{ kind: 'SendEmail', message: text.messages.collection }],
      };
    case 'paymentWriteOff':
      return {
        name,
        trigger: 'PaymentReceived',
        conditions: [],
        wait: null,
        actions: [{ kind: 'RegisterPayment', message: null }, team],
      };
  }
}

export function blankRule(): RuleDefinition {
  return {
    name: '',
    trigger: 'ServiceOrderStatusChanged',
    conditions: [],
    wait: null,
    actions: [{ kind: 'NotifyTeam', message: '' }],
  };
}

export function readRule(value: unknown): RuleDefinition | null {
  if (typeof value !== 'object' || value === null) {
    return null;
  }

  const record = value as Readonly<Record<string, unknown>>;
  const trigger = RULE_TRIGGERS.find((item) => item === record['trigger']);
  const list = (key: string): readonly Readonly<Record<string, unknown>>[] =>
    Array.isArray(record[key]) ? (record[key] as Readonly<Record<string, unknown>>[]) : [];
  const conditions = list('conditions').map((item) => {
    const field = Object.keys(FIELD_KIND).find((key) => key === item['field']);
    const operator = (['equals', 'notEquals', 'greaterThan', 'lessThan'] as const).find(
      (key) => key === item['operator'],
    );
    return field && operator && typeof item['value'] === 'string'
      ? { field: field as ConditionField, operator, value: item['value'] }
      : null;
  });
  const actions = list('actions').map((item) => {
    const kind = ACTION_KINDS.find((key) => key === item['kind']);
    return kind
      ? { kind, message: typeof item['message'] === 'string' ? item['message'] : null }
      : null;
  });
  const wait = record['wait'] as Readonly<Record<string, unknown>> | null | undefined;
  const unit = WAIT_UNITS.find((key) => key === wait?.['unit']);
  if (
    !trigger ||
    typeof record['name'] !== 'string' ||
    conditions.includes(null) ||
    actions.includes(null)
  ) {
    return null;
  }

  return {
    name: record['name'],
    trigger,
    conditions: conditions.filter((item): item is RuleCondition => item !== null),
    actions: actions.filter((item): item is RuleAction => item !== null),
    wait: unit && typeof wait?.['amount'] === 'number' ? { amount: wait['amount'], unit } : null,
  };
}

export type QueueName =
  'notifications' | 'documents' | 'ai' | 'payments' | 'imports' | 'reports' | 'automations';

export interface QueueHealth {
  readonly name: QueueName;
  readonly processed: number;
  readonly failed: number;
  readonly pending: number;
}

export interface QueueFailure {
  readonly id: string;
  readonly queue: QueueName;
  readonly summary: string;
  readonly error: string;
  readonly attempts: number;
  readonly failedAt: string;
}

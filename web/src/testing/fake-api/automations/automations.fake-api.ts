import { messages } from '../../../app/core/i18n/messages.pt-BR';
import {
  QueueName,
  readRule,
  RuleDefinition,
  templateRule,
  validateRule,
} from '../../../app/features/automations/data/automation.models';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import {
  accepted,
  created,
  fieldError,
  FakeRouter,
  noContent,
  ok,
  problem,
  readBody,
  validation,
} from '../fake-api/fake-http';
import { fakeId } from '../fake-api/fake-ids';
import { authorize, FakeRole, isPrincipal } from '../fake-api/fake-session';
import { FakeRequest } from '../fake-request';

export const AUTOMATION_PATHS = {
  rules: '/api/admin/automations/rules',
  rule: '/api/admin/automations/rules/:id',
  status: '/api/admin/automations/rules/:id/status',
  queues: '/api/admin/automations/queues',
  failures: '/api/admin/automations/queues/failures',
  retry: '/api/admin/automations/queues/failures/:id/retry',
} as const;

export interface FakeRule {
  id: string;
  enabled: boolean;
  definition: RuleDefinition;
  runs24h: number;
  lastRunAt: string | null;
}

export interface FakeQueue {
  name: QueueName;
  processed: number;
  failed: number;
  pending: number;
}

export interface FakeFailure {
  id: string;
  queue: QueueName;
  summary: string;
  error: string;
  attempts: number;
  failedAt: string;
}

export interface FakeAutomations {
  rules: FakeRule[];
  queues: FakeQueue[];
  failures: FakeFailure[];
  retried: string[];
}

const viewers: readonly FakeRole[] = ['Admin', 'Manager'];
const admins: readonly FakeRole[] = ['Admin'];

export function createAutomationsSeed(): FakeAutomations {
  return {
    retried: [],
    rules: [
      {
        id: fakeId(),
        enabled: true,
        definition: templateRule('quoteReminder'),
        runs24h: 4,
        lastRunAt: '2026-10-15T12:00:00Z',
      },
      {
        id: fakeId(),
        enabled: true,
        definition: templateRule('restock'),
        runs24h: 1,
        lastRunAt: '2026-10-15T09:30:00Z',
      },
      {
        id: fakeId(),
        enabled: false,
        definition: templateRule('collection'),
        runs24h: 0,
        lastRunAt: null,
      },
    ],
    queues: [
      { name: 'notifications', processed: 128, failed: 1, pending: 2 },
      { name: 'documents', processed: 46, failed: 1, pending: 0 },
      { name: 'ai', processed: 19, failed: 0, pending: 0 },
      { name: 'payments', processed: 12, failed: 0, pending: 0 },
      { name: 'imports', processed: 0, failed: 0, pending: 0 },
      { name: 'reports', processed: 7, failed: 0, pending: 1 },
      { name: 'automations', processed: 33, failed: 0, pending: 0 },
    ],
    failures: [
      {
        id: 'failure-whatsapp',
        queue: 'notifications',
        summary: 'WhatsApp da OS 3318',
        error: 'O número não tem WhatsApp.',
        attempts: 5,
        failedAt: '2026-10-16T08:10:00Z',
      },
      {
        id: 'failure-pdf',
        queue: 'documents',
        summary: 'PDF do recibo REC1-3310',
        error: 'Tempo esgotado ao gerar o PDF.',
        attempts: 3,
        failedAt: '2026-10-16T07:45:00Z',
      },
    ],
  };
}

export function createAutomationsFakeApi(state: FakeBackendState): FakeFeatureApi {
  const db = (): FakeAutomations => state.database.automations;
  const guard = (request: FakeRequest, roles = viewers) =>
    authorize(request, state.database.users, roles);
  const notFound = () => problem(404, 'automations.notFound', messages.errors.notFound);
  const read = (request: FakeRequest) => {
    const definition = readRule(readBody(request)['definition']);
    if (!definition) {
      return validation([
        fieldError('definition', 'automations.definition', messages.fake.invalidRule),
      ]);
    }

    const problems = validateRule(definition);
    return problems.length > 0
      ? problem(
          422,
          'automations.invalid',
          messages.automations.problems[problems[0] ?? 'noActions'],
        )
      : definition;
  };
  const isDefinition = (
    value: RuleDefinition | ReturnType<typeof problem>,
  ): value is RuleDefinition => 'trigger' in value;

  const router = new FakeRouter()
    .on('GET', AUTOMATION_PATHS.rules, (request) => {
      const principal = guard(request);
      return isPrincipal(principal) ? ok({ items: db().rules }) : principal;
    })
    .on('POST', AUTOMATION_PATHS.rules, (request) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const definition = read(request);
      if (!isDefinition(definition)) {
        return definition;
      }

      const rule: FakeRule = {
        id: fakeId(),
        enabled: true,
        definition,
        runs24h: 0,
        lastRunAt: null,
      };
      db().rules.unshift(rule);
      return created(rule);
    })
    .on('PUT', AUTOMATION_PATHS.rule, (request, params) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const rule = db().rules.find((item) => item.id === params['id']);
      if (!rule) {
        return notFound();
      }

      const definition = read(request);
      if (!isDefinition(definition)) {
        return definition;
      }

      rule.definition = definition;
      return ok(rule);
    })
    .on('PUT', AUTOMATION_PATHS.status, (request, params) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const rule = db().rules.find((item) => item.id === params['id']);
      const enabled = readBody(request)['enabled'];
      if (!rule) {
        return notFound();
      }

      if (typeof enabled !== 'boolean') {
        return validation([
          fieldError('enabled', 'automations.enabled', messages.fake.invalidRule),
        ]);
      }

      rule.enabled = enabled;
      return ok(rule);
    })
    .on('DELETE', AUTOMATION_PATHS.rule, (request, params) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const before = db().rules.length;
      db().rules = db().rules.filter((item) => item.id !== params['id']);
      return db().rules.length === before ? notFound() : noContent();
    })
    .on('GET', AUTOMATION_PATHS.queues, (request) => {
      const principal = guard(request);
      return isPrincipal(principal) ? ok({ items: db().queues }) : principal;
    })
    .on('GET', AUTOMATION_PATHS.failures, (request) => {
      const principal = guard(request);
      return isPrincipal(principal) ? ok({ items: db().failures }) : principal;
    })
    .on('POST', AUTOMATION_PATHS.retry, (request, params) => {
      const principal = guard(request, admins);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const failure = db().failures.find((item) => item.id === params['id']);
      if (!failure) {
        return notFound();
      }

      db().failures = db().failures.filter((item) => item.id !== failure.id);
      db().retried.push(failure.id);
      const queue = db().queues.find((item) => item.name === failure.queue);
      if (queue) {
        queue.failed = Math.max(0, queue.failed - 1);
        queue.pending += 1;
      }

      return accepted(null);
    });

  return new FakeFeatureApi('automations', router, state.scenarios);
}

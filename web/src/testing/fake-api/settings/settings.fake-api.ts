import { messages } from '../../../app/core/i18n/messages.pt-BR';
import {
  MESSAGE_KINDS,
  unknownVariables,
  WEEKDAYS,
} from '../../../app/features/settings/data/settings.models';
import { isValidCnpj } from '../../../app/shared/util/document';
import { FAKE_TWO_FACTOR_CODE } from '../fake-api/fake-auth-state';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import { FAKE_ADMIN_PASSWORD } from '../fake-api/fake-database';
import {
  fieldError,
  FakeRouter,
  noContent,
  ok,
  paginate,
  problem,
  readBody,
  readText,
  validation,
} from '../fake-api/fake-http';
import { fakeId } from '../fake-api/fake-ids';
import { authorize, FakePrincipal, FakeRole, isPrincipal } from '../fake-api/fake-session';
import { FakeRequest } from '../fake-request';

export const SETTINGS_PATHS = {
  workshop: '/api/admin/settings/workshop',
  logo: '/api/admin/settings/workshop/logo',
  discounts: '/api/admin/settings/discounts',
  payments: '/api/admin/settings/payments',
  messages: '/api/admin/settings/messages',
  security: '/api/admin/settings/security',
  sessions: '/api/admin/settings/sessions',
  session: '/api/admin/settings/sessions/:id',
  audit: '/api/admin/settings/audit/search',
  fiscal: '/api/admin/settings/fiscal',
  reauthenticate: '/api/auth/reauthenticate',
} as const;

export interface FakeAuditEntry {
  id: string;
  occurredAt: string;
  actorName: string;
  area: string;
  action: string;
  target: string;
  detail: string;
}

export interface FakeSettings {
  workshop: {
    name: string;
    document: string;
    address: Record<string, string>;
    logoUrl: string | null;
    hours: { weekday: string; closed: boolean; opens: string; closes: string }[];
    plan: { name: string; renewsAt: string; users: number; maxUsers: number };
  };
  messages: {
    templates: { kind: string; channel: string; body: string }[];
    window: { from: string; to: string; weekdays: string[] };
  };
  security: {
    requireTwoFactorForAll: boolean;
    idleMinutes: number;
    lockoutAttempts: number;
    lockoutMinutes: number;
  };
  sessions: {
    id: string;
    userName: string;
    device: string;
    lastSeenAt: string;
    current: boolean;
  }[];
  audit: FakeAuditEntry[];
  fiscal: { kind: string; status: string; provider: string | null }[];
  reauthTokens: string[];
}

const viewers: readonly FakeRole[] = ['Admin', 'Manager'];
const admins: readonly FakeRole[] = ['Admin'];

export function createSettingsSeed(): FakeSettings {
  const day = (weekday: string, closed = false) => ({
    weekday,
    closed,
    opens: weekday === 'Saturday' ? '08:00' : '07:30',
    closes: weekday === 'Saturday' ? '12:00' : '18:00',
  });
  return {
    workshop: {
      name: 'Oficina Exemplo Ltda',
      document: '11222333000181',
      address: {
        zipCode: '01310100',
        street: 'Avenida Paulista',
        number: '1000',
        district: 'Bela Vista',
        city: 'São Paulo',
        state: 'SP',
      },
      logoUrl: null,
      hours: [
        day('Monday'),
        day('Tuesday'),
        day('Wednesday'),
        day('Thursday'),
        day('Friday'),
        day('Saturday'),
        day('Sunday', true),
      ],
      plan: { name: 'Essencial', renewsAt: '2026-12-01T00:00:00Z', users: 7, maxUsers: 10 },
    },
    messages: {
      templates: [
        {
          kind: 'QuoteSent',
          channel: 'WhatsApp',
          body: 'Olá, {cliente}! O orçamento do seu {veiculo} ({placa}) ficou em {total}. Veja e aprove: {link}',
        },
        {
          kind: 'StatusChanged',
          channel: 'WhatsApp',
          body: 'Olá, {cliente}! O seu {veiculo} mudou de etapa. Previsão de entrega: {prazo}. {link}',
        },
        {
          kind: 'QuoteReminder',
          channel: 'Email',
          body: 'Olá, {cliente}. O orçamento de {total} ainda aguarda sua resposta: {link}',
        },
        {
          kind: 'Receipt',
          channel: 'Email',
          body: 'Olá, {cliente}. Segue o recibo do serviço no {veiculo}: {link}',
        },
      ],
      window: {
        from: '08:00',
        to: '19:00',
        weekdays: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'],
      },
    },
    security: {
      requireTwoFactorForAll: false,
      idleMinutes: 30,
      lockoutAttempts: 5,
      lockoutMinutes: 15,
    },
    sessions: [
      {
        id: 'session-current',
        userName: 'Administrador',
        device: 'Chrome no Windows',
        lastSeenAt: '2026-10-16T12:00:00Z',
        current: true,
      },
      {
        id: 'session-phone',
        userName: 'Marcos Mecânico',
        device: 'Chrome no Android',
        lastSeenAt: '2026-10-16T11:20:00Z',
        current: false,
      },
      {
        id: 'session-desk',
        userName: 'Ana Atendente',
        device: 'Edge no Windows',
        lastSeenAt: '2026-10-15T18:05:00Z',
        current: false,
      },
    ],
    audit: [
      {
        id: fakeId(),
        occurredAt: '2026-10-16T10:15:00Z',
        actorName: 'Gabi Gestora',
        area: 'discounts',
        action: 'Desconto aprovado',
        target: 'Orçamento nº 1042',
        detail: '15% acima do limite do atendente',
      },
      {
        id: fakeId(),
        occurredAt: '2026-10-16T09:02:00Z',
        actorName: 'Ana Atendente',
        area: 'customers',
        action: 'Dado pessoal revelado',
        target: 'Cliente Joana P.',
        detail: 'Telefone, motivo: contato',
      },
      {
        id: fakeId(),
        occurredAt: '2026-10-15T17:40:00Z',
        actorName: 'Administrador',
        area: 'payments',
        action: 'Taxas de pagamento alteradas',
        target: 'Crédito',
        detail: '3,29% para 3,49%',
      },
      {
        id: fakeId(),
        occurredAt: '2026-10-15T08:30:00Z',
        actorName: 'Marcos Mecânico',
        area: 'stock',
        action: 'Ajuste de estoque',
        target: 'Pastilha de freio dianteira',
        detail: '-2 unidades, contagem',
      },
      {
        id: fakeId(),
        occurredAt: '2026-10-14T21:00:00Z',
        actorName: 'Administrador',
        area: 'security',
        action: 'Sessão encerrada',
        target: 'Edge no Windows',
        detail: 'Encerrada pelo administrador',
      },
    ],
    fiscal: [
      { kind: 'NFSe', status: 'Planned', provider: null },
      { kind: 'NFe', status: 'NotConfigured', provider: null },
      { kind: 'NFCe', status: 'NotConfigured', provider: null },
    ],
    reauthTokens: [],
  };
}

export function createSettingsFakeApi(state: FakeBackendState): FakeFeatureApi {
  const db = (): FakeSettings => state.database.settings;
  const quotes = () => state.database.quotes;
  const guard = (request: FakeRequest, roles = viewers) =>
    authorize(request, state.database.users, roles);
  const nameOf = (principal: FakePrincipal) =>
    state.database.users.find((user) => user.id === principal.id)?.name ??
    messages.roles[principal.role];
  const record = (principal: FakePrincipal, area: string, action: string, target: string) => {
    db().audit.unshift({
      id: fakeId(),
      occurredAt: new Date().toISOString(),
      actorName: nameOf(principal),
      area,
      action,
      target,
      detail: '',
    });
  };
  const sensitive = (request: FakeRequest) => {
    const reauth = request.reauthentication ?? null;
    const principal = guard(request, admins);
    if (!isPrincipal(principal)) {
      return principal;
    }

    const index = reauth ? db().reauthTokens.indexOf(reauth) : -1;
    if (index < 0) {
      return problem(403, 'settings.reauthenticationRequired', messages.settings.reauth.required);
    }

    db().reauthTokens.splice(index, 1);
    return principal;
  };
  const invalid = (field: string, text: string) =>
    validation([fieldError(field, `settings.${field}`, text)]);

  const router = new FakeRouter()
    .on('POST', SETTINGS_PATHS.reauthenticate, (request) => {
      const principal = guard(request, admins);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      const user = state.database.users.find((candidate) => candidate.id === principal.id);
      const password = readText(body, 'password');
      const code = readText(body, 'code');
      const valid =
        (password !== '' && password === (user?.password ?? FAKE_ADMIN_PASSWORD)) ||
        code === FAKE_TWO_FACTOR_CODE;
      if (!valid) {
        return problem(422, 'auth.reauthenticationFailed', messages.settings.reauth.failed);
      }

      const token = `reauth-${fakeId()}`;
      db().reauthTokens.push(token);
      return ok({ reauthToken: token });
    })
    .on('GET', SETTINGS_PATHS.workshop, (request) => {
      const principal = guard(request);
      return isPrincipal(principal) ? ok(db().workshop) : principal;
    })
    .on('PUT', SETTINGS_PATHS.workshop, (request) => {
      const principal = sensitive(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      if (!isValidCnpj(readText(body, 'document'))) {
        return invalid('document', messages.settings.workshop.problems.document);
      }

      Object.assign(db().workshop, {
        name: readText(body, 'name'),
        document: readText(body, 'document'),
        address: body['address'],
        hours: body['hours'],
      });
      record(principal, 'security', 'Dados da oficina alterados', db().workshop.name);
      return ok(db().workshop);
    })
    .on('POST', SETTINGS_PATHS.logo, (request) => {
      const principal = sensitive(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      if (!['image/png', 'image/jpeg'].includes(readText(body, 'contentType'))) {
        return invalid('logo', messages.settings.workshop.logoInvalid);
      }

      db().workshop.logoUrl = readText(body, 'dataUrl');
      return ok({ logoUrl: db().workshop.logoUrl });
    })
    .on('GET', SETTINGS_PATHS.discounts, (request) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const limits = quotes().discountLimits;
      return ok({
        rules: quotes().rules,
        limits: Object.entries(limits).map(([role, percent]) => ({ role, percent })),
      });
    })
    .on('PUT', SETTINGS_PATHS.discounts, (request) => {
      const principal = sensitive(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      const rules = Array.isArray(body['rules'])
        ? (body['rules'] as Record<string, unknown>[])
        : [];
      const limits = Array.isArray(body['limits'])
        ? (body['limits'] as Record<string, unknown>[])
        : [];
      if (
        limits.some(
          (item) => typeof item['percent'] !== 'number' || (item['percent'] as number) > 100,
        )
      ) {
        return invalid('limits', messages.settings.discounts.problems.limit);
      }

      quotes().rules = rules.map((rule) => ({
        id: typeof rule['id'] === 'string' && rule['id'] !== '' ? rule['id'] : fakeId(),
        name: readText(rule, 'name'),
        type: rule['type'] === 'Amount' ? 'Amount' : 'Percent',
        value: Number(rule['value']),
        condition: rule['condition'] as 'LoyalCustomer' | 'FirstService' | 'Fleet',
        isActive: rule['isActive'] === true,
      }));
      for (const item of limits) {
        const role = item['role'];
        if (role === 'Admin' || role === 'Manager' || role === 'Attendant' || role === 'Mechanic') {
          quotes().discountLimits[role] = item['percent'] as number;
        }
      }

      record(principal, 'discounts', 'Regras de desconto alteradas', `${rules.length} regras`);
      return ok({
        rules: quotes().rules,
        limits: Object.entries(quotes().discountLimits).map(([role, percent]) => ({
          role,
          percent,
        })),
      });
    })
    .on('GET', SETTINGS_PATHS.payments, (request) => {
      const principal = guard(request);
      return isPrincipal(principal) ? ok(quotes().payment) : principal;
    })
    .on('PUT', SETTINGS_PATHS.payments, (request) => {
      const principal = sensitive(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      const methods = Array.isArray(body['methods']) ? (body['methods'] as string[]) : [];
      if (methods.length === 0) {
        return invalid('methods', messages.settings.payments.problems.methods);
      }

      Object.assign(quotes().payment, {
        methods,
        maxInstallments: body['maxInstallments'],
        interestFreeInstallments: body['interestFreeInstallments'],
        monthlyInterestPercent: body['monthlyInterestPercent'],
        fees: body['fees'],
      });
      record(principal, 'payments', 'Formas e taxas de pagamento alteradas', methods.join(', '));
      return ok(quotes().payment);
    })
    .on('GET', SETTINGS_PATHS.messages, (request) => {
      const principal = guard(request);
      return isPrincipal(principal) ? ok(db().messages) : principal;
    })
    .on('PUT', SETTINGS_PATHS.messages, (request) => {
      const principal = sensitive(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      const templates = Array.isArray(body['templates'])
        ? (body['templates'] as { kind: string; channel: string; body: string }[])
        : [];
      if (
        templates.some(
          (template) =>
            !MESSAGE_KINDS.some((kind) => kind === template.kind) ||
            unknownVariables(String(template.body)).length > 0,
        )
      ) {
        return invalid('templates', messages.settings.messages.problems.unknownVariable);
      }

      const window = body['window'] as FakeSettings['messages']['window'];
      db().messages = {
        templates,
        window: {
          ...window,
          weekdays: window.weekdays.filter((day) => WEEKDAYS.some((w) => w === day)),
        },
      };
      record(principal, 'security', 'Modelos de mensagem alterados', `${templates.length} modelos`);
      return ok(db().messages);
    })
    .on('GET', SETTINGS_PATHS.security, (request) => {
      const principal = guard(request, admins);
      return isPrincipal(principal) ? ok(db().security) : principal;
    })
    .on('PUT', SETTINGS_PATHS.security, (request) => {
      const principal = sensitive(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      const idle = Number(body['idleMinutes']);
      if (!Number.isInteger(idle) || idle < 5 || idle > 60) {
        return invalid('idleMinutes', messages.settings.security.problems.idle);
      }

      db().security = {
        requireTwoFactorForAll: body['requireTwoFactorForAll'] === true,
        idleMinutes: idle,
        lockoutAttempts: Number(body['lockoutAttempts']),
        lockoutMinutes: Number(body['lockoutMinutes']),
      };
      record(principal, 'security', 'Política de segurança alterada', `${idle} min de inatividade`);
      return ok(db().security);
    })
    .on('GET', SETTINGS_PATHS.sessions, (request) => {
      const principal = guard(request, admins);
      return isPrincipal(principal) ? ok({ items: db().sessions }) : principal;
    })
    .on('DELETE', SETTINGS_PATHS.session, (request, params) => {
      const principal = sensitive(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const session = db().sessions.find((item) => item.id === params['id']);
      if (!session || session.current) {
        return problem(404, 'settings.sessionNotFound', messages.errors.notFound);
      }

      db().sessions = db().sessions.filter((item) => item.id !== session.id);
      record(principal, 'security', 'Sessão encerrada', session.device);
      return noContent();
    })
    .on('POST', SETTINGS_PATHS.audit, (request) => {
      const principal = guard(request, admins);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      const area = readText(body, 'area');
      const from = readText(body, 'from');
      const to = readText(body, 'to');
      const actor = readText(body, 'actor').trim().toLowerCase();
      const items = db().audit.filter(
        (entry) =>
          (area === '' || area === 'all' || entry.area === area) &&
          (from === '' || entry.occurredAt.slice(0, 10) >= from) &&
          (to === '' || entry.occurredAt.slice(0, 10) <= to) &&
          (actor === '' || entry.actorName.toLowerCase().includes(actor)),
      );
      const query = new URLSearchParams({
        page: String(body['page'] ?? 1),
        pageSize: String(body['pageSize'] ?? 20),
      });
      return paginate(items, query);
    })
    .on('GET', SETTINGS_PATHS.fiscal, (request) => {
      const principal = guard(request);
      return isPrincipal(principal) ? ok({ integrations: db().fiscal }) : principal;
    });

  return new FakeFeatureApi('settings', router, state.scenarios);
}

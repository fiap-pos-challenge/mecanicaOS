import { TestBed } from '@angular/core/testing';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi, TestApiMode } from '../../../testing/api';
import { signInWithFakeApi } from '../../../testing/auth';
import { expectNoAxeViolations } from '../../../testing/axe';
import { FakeBackend } from '../../../testing/fake-api/fake-backend.service';
import { BackendRole } from '../../core/auth/permissions';
import { messages } from '../../core/i18n/messages.pt-BR';
import { ToastService } from '../../shared/ui/toast/toast';
import { provideAuthData } from '../auth/data/auth.providers';
import { provideAutomationData, toFailure, toQueue, toRule } from './data/automation.gateway';
import {
  actionsFor,
  blankRule,
  readRule,
  RULE_TEMPLATES,
  RuleDefinition,
  templateRule,
  validateRule,
} from './data/automation.models';
import { changeTrigger, conditionFor, newCondition } from './dialogs/rule-editor-dialog';
import { AutomationsPage, ruleSummary } from './pages/automations-page';
import { AutomationsStore } from './state/automations.store';

const text = messages.automations;

function rule(change: Partial<RuleDefinition> = {}): RuleDefinition {
  return { ...templateRule('quoteReminder'), ...change };
}

describe('automation rules', () => {
  it('accepts every ready-made template', () => {
    for (const template of RULE_TEMPLATES) {
      expect(validateRule(templateRule(template))).toEqual([]);
    }
  });

  it('rejects incomplete or incompatible rules', () => {
    expect(validateRule(blankRule())).toEqual(['name', 'messageMissing']);
    expect(validateRule(rule({ actions: [] }))).toEqual(['noActions']);
    expect(
      validateRule(
        rule({
          conditions: Array.from({ length: 6 }, () => ({
            field: 'quoteStatus' as const,
            operator: 'equals' as const,
            value: 'Pending',
          })),
        }),
      ),
    ).toEqual(['tooManyConditions']);
    expect(
      validateRule(
        rule({ actions: Array.from({ length: 6 }, () => ({ kind: 'NotifyTeam', message: 'x' })) }),
      ),
    ).toEqual(['tooManyActions']);
    expect(
      validateRule(rule({ conditions: [{ field: 'partStock', operator: 'equals', value: '1' }] })),
    ).toEqual(['conditionField']);
    expect(
      validateRule(
        rule({ conditions: [{ field: 'quoteStatus', operator: 'greaterThan', value: 'Pending' }] }),
      ),
    ).toEqual(['conditionOperator']);
    expect(
      validateRule(rule({ conditions: [{ field: 'amount', operator: 'lessThan', value: 'abc' }] })),
    ).toEqual(['conditionValue']);
    expect(validateRule(rule({ wait: { amount: 61, unit: 'days' } }))).toEqual(['waitAmount']);
    expect(validateRule(rule({ wait: { amount: 1.5, unit: 'hours' } }))).toEqual(['waitAmount']);
    expect(
      validateRule(rule({ actions: [{ kind: 'SendWhatsApp', message: 'x'.repeat(501) }] })),
    ).toEqual(['messageTooLong']);
    expect(validateRule(rule({ actions: [{ kind: 'SendEmail', message: 'Olá {nome}' }] }))).toEqual(
      ['messagePlaceholder'],
    );
    expect(validateRule(rule({ actions: [{ kind: 'RegisterPayment', message: null }] }))).toEqual([
      'actionTrigger',
    ]);
    expect(actionsFor('StockBelowMinimum')).toContain('CreatePurchaseOrder');
  });

  it('reads only typed rules', () => {
    expect(readRule(null)).toBeNull();
    expect(readRule({ ...rule(), trigger: 'DropDatabase' })).toBeNull();
    expect(
      readRule({ ...rule(), conditions: [{ field: 'sql', operator: 'equals', value: 'x' }] }),
    ).toBeNull();
    expect(readRule({ ...rule(), actions: [{ kind: 'Shell' }] })).toBeNull();
    expect(readRule({ ...rule(), wait: { amount: '2', unit: 'days' } })?.wait).toBeNull();
    expect(readRule({ ...rule(), actions: [{ kind: 'NotifyTeam' }] })?.actions).toEqual([
      { kind: 'NotifyTeam', message: null },
    ]);
  });

  it('keeps only compatible parts when the trigger changes', () => {
    const changed = changeTrigger(templateRule('restock'), 'PaymentReceived');
    expect(changed.actions).toEqual([templateRule('restock').actions[1]]);
    expect(changeTrigger(rule(), 'StockBelowMinimum')).toMatchObject({
      conditions: [],
      actions: rule().actions,
    });
    expect(
      changeTrigger(templateRule('paymentWriteOff'), 'QuoteSent').actions.map((a) => a.kind),
    ).toEqual(['NotifyTeam']);
    expect(
      changeTrigger(
        {
          ...templateRule('paymentWriteOff'),
          actions: [{ kind: 'RegisterPayment', message: null }],
        },
        'QuoteSent',
      ).actions,
    ).toEqual([{ kind: 'NotifyTeam', message: '' }]);
    expect(newCondition('VehicleMaintenanceDue')).toEqual({
      field: 'customerConsent',
      operator: 'equals',
      value: 'true',
    });
    expect(conditionFor('amount')).toEqual({ field: 'amount', operator: 'equals', value: '' });
    expect(ruleSummary(rule())).toBe(
      `${text.summary.when(text.triggers.QuoteSent)}, ${text.summary.wait(2, text.units.days)}. ${text.summary.actions(text.actions.SendWhatsApp)}`,
    );
  });

  it('maps the contract with safe defaults', () => {
    expect(() => toRule({ id: 'r', definition: { name: 'x' } })).toThrow();
    expect(toRule({ id: 'r', definition: rule() })).toMatchObject({
      enabled: false,
      runs24h: 0,
      lastRunAt: null,
    });
    expect(toQueue({ name: 'ai' })).toEqual({ name: 'ai', processed: 0, failed: 0, pending: 0 });
    expect(
      toFailure({ id: 'f', queue: 'ai', summary: 's', failedAt: '2026-10-16T00:00:00Z' }),
    ).toMatchObject({ error: '', attempts: 0 });
  });
});

async function show(role: BackendRole = 'Admin', mode: TestApiMode = 'mock') {
  const view = await render(AutomationsPage, {
    providers: [
      ...provideTestApi(mode, [{ path: '**', children: [] }]),
      provideAuthData(),
      provideAutomationData(),
      AutomationsStore,
    ],
    configureTestBed: () => {
      signInWithFakeApi(role);
    },
  });
  return { view, user: userEvent.setup() };
}

function automations() {
  return TestBed.inject(FakeBackend).state.database.automations;
}

async function toast(message: string): Promise<void> {
  await vi.waitFor(() =>
    expect(
      TestBed.inject(ToastService)
        .toasts()
        .map((item) => item.message),
    ).toContain(message),
  );
}

function field(container: HTMLElement, label: string): HTMLElement {
  return within(container).getByRole('combobox', { name: new RegExp(`^\\s*${label}`) });
}

describe('AutomationsPage', () => {
  it('lists rules, pauses and activates them and shows the queues', async () => {
    const { view, user } = await show();
    const list = await screen.findByRole('list', { name: text.rules.label });
    expect(within(list).getAllByRole('listitem')).toHaveLength(3);
    const reminder = text.templates.names.quoteReminder;
    await user.click(screen.getByRole('switch', { name: text.rules.toggle(reminder) }));
    await toast(text.rules.pausedNow(reminder));
    expect(automations().rules[0]?.enabled).toBe(false);
    await user.click(screen.getByRole('switch', { name: text.rules.toggle(reminder) }));
    await toast(text.rules.activated(reminder));

    expect(screen.getByRole('list', { name: text.queues.label }).textContent).toContain(
      text.queues.names.notifications,
    );
    const failures = screen.getByRole('list', { name: text.queues.failuresLabel });
    await user.click(
      within(failures).getByRole('button', { name: text.queues.retryOf('WhatsApp da OS 3318') }),
    );
    await toast(text.queues.retried);
    expect(automations().retried).toEqual(['failure-whatsapp']);
    await expectNoAxeViolations(view.container);
  });

  it('creates a rule from a template, edits it in the visual editor and removes it', async () => {
    const { user } = await show();
    await screen.findByRole('list', { name: text.rules.label });
    await user.click(screen.getByRole('button', { name: text.templates.names.postSale }));
    const dialog = await screen.findByRole('dialog');
    const name = within(dialog).getByRole('textbox', { name: new RegExp(text.editor.name) });
    await user.clear(name);
    await user.click(within(dialog).getByRole('button', { name: text.editor.save }));
    expect(within(dialog).getByTestId('rule-problems').textContent).toContain(text.problems.name);
    await user.type(name, 'Pós-venda VIP');
    await user.click(within(dialog).getByRole('button', { name: text.editor.addCondition }));
    await user.selectOptions(field(dialog, text.editor.field), 'amount');
    await user.selectOptions(field(dialog, text.editor.operator), 'greaterThan');
    await user.type(
      within(dialog).getByRole('textbox', { name: new RegExp(text.editor.value) }),
      '500',
    );
    await user.click(within(dialog).getByRole('button', { name: text.editor.addAction }));
    await user.click(within(dialog).getByRole('button', { name: text.editor.save }));
    expect(within(dialog).getByTestId('rule-problems').textContent).toContain(
      text.problems.messageMissing,
    );
    await user.click(within(dialog).getByRole('button', { name: text.editor.removeAction(2) }));
    await user.click(within(dialog).getByRole('button', { name: text.editor.save }));
    await toast(text.editor.saved('Pós-venda VIP'));
    expect(automations().rules[0]?.definition).toMatchObject({
      name: 'Pós-venda VIP',
      trigger: 'ServiceOrderDelivered',
      conditions: [{ field: 'amount', operator: 'greaterThan', value: '500' }],
      wait: { amount: 3, unit: 'days' },
    });

    await user.click(screen.getByRole('button', { name: text.rules.edit('Pós-venda VIP') }));
    const editing = await screen.findByRole('dialog');
    await user.selectOptions(field(editing, text.editor.trigger), 'StockBelowMinimum');
    await user.selectOptions(field(editing, text.editor.action), 'CreatePurchaseOrder');
    await user.click(within(editing).getByRole('checkbox', { name: text.editor.wait }));
    await user.click(within(editing).getByRole('button', { name: text.editor.addCondition }));
    await user.clear(within(editing).getByRole('textbox', { name: new RegExp(text.editor.value) }));
    await user.type(
      within(editing).getByRole('textbox', { name: new RegExp(text.editor.value) }),
      '2',
    );
    await user.click(within(editing).getByRole('button', { name: text.editor.removeCondition(1) }));
    await user.click(within(editing).getByRole('button', { name: text.editor.save }));
    await toast(text.editor.saved('Pós-venda VIP'));
    expect(automations().rules[0]?.definition).toMatchObject({
      trigger: 'StockBelowMinimum',
      conditions: [],
      wait: null,
      actions: [{ kind: 'CreatePurchaseOrder', message: null }],
    });

    await user.click(screen.getByRole('button', { name: text.rules.remove('Pós-venda VIP') }));
    await user.click(await screen.findByRole('button', { name: text.rules.removeConfirm }));
    await toast(text.rules.removed('Pós-venda VIP'));
    expect(automations().rules.map((item) => item.definition.name)).not.toContain('Pós-venda VIP');
  });

  it('edits waits and every kind of condition value from a blank rule', async () => {
    const { user } = await show();
    await screen.findByRole('list', { name: text.rules.label });
    await user.click(screen.getByRole('button', { name: text.newRule }));
    const dialog = await screen.findByRole('dialog');
    await user.type(
      within(dialog).getByRole('textbox', { name: new RegExp(text.editor.name) }),
      'Aviso',
    );
    await user.click(within(dialog).getByRole('button', { name: text.editor.addCondition }));
    await user.selectOptions(field(dialog, text.editor.value), 'Completed');
    await user.selectOptions(field(dialog, text.editor.operator), 'notEquals');
    await user.click(within(dialog).getByRole('checkbox', { name: text.editor.wait }));
    const amount = within(dialog).getByRole('spinbutton');
    await user.clear(amount);
    await user.type(amount, '12');
    await user.selectOptions(field(dialog, text.editor.waitUnit), 'hours');
    await user.type(
      within(dialog).getByRole('textbox', { name: new RegExp(text.editor.message) }),
      'OS {{os} mudou',
    );
    await user.selectOptions(field(dialog, text.editor.action), 'SendEmail');
    await user.click(within(dialog).getByRole('button', { name: text.editor.save }));
    await toast(text.editor.saved('Aviso'));
    expect(automations().rules[0]?.definition).toMatchObject({
      conditions: [{ field: 'orderStatus', operator: 'notEquals', value: 'Completed' }],
      wait: { amount: 12, unit: 'hours' },
      actions: [{ kind: 'SendEmail', message: 'OS {os} mudou' }],
    });

    await user.click(screen.getByRole('button', { name: text.templates.names.quoteReminder }));
    const reminder = await screen.findByRole('dialog');
    await user.selectOptions(field(reminder, text.editor.value), 'Rejected');
    await user.click(within(reminder).getByRole('button', { name: text.editor.cancel }));
  });

  it('hides the retry from managers and reports failures', async () => {
    const { user } = await show('Manager');
    await screen.findByRole('list', { name: text.queues.failuresLabel });
    expect(screen.getByText(text.queues.adminOnly)).toBeTruthy();
    expect(screen.queryByRole('button', { name: text.queues.retry })).toBeNull();

    const store = TestBed.inject(AutomationsStore);
    TestBed.inject(FakeBackend).setScenario('automations', 'serverError');
    const first = store.rules()[0];
    const failure = store.failures()[0];
    if (!first || !failure) {
      throw new Error('Missing data');
    }

    await user.click(
      screen.getByRole('switch', { name: text.rules.toggle(first.definition.name) }),
    );
    expect((await store.remove(first)).ok).toBe(false);
    expect((await store.retry(failure)).ok).toBe(false);
    expect((await store.save(null, first.definition)).ok).toBe(false);
    await store.load();
    expect(await screen.findAllByText(text.loadError)).toHaveLength(2);
    TestBed.inject(FakeBackend).setScenario('automations', 'none');
    for (const button of screen.getAllByRole('button', { name: text.retry })) {
      await user.click(button);
    }

    expect(await screen.findByRole('list', { name: text.rules.label })).toBeTruthy();

    TestBed.resetTestingModule();
    await show('Admin', 'pending');
    expect(await screen.findByText(messages.pending.automations)).toBeTruthy();
  });
});

import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { X as XIcon } from 'lucide';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiIcon } from '../../../shared/ui/icon/icon';
import {
  ActionKind,
  actionsFor,
  AutomationRule,
  ConditionField,
  ConditionOperator,
  fieldsFor,
  MAX_ITEMS,
  needsMessage,
  operatorsFor,
  RULE_TRIGGERS,
  RuleAction,
  RuleCondition,
  RuleDefinition,
  RuleTrigger,
  validateRule,
  valuesFor,
  WAIT_UNITS,
  WaitUnit,
} from '../data/automation.models';
import { AutomationsStore } from '../state/automations.store';

export interface RuleEditorData {
  readonly store: AutomationsStore;
  readonly rule: AutomationRule | null;
  readonly definition: RuleDefinition;
}

export function changeTrigger(definition: RuleDefinition, trigger: RuleTrigger): RuleDefinition {
  const fields = fieldsFor(trigger);
  const actions = actionsFor(trigger);
  const kept = definition.actions.filter((action) => actions.includes(action.kind));
  return {
    ...definition,
    trigger,
    conditions: definition.conditions.filter((condition) => fields.includes(condition.field)),
    actions: kept.length > 0 ? kept : [{ kind: 'NotifyTeam', message: '' }],
  };
}

export function conditionFor(field: ConditionField): RuleCondition {
  return {
    field,
    operator: operatorsFor(field)[0] ?? 'equals',
    value: valuesFor(field)?.[0] ?? '',
  };
}

export function newCondition(trigger: RuleTrigger): RuleCondition | null {
  const field = fieldsFor(trigger)[0];
  return field ? conditionFor(field) : null;
}

@Component({
  selector: 'app-rule-editor-dialog',
  imports: [UiButton, UiControl, UiDialogLayout, UiField, UiIcon],
  templateUrl: './rule-editor-dialog.html',
  styleUrl: './rule-editor-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RuleEditorDialog {
  protected readonly ref = inject<DialogRef<string | null>>(DialogRef);
  protected readonly data = inject<RuleEditorData>(DIALOG_DATA);

  protected readonly text = messages.automations;
  protected readonly triggers = RULE_TRIGGERS;
  protected readonly units = WAIT_UNITS;
  protected readonly max = MAX_ITEMS;
  protected readonly removeIcon = XIcon;
  protected readonly statuses = messages.serviceOrders.statuses;
  protected readonly draft = signal<RuleDefinition>(this.data.definition);
  protected readonly submitted = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly problems = computed(() => validateRule(this.draft()));
  protected readonly fields = computed(() => fieldsFor(this.draft().trigger));
  protected readonly actionKinds = computed(() => actionsFor(this.draft().trigger));
  protected readonly title = this.data.rule
    ? this.text.editor.editTitle(this.data.rule.definition.name)
    : this.text.editor.newTitle;

  protected operators(field: ConditionField): readonly ConditionOperator[] {
    return operatorsFor(field);
  }

  protected values(field: ConditionField): readonly string[] | null {
    return valuesFor(field);
  }

  protected valueLabel(field: ConditionField, value: string): string {
    if (field === 'orderStatus') {
      return this.statuses[value as keyof typeof this.statuses];
    }

    if (field === 'quoteStatus') {
      return this.text.quoteStatuses[value as keyof typeof this.text.quoteStatuses];
    }

    return this.text.booleans[value as keyof typeof this.text.booleans];
  }

  protected needsMessage(kind: ActionKind): boolean {
    return needsMessage(kind);
  }

  protected setName(event: Event): void {
    const name = (event.target as HTMLInputElement).value;
    this.draft.update((draft) => ({ ...draft, name }));
  }

  protected setTrigger(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    const trigger = RULE_TRIGGERS.find((item) => item === value);
    if (trigger) {
      this.draft.update((draft) => changeTrigger(draft, trigger));
    }
  }

  protected addCondition(): void {
    const condition = newCondition(this.draft().trigger);
    if (condition) {
      this.draft.update((draft) => ({ ...draft, conditions: [...draft.conditions, condition] }));
    }
  }

  protected removeCondition(index: number): void {
    this.draft.update((draft) => ({
      ...draft,
      conditions: draft.conditions.filter((_, position) => position !== index),
    }));
  }

  protected setCondition(index: number, key: 'field' | 'operator' | 'value', event: Event): void {
    const value = (event.target as HTMLInputElement | HTMLSelectElement).value;
    this.draft.update((draft) => ({
      ...draft,
      conditions: draft.conditions.map((condition, position) => {
        if (position !== index) {
          return condition;
        }

        if (key === 'field') {
          const field = this.fields().find((item) => item === value);
          return field ? conditionFor(field) : condition;
        }

        if (key === 'operator') {
          const operator = operatorsFor(condition.field).find((item) => item === value);
          return operator ? { ...condition, operator } : condition;
        }

        return { ...condition, value };
      }),
    }));
  }

  protected toggleWait(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.draft.update((draft) => ({
      ...draft,
      wait: checked ? { amount: 1, unit: 'days' } : null,
    }));
  }

  protected setWait(key: 'amount' | 'unit', event: Event): void {
    const value = (event.target as HTMLInputElement | HTMLSelectElement).value;
    this.draft.update((draft) => {
      const wait = draft.wait ?? { amount: 1, unit: 'days' };
      if (key === 'amount') {
        return { ...draft, wait: { ...wait, amount: Number(value) } };
      }

      const unit: WaitUnit = WAIT_UNITS.find((item) => item === value) ?? wait.unit;
      return { ...draft, wait: { ...wait, unit } };
    });
  }

  protected addAction(): void {
    this.draft.update((draft) => ({
      ...draft,
      actions: [...draft.actions, { kind: 'NotifyTeam', message: '' }],
    }));
  }

  protected removeAction(index: number): void {
    this.draft.update((draft) => ({
      ...draft,
      actions: draft.actions.filter((_, position) => position !== index),
    }));
  }

  protected setAction(index: number, key: 'kind' | 'message', event: Event): void {
    const value = (event.target as HTMLTextAreaElement | HTMLSelectElement).value;
    this.draft.update((draft) => ({
      ...draft,
      actions: draft.actions.map((action, position): RuleAction => {
        if (position !== index) {
          return action;
        }

        if (key === 'message') {
          return { ...action, message: value };
        }

        const kind = this.actionKinds().find((item) => item === value) ?? action.kind;
        return { kind, message: needsMessage(kind) ? (action.message ?? '') : null };
      }),
    }));
  }

  protected async submit(event: Event): Promise<void> {
    event.preventDefault();
    this.submitted.set(true);
    if (this.problems().length > 0) {
      return;
    }

    this.saving.set(true);
    const definition = { ...this.draft(), name: this.draft().name.trim() };
    const outcome = await this.data.store.save(this.data.rule?.id ?? null, definition);
    this.saving.set(false);
    if (outcome.ok) {
      this.ref.close(definition.name);
    } else {
      this.error.set(outcome.error);
    }
  }
}

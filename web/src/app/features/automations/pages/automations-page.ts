import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiAiBadge } from '../../../shared/ui/ai-badge/ai-badge';
import { UiButton } from '../../../shared/ui/button/button';
import { UiCard } from '../../../shared/ui/card/card';
import { ConfirmDialogData, UiConfirmDialog } from '../../../shared/ui/dialog/confirm-dialog';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { ToastService } from '../../../shared/ui/toast/toast';
import { UiToggle } from '../../../shared/ui/toggle/toggle';
import { formatDateTime } from '../../../shared/util/date';
import { CanDirective } from '../../auth/can.directive';
import { AuthStore } from '../../auth/state/auth.store';
import {
  AutomationRule,
  blankRule,
  QueueFailure,
  RULE_TEMPLATES,
  RuleDefinition,
  RuleTemplate,
  templateRule,
} from '../data/automation.models';
import { RuleEditorData, RuleEditorDialog } from '../dialogs/rule-editor-dialog';
import { AutomationsStore } from '../state/automations.store';

const text = messages.automations;

export function ruleSummary(definition: RuleDefinition): string {
  const wait = definition.wait
    ? `, ${text.summary.wait(definition.wait.amount, text.units[definition.wait.unit])}`
    : '';
  const actions = definition.actions.map((action) => text.actions[action.kind]).join(', ');
  return `${text.summary.when(text.triggers[definition.trigger])}${wait}. ${text.summary.then(actions)}`;
}

@Component({
  selector: 'app-automations-page',
  imports: [
    CanDirective,
    UiAiBadge,
    UiButton,
    UiCard,
    UiPendingNotice,
    UiSkeleton,
    UiStatusPill,
    UiToggle,
  ],
  templateUrl: './automations-page.html',
  styleUrl: './automations-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AutomationsPage {
  protected readonly store = inject(AutomationsStore);
  private readonly dialogs = inject(UiDialogService);
  private readonly toast = inject(ToastService);

  protected readonly text = text;
  protected readonly screen = messages.screens.automations;
  protected readonly pendingText = messages.pending.automations;
  protected readonly templates = RULE_TEMPLATES;
  protected readonly aiRule = messages.ui.ai.hint;
  protected readonly canRetry = inject(AuthStore).can('automations.reprocess');

  constructor() {
    void this.store.load();
  }

  protected summary(rule: AutomationRule): string {
    return ruleSummary(rule.definition);
  }

  protected when(date: string): string {
    return formatDateTime(date);
  }

  protected create(): void {
    void this.edit(null, blankRule());
  }

  protected useTemplate(template: RuleTemplate): void {
    void this.edit(null, templateRule(template));
  }

  protected async edit(rule: AutomationRule | null, definition: RuleDefinition): Promise<void> {
    const ref = this.dialogs.open<RuleEditorDialog, RuleEditorData, string | null>(
      RuleEditorDialog,
      { store: this.store, rule, definition },
    );
    const name = await firstValueFrom(ref.closed);
    if (name) {
      this.toast.show(text.editor.saved(name), { tone: 'success' });
    }
  }

  protected async toggle(rule: AutomationRule): Promise<void> {
    const outcome = await this.store.toggle(rule);
    const name = rule.definition.name;
    this.toast.show(
      outcome.ok
        ? rule.enabled
          ? text.rules.pausedNow(name)
          : text.rules.activated(name)
        : outcome.error,
      { tone: outcome.ok ? 'success' : 'error' },
    );
  }

  protected async remove(rule: AutomationRule): Promise<void> {
    const ref = this.dialogs.open<UiConfirmDialog, ConfirmDialogData, boolean>(UiConfirmDialog, {
      title: text.rules.removeTitle,
      body: text.rules.removeBody(rule.definition.name),
      confirm: text.rules.removeConfirm,
      danger: true,
    });
    if ((await firstValueFrom(ref.closed)) !== true) {
      return;
    }

    const outcome = await this.store.remove(rule);
    this.toast.show(outcome.ok ? text.rules.removed(rule.definition.name) : outcome.error, {
      tone: outcome.ok ? 'success' : 'error',
    });
  }

  protected async retry(item: QueueFailure): Promise<void> {
    const outcome = await this.store.retry(item);
    this.toast.show(outcome.ok ? text.queues.retried : outcome.error, {
      tone: outcome.ok ? 'success' : 'error',
    });
  }
}

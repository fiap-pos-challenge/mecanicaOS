import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiAiBadge } from '../../../shared/ui/ai-badge/ai-badge';
import { UiButton } from '../../../shared/ui/button/button';
import { UiCard } from '../../../shared/ui/card/card';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import {
  AssistantGateway,
  QUESTION_MAX,
  ReportSuggestion,
  validQuestion,
} from '../data/assistant.gateway';
import { ReportDefinition } from '../data/report.models';
import { ReportsStore } from '../state/reports.store';

const text = messages.reports;

export function describeDefinition(definition: ReportDefinition): string {
  const period =
    definition.period.preset === 'custom' && definition.period.from && definition.period.to
      ? text.range(definition.period.from, definition.period.to)
      : text.periods[definition.period.preset];
  return [
    text.title(text.measures[definition.measure], text.groupings[definition.groupBy]),
    period,
    text.charts[definition.chart],
  ].join(' · ');
}

@Component({
  selector: 'app-assistant-panel',
  imports: [UiAiBadge, UiButton, UiCard, UiControl, UiField],
  template: `
    <ui-card class="assistant" [headingLevel]="2" [title]="text.title" [subtitle]="text.hint">
      <form class="form" novalidate (submit)="ask($event)">
        <ui-field
          [label]="text.question"
          [error]="invalid() ? text.tooShort : null"
          [required]="true"
        >
          <textarea
            uiControl
            rows="2"
            [attr.maxlength]="max"
            [value]="question()"
            (input)="setQuestion($event)"
          ></textarea>
        </ui-field>
        <button uiButton type="submit" [loading]="asking()">{{ text.ask }}</button>
      </form>
      @if (error(); as message) {
        <p class="alert" role="alert">{{ message }}</p>
      }
      @if (suggestion(); as suggestion) {
        <section class="suggestion" [attr.aria-label]="text.suggestion" data-ai="true">
          <ui-ai-badge />
          <strong>{{ text.suggestion }}</strong>
          <p class="detail">{{ summary() }}</p>
          <p class="detail">{{ suggestion.explanation }}</p>
          <p class="detail">{{ aiHint }}</p>
          <div class="actions">
            <button uiButton variant="primary" type="button" (click)="confirm(suggestion)">
              {{ text.open }}
            </button>
            <button uiButton type="button" (click)="suggestion$.set(null)">
              {{ text.discard }}
            </button>
          </div>
        </section>
      }
    </ui-card>
  `,
  styles: `
    .form,
    .suggestion {
      display: grid;
      gap: var(--space-2);
      justify-items: start;
    }
    .form {
      justify-items: stretch;
    }
    .form button {
      justify-self: start;
    }
    .suggestion {
      margin-top: var(--space-3);
      padding: var(--space-3);
      border: 1px dashed var(--violet);
      border-radius: var(--radius-md);
      background: var(--violet-soft);
    }
    .detail {
      margin: 0;
      font-size: var(--text-sm);
    }
    .actions {
      display: flex;
      flex-wrap: wrap;
      gap: var(--space-2);
    }
    .alert {
      margin: var(--space-2) 0 0;
      padding: var(--space-3);
      border: 1px solid var(--bad);
      border-radius: var(--radius-md);
      background: var(--bad-soft);
      font-size: var(--text-sm);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssistantPanel {
  private readonly gateway = inject(AssistantGateway);
  private readonly store = inject(ReportsStore);

  protected readonly text = messages.assistant;
  protected readonly aiHint = messages.ui.ai.hint;
  protected readonly max = QUESTION_MAX;
  protected readonly question = signal('');
  protected readonly invalid = signal(false);
  protected readonly asking = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly suggestion$ = signal<ReportSuggestion | null>(null);
  protected readonly suggestion = this.suggestion$.asReadonly();
  protected readonly summary = computed(() => {
    const suggestion = this.suggestion();
    return suggestion ? describeDefinition(suggestion.definition) : '';
  });

  protected setQuestion(event: Event): void {
    this.question.set((event.target as HTMLTextAreaElement).value);
    this.invalid.set(false);
  }

  protected async ask(event: Event): Promise<void> {
    event.preventDefault();
    if (!validQuestion(this.question())) {
      this.invalid.set(true);
      return;
    }

    this.asking.set(true);
    this.error.set(null);
    this.suggestion$.set(null);
    try {
      this.suggestion$.set(await firstValueFrom(this.gateway.suggestReport(this.question())));
    } catch (error) {
      this.error.set(
        isFeaturePending(error)
          ? messages.pending.assistant
          : isApiError(error)
            ? error.detail
            : this.text.invalid,
      );
    } finally {
      this.asking.set(false);
    }
  }

  protected confirm(suggestion: ReportSuggestion): void {
    this.store.apply(suggestion.definition, { fromAssistant: true });
    this.suggestion$.set(null);
  }
}

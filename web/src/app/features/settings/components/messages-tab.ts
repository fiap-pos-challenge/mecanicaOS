import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { ToastService } from '../../../shared/ui/toast/toast';
import {
  MAX_TEMPLATE,
  MessageSettings,
  renderTemplate,
  unknownVariables,
  validateMessages,
  Weekday,
  WEEKDAYS,
} from '../data/settings.models';
import { SectionLoad, SettingsStore } from '../state/settings.store';

@Component({
  selector: 'app-messages-tab',
  imports: [UiButton, UiControl, UiField, UiSkeleton],
  template: `
    @switch (state()) {
      @case ('loading') {
        <ui-skeleton height="200px" />
      }
      @case ('error') {
        <div class="alert" role="alert">
          <span>{{ common.loadError }}</span>
          <button uiButton size="sm" type="button" (click)="load()">{{ common.retry }}</button>
        </div>
      }
    }
    @if (draft(); as settings) {
      <section class="section" [attr.aria-label]="text.title">
        <h2 class="title">{{ text.title }}</h2>
        <p class="hint">{{ text.hint }}</p>
        @if (submitted() && problems().length > 0) {
          <ul class="alert" role="alert">
            @for (problem of problems(); track problem) {
              <li>{{ text.problems[problem] }}</li>
            }
          </ul>
        }
        @for (template of settings.templates; track $index; let index = $index) {
          <div class="list">
            <ui-field
              [label]="text.body(text.kinds[template.kind], text.channels[template.channel])"
              [error]="unknown(template.body)"
            >
              <textarea
                uiControl
                rows="3"
                [attr.maxlength]="max"
                [readOnly]="!store.canManage"
                [value]="template.body"
                (input)="setBody(index, $event)"
              ></textarea>
            </ui-field>
            <p class="preview" [attr.aria-label]="text.preview" data-testid="message-preview">
              {{ preview(template.body) }}
            </p>
          </div>
        }
        <h3 class="subtitle">{{ text.window }}</h3>
        <p class="hint">{{ text.windowHint }}</p>
        <fieldset class="fields" [disabled]="!store.canManage">
          <ui-field [label]="text.from">
            <input
              uiControl
              type="time"
              [value]="settings.window.from"
              (input)="setTime('from', $event)"
            />
          </ui-field>
          <ui-field [label]="text.to">
            <input
              uiControl
              type="time"
              [value]="settings.window.to"
              (input)="setTime('to', $event)"
            />
          </ui-field>
        </fieldset>
        <fieldset class="day" [disabled]="!store.canManage">
          @for (day of weekdays; track day) {
            <label class="check">
              <input
                type="checkbox"
                [checked]="settings.window.weekdays.includes(day)"
                (change)="toggleDay(day)"
              />
              <span>{{ common.weekdays[day] }}</span>
            </label>
          }
        </fieldset>
        @if (store.canManage) {
          <div class="actions">
            <button uiButton variant="primary" type="button" (click)="save()">
              {{ common.save }}
            </button>
          </div>
        }
      </section>
    }
  `,
  styleUrl: './settings-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MessagesTab {
  protected readonly store = inject(SettingsStore);
  private readonly toast = inject(ToastService);

  protected readonly text = messages.settings.messages;
  protected readonly common = messages.settings;
  protected readonly weekdays = WEEKDAYS;
  protected readonly max = MAX_TEMPLATE;
  protected readonly state = signal<SectionLoad>('loading');
  protected readonly draft = signal<MessageSettings | null>(null);
  protected readonly submitted = signal(false);
  protected readonly problems = computed(() => {
    const draft = this.draft();
    return draft ? validateMessages(draft) : [];
  });

  constructor() {
    void this.load();
  }

  async load(): Promise<void> {
    const settings = await this.store.load(this.store.gateway.messages(), this.state);
    if (settings) {
      this.draft.set(settings);
    }
  }

  protected preview(body: string): string {
    return renderTemplate(body, this.text.sample);
  }

  protected unknown(body: string): string | null {
    const names = unknownVariables(body);
    return names.length > 0 ? this.text.unknown(names.map((name) => `{${name}}`).join(', ')) : null;
  }

  protected setBody(index: number, event: Event): void {
    const body = (event.target as HTMLTextAreaElement).value;
    this.draft.update((draft) =>
      draft
        ? {
            ...draft,
            templates: draft.templates.map((template, position) =>
              position === index ? { ...template, body } : template,
            ),
          }
        : draft,
    );
  }

  protected setTime(key: 'from' | 'to', event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.draft.update((draft) =>
      draft ? { ...draft, window: { ...draft.window, [key]: value } } : draft,
    );
  }

  protected toggleDay(day: Weekday): void {
    this.draft.update((draft) => {
      if (!draft) {
        return draft;
      }

      const weekdays = draft.window.weekdays.includes(day)
        ? draft.window.weekdays.filter((item) => item !== day)
        : WEEKDAYS.filter((item) => item === day || draft.window.weekdays.includes(item));
      return { ...draft, window: { ...draft.window, weekdays } };
    });
  }

  protected async save(): Promise<void> {
    const draft = this.draft();
    this.submitted.set(true);
    if (!draft || this.problems().length > 0) {
      return;
    }

    const saved = await this.store.change(this.text.change, (token) =>
      this.store.gateway.saveMessages(draft, token),
    );
    if (saved) {
      this.draft.set(saved);
      this.toast.show(this.common.saved, { tone: 'success' });
    }
  }
}

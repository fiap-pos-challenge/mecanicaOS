import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { BACKEND_ROLES, BackendRole } from '../../../core/auth/permissions';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { ToastService } from '../../../shared/ui/toast/toast';
import { centsFromDecimal, centsToDecimal } from '../../../shared/util/money';
import { DiscountCondition, DiscountRule, DiscountValue } from '../../quotes/data/quote.models';
import { DiscountSettings, validateDiscounts } from '../data/settings.models';
import { SectionLoad, SettingsStore } from '../state/settings.store';

export const CONDITIONS: readonly DiscountCondition[] = ['LoyalCustomer', 'FirstService', 'Fleet'];

export function ruleNumber(value: DiscountValue): number {
  return value.kind === 'Percent' ? value.basisPoints / 100 : centsToDecimal(value.amount);
}

export function toValue(kind: DiscountValue['kind'], amount: number): DiscountValue {
  return kind === 'Percent'
    ? { kind, basisPoints: Math.round(amount * 100) }
    : { kind, amount: centsFromDecimal(amount) };
}

let nextRule = 0;

@Component({
  selector: 'app-discounts-tab',
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
        <fieldset class="list" [disabled]="!store.canManage">
          <ul class="list" [attr.aria-label]="text.rulesLabel">
            @for (rule of settings.rules; track rule.id; let index = $index) {
              <li class="row">
                <ui-field [label]="text.name">
                  <input uiControl [value]="rule.name" (input)="setName(index, $event)" />
                </ui-field>
                <ui-field [label]="text.type">
                  <select uiControl (change)="setKind(index, $event)">
                    <option value="Percent" [selected]="rule.value.kind === 'Percent'">
                      {{ text.types.Percent }}
                    </option>
                    <option value="Amount" [selected]="rule.value.kind === 'Amount'">
                      {{ text.types.Amount }}
                    </option>
                  </select>
                </ui-field>
                <ui-field [label]="text.value">
                  <input
                    uiControl
                    type="number"
                    min="0"
                    step="0.01"
                    [value]="amount(rule)"
                    (input)="setAmount(index, $event)"
                  />
                </ui-field>
                <ui-field [label]="text.condition">
                  <select uiControl (change)="setCondition(index, $event)">
                    @for (condition of conditions; track condition) {
                      <option [value]="condition" [selected]="condition === rule.condition">
                        {{ text.conditions[condition] }}
                      </option>
                    }
                  </select>
                </ui-field>
                <label class="check">
                  <input type="checkbox" [checked]="rule.isActive" (change)="toggle(index)" />
                  <span>{{ text.active }}</span>
                </label>
                @if (store.canManage) {
                  <button
                    uiButton
                    size="sm"
                    variant="danger"
                    type="button"
                    [attr.aria-label]="text.remove(rule.name)"
                    (click)="remove(index)"
                  >
                    {{ text.removeLabel }}
                  </button>
                }
              </li>
            }
          </ul>
          @if (store.canManage) {
            <div>
              <button uiButton size="sm" type="button" (click)="add()">{{ text.add }}</button>
            </div>
          }
        </fieldset>

        <h3 class="subtitle">{{ text.limits }}</h3>
        <p class="hint">{{ text.limitsHint }}</p>
        <fieldset class="fields" [disabled]="!store.canManage">
          @for (role of roles; track role) {
            <ui-field [label]="text.limitOf(roleNames[role])">
              <input
                uiControl
                type="number"
                min="0"
                max="100"
                [value]="settings.limits[role]"
                (input)="setLimit(role, $event)"
              />
            </ui-field>
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
export class DiscountsTab {
  protected readonly store = inject(SettingsStore);
  private readonly toast = inject(ToastService);

  protected readonly text = messages.settings.discounts;
  protected readonly common = messages.settings;
  protected readonly roleNames = messages.roles;
  protected readonly roles = BACKEND_ROLES;
  protected readonly conditions = CONDITIONS;
  protected readonly state = signal<SectionLoad>('loading');
  protected readonly draft = signal<DiscountSettings | null>(null);
  protected readonly submitted = signal(false);
  protected readonly problems = computed(() => {
    const draft = this.draft();
    return draft ? validateDiscounts(draft) : [];
  });

  constructor() {
    void this.load();
  }

  async load(): Promise<void> {
    const settings = await this.store.load(this.store.gateway.discounts(), this.state);
    if (settings) {
      this.draft.set(settings);
    }
  }

  protected amount(rule: DiscountRule): number {
    return ruleNumber(rule.value);
  }

  protected setName(index: number, event: Event): void {
    const name = (event.target as HTMLInputElement).value;
    this.updateRule(index, (rule) => ({ ...rule, name }));
  }

  protected setKind(index: number, event: Event): void {
    const kind = (event.target as HTMLSelectElement).value === 'Amount' ? 'Amount' : 'Percent';
    this.updateRule(index, (rule) => ({ ...rule, value: toValue(kind, ruleNumber(rule.value)) }));
  }

  protected setAmount(index: number, event: Event): void {
    const amount = Number((event.target as HTMLInputElement).value);
    this.updateRule(index, (rule) => ({ ...rule, value: toValue(rule.value.kind, amount) }));
  }

  protected setCondition(index: number, event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    const condition = CONDITIONS.find((item) => item === value);
    if (condition) {
      this.updateRule(index, (rule) => ({ ...rule, condition }));
    }
  }

  protected toggle(index: number): void {
    this.updateRule(index, (rule) => ({ ...rule, isActive: !rule.isActive }));
  }

  protected add(): void {
    this.draft.update((draft) =>
      draft
        ? {
            ...draft,
            rules: [
              ...draft.rules,
              {
                id: `new-${++nextRule}`,
                name: this.text.newRule,
                value: toValue('Percent', 5),
                condition: 'LoyalCustomer',
                isActive: true,
              },
            ],
          }
        : draft,
    );
  }

  protected remove(index: number): void {
    this.draft.update((draft) =>
      draft ? { ...draft, rules: draft.rules.filter((_, position) => position !== index) } : draft,
    );
  }

  protected setLimit(role: BackendRole, event: Event): void {
    const value = Number((event.target as HTMLInputElement).value);
    this.draft.update((draft) =>
      draft ? { ...draft, limits: { ...draft.limits, [role]: value } } : draft,
    );
  }

  protected async save(): Promise<void> {
    const draft = this.draft();
    this.submitted.set(true);
    if (!draft || this.problems().length > 0) {
      return;
    }

    const saved = await this.store.change(this.text.change, (token) =>
      this.store.gateway.saveDiscounts(
        {
          ...draft,
          rules: draft.rules.map((rule) =>
            rule.id.startsWith('new-') ? { ...rule, id: '' } : rule,
          ),
        },
        token,
      ),
    );
    if (saved) {
      this.draft.set(saved);
      this.toast.show(this.common.saved, { tone: 'success' });
    }
  }

  private updateRule(index: number, change: (rule: DiscountRule) => DiscountRule): void {
    this.draft.update((draft) =>
      draft
        ? {
            ...draft,
            rules: draft.rules.map((rule, position) => (position === index ? change(rule) : rule)),
          }
        : draft,
    );
  }
}

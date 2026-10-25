import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { ToastService } from '../../../shared/ui/toast/toast';
import { PaymentMethod } from '../../quotes/data/quote.models';
import { PaymentConfiguration, PaymentFee, validatePayments } from '../data/settings.models';
import { SectionLoad, SettingsStore } from '../state/settings.store';

export const PAYMENT_METHODS: readonly PaymentMethod[] = [
  'Pix',
  'Credit',
  'Debit',
  'Cash',
  'Boleto',
];

export function feeOf(settings: PaymentConfiguration, method: PaymentMethod): PaymentFee {
  return (
    settings.fees.find((fee) => fee.method === method) ?? {
      method,
      feeBasisPoints: 0,
      settlementDays: 0,
    }
  );
}

@Component({
  selector: 'app-payments-tab',
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
          <legend class="subtitle">{{ text.methods }}</legend>
          @for (method of methods; track method) {
            <div class="row">
              <label class="check">
                <input
                  type="checkbox"
                  [checked]="settings.methods.includes(method)"
                  (change)="toggle(method)"
                />
                <span>{{ text.methodNames[method] }}</span>
              </label>
              <ui-field [label]="text.fee(text.methodNames[method])">
                <input
                  uiControl
                  type="number"
                  min="0"
                  step="0.01"
                  [value]="fee(method).feeBasisPoints / 100"
                  (input)="setFee(method, 'feeBasisPoints', $event)"
                />
              </ui-field>
              <ui-field [label]="text.settlement(text.methodNames[method])">
                <input
                  uiControl
                  type="number"
                  min="0"
                  [value]="fee(method).settlementDays"
                  (input)="setFee(method, 'settlementDays', $event)"
                />
              </ui-field>
            </div>
          }
        </fieldset>
        <h3 class="subtitle">{{ text.installments }}</h3>
        <fieldset class="fields" [disabled]="!store.canManage">
          <ui-field [label]="text.maxInstallments">
            <input
              uiControl
              type="number"
              min="1"
              max="24"
              [value]="settings.credit.maxInstallments"
              (input)="setCredit('maxInstallments', $event)"
            />
          </ui-field>
          <ui-field [label]="text.interestFree">
            <input
              uiControl
              type="number"
              min="1"
              [value]="settings.credit.interestFreeInstallments"
              (input)="setCredit('interestFreeInstallments', $event)"
            />
          </ui-field>
          <ui-field [label]="text.interest">
            <input
              uiControl
              type="number"
              min="0"
              step="0.01"
              [value]="settings.credit.monthlyInterestBasisPoints / 100"
              (input)="setCredit('monthlyInterestBasisPoints', $event)"
            />
          </ui-field>
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
export class PaymentsTab {
  protected readonly store = inject(SettingsStore);
  private readonly toast = inject(ToastService);

  protected readonly text = messages.settings.payments;
  protected readonly common = messages.settings;
  protected readonly methods = PAYMENT_METHODS;
  protected readonly state = signal<SectionLoad>('loading');
  protected readonly draft = signal<PaymentConfiguration | null>(null);
  protected readonly submitted = signal(false);
  protected readonly problems = computed(() => {
    const draft = this.draft();
    return draft ? validatePayments(draft) : [];
  });

  constructor() {
    void this.load();
  }

  async load(): Promise<void> {
    const settings = await this.store.load(this.store.gateway.payments(), this.state);
    if (settings) {
      this.draft.set(settings);
    }
  }

  protected fee(method: PaymentMethod): PaymentFee {
    const draft = this.draft();
    return draft ? feeOf(draft, method) : { method, feeBasisPoints: 0, settlementDays: 0 };
  }

  protected toggle(method: PaymentMethod): void {
    this.draft.update((draft) =>
      draft
        ? {
            ...draft,
            methods: draft.methods.includes(method)
              ? draft.methods.filter((item) => item !== method)
              : PAYMENT_METHODS.filter((item) => item === method || draft.methods.includes(item)),
          }
        : draft,
    );
  }

  protected setFee(method: PaymentMethod, key: 'feeBasisPoints' | 'settlementDays', event: Event) {
    const raw = Number((event.target as HTMLInputElement).value);
    const value = key === 'feeBasisPoints' ? Math.round(raw * 100) : raw;
    this.draft.update((draft) =>
      draft
        ? {
            ...draft,
            fees: PAYMENT_METHODS.map((item) => {
              const fee = feeOf(draft, item);
              return item === method ? { ...fee, [key]: value } : fee;
            }),
          }
        : draft,
    );
  }

  protected setCredit(
    key: 'maxInstallments' | 'interestFreeInstallments' | 'monthlyInterestBasisPoints',
    event: Event,
  ): void {
    const raw = Number((event.target as HTMLInputElement).value);
    const value = key === 'monthlyInterestBasisPoints' ? Math.round(raw * 100) : raw;
    this.draft.update((draft) =>
      draft ? { ...draft, credit: { ...draft.credit, [key]: value } } : draft,
    );
  }

  protected async save(): Promise<void> {
    const draft = this.draft();
    this.submitted.set(true);
    if (!draft || this.problems().length > 0) {
      return;
    }

    const saved = await this.store.change(this.text.change, (token) =>
      this.store.gateway.savePayments(draft, token),
    );
    if (saved) {
      this.draft.set(saved);
      this.toast.show(this.common.saved, { tone: 'success' });
    }
  }
}

import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  linkedSignal,
  output,
  untracked,
} from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiCard } from '../../../shared/ui/card/card';
import { UiChip } from '../../../shared/ui/chip/chip';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { SegmentedOption, UiSegmented } from '../../../shared/ui/segmented/segmented';
import { cents, formatAmount, formatBrl, parseBrl } from '../../../shared/util/money';
import { Installment, QuotePricing, ruleMatches, toBasisPoints } from '../data/quote.math';
import {
  CustomerFacts,
  DiscountApproval,
  DiscountMode,
  DiscountValue,
  ManualDiscount,
  PAYMENT_METHODS,
  PaymentMethod,
  PaymentPlan,
  QuoteOptions,
} from '../data/quote.models';
import { formatBasisPoints, formatDiscount, parseDecimal } from '../quote-display';

type ManualKind = DiscountValue['kind'];

export function toManualValue(kind: ManualKind, value: string): DiscountValue {
  if (kind === 'Percent') {
    const percent = parseDecimal(value);
    return { kind, basisPoints: percent === null ? 0 : toBasisPoints(percent) };
  }

  return { kind, amount: parseBrl(value) ?? cents(0) };
}

export function manualText(value: DiscountValue | null): string {
  if (!value) {
    return '';
  }

  return value.kind === 'Percent'
    ? String(value.basisPoints / 100).replace('.', ',')
    : formatAmount(value.amount);
}

@Component({
  selector: 'app-quote-pricing',
  imports: [UiButton, UiCard, UiChip, UiControl, UiField, UiSegmented],
  templateUrl: './quote-pricing.html',
  styleUrl: './quote-pricing.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuotePricingPanel {
  readonly mode = input.required<DiscountMode>();
  readonly manual = input<ManualDiscount | null>(null);
  readonly payment = input.required<PaymentPlan>();
  readonly options = input.required<QuoteOptions>();
  readonly facts = input.required<CustomerFacts>();
  readonly pricing = input.required<QuotePricing>();
  readonly approval = input<DiscountApproval | null>(null);
  readonly installments = input<readonly Installment[]>([]);
  readonly editable = input(true);
  readonly canApprove = input(false);
  readonly busy = input(false);
  readonly resetKey = input('');
  readonly modeChange = output<DiscountMode>();
  readonly manualChange = output<ManualDiscount>();
  readonly paymentChange = output<PaymentPlan>();
  readonly approvalRequested = output();
  readonly decided = output<boolean>();

  protected readonly text = messages.quotes;
  protected readonly modes: readonly SegmentedOption<DiscountMode>[] = (
    ['Automatic', 'Manual', 'None'] as const
  ).map((value) => ({ value, label: this.text.discount.modes[value] }));
  protected readonly kinds: readonly SegmentedOption<ManualKind>[] = (
    ['Percent', 'Amount'] as const
  ).map((value) => ({ value, label: this.text.discount.kinds[value] }));
  protected readonly kind = linkedSignal<string, ManualKind>({
    source: this.resetKey,
    computation: () => untracked(() => this.manual()?.value.kind ?? 'Percent'),
  });
  protected readonly value = linkedSignal<string, string>({
    source: this.resetKey,
    computation: () => untracked(() => manualText(this.manual()?.value ?? null)),
  });
  protected readonly reason = linkedSignal<string, string>({
    source: this.resetKey,
    computation: () => untracked(() => this.manual()?.reason ?? ''),
  });
  protected readonly rules = computed(() =>
    this.options()
      .rules.filter((rule) => rule.isActive)
      .map((rule) => ({
        rule,
        label: `${rule.name} · ${formatDiscount(rule.value)}`,
        matches: ruleMatches(rule, this.facts()),
        applied: this.pricing().appliedRule?.id === rule.id,
      })),
  );
  protected readonly limit = computed(() =>
    formatBasisPoints(this.options().discountLimitBasisPoints),
  );
  protected readonly methods = computed(() =>
    PAYMENT_METHODS.filter((method) => this.options().payment.methods.includes(method)),
  );
  protected readonly installmentOptions = computed(() =>
    Array.from({ length: this.options().payment.credit.maxInstallments }, (_, index) => index + 1),
  );
  protected readonly simulation = computed(() =>
    this.installments().map((plan) =>
      plan.interestFree
        ? this.text.payment.interestFree(plan.count, formatBrl(plan.amount)) +
          (plan.first !== plan.amount ? this.text.payment.firstDiffers(formatBrl(plan.first)) : '')
        : this.text.payment.withInterest(plan.count, formatBrl(plan.amount), formatBrl(plan.total)),
    ),
  );
  protected readonly totals = computed(() => {
    const pricing = this.pricing();
    return {
      services: formatBrl(pricing.services),
      parts: formatBrl(pricing.parts),
      discountLabel:
        this.mode() === 'Manual'
          ? this.text.totals.discountManual
          : pricing.appliedRule
            ? this.text.totals.discountRule(pricing.appliedRule.name)
            : this.text.totals.discount,
      discount: `− ${formatBrl(pricing.discount)}`,
      total: formatBrl(pricing.total),
    };
  });
  protected readonly approvalText = computed(() => {
    const approval = this.approval();
    if (!approval || this.mode() !== 'Manual') {
      return null;
    }

    return this.text.discount.approval[approval.status](
      approval.status === 'Pending' ? approval.requestedBy : (approval.decidedBy ?? ''),
    );
  });

  protected emitManual(): void {
    this.manualChange.emit({
      value: toManualValue(this.kind(), this.value()),
      reason: this.reason(),
    });
  }

  protected setKind(kind: ManualKind): void {
    this.kind.set(kind);
    this.emitManual();
  }

  protected setValue(event: Event): void {
    this.value.set((event.target as HTMLInputElement).value);
    this.emitManual();
  }

  protected setReason(event: Event): void {
    this.reason.set((event.target as HTMLInputElement).value);
    this.emitManual();
  }

  protected toggleMethod(method: PaymentMethod, selected: boolean): void {
    const current = this.payment().methods;
    const methods = selected
      ? PAYMENT_METHODS.filter((item) => item === method || current.includes(item))
      : current.filter((item) => item !== method);
    this.paymentChange.emit({ ...this.payment(), methods });
  }

  protected setInstallments(event: Event): void {
    this.paymentChange.emit({
      ...this.payment(),
      maxInstallments: Number((event.target as HTMLSelectElement).value),
    });
  }
}

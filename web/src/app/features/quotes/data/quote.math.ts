import { addCents, cents, Cents, multiplyCents, percentOf } from '../../../shared/util/money';
import {
  CreditSettings,
  CustomerFacts,
  DiscountApproval,
  DiscountMode,
  DiscountRule,
  DiscountValue,
  ManualDiscount,
  PricedLine,
  QuoteLine,
  QuoteOption,
  QuoteTotals,
} from './quote.models';

export const MAX_QUANTITY = 999;
export const MAX_BASIS_POINTS = 10_000;
export const MIN_REASON_LENGTH = 5;

export interface PricingInput {
  readonly lines: readonly PricedLine[];
  readonly rules: readonly DiscountRule[];
  readonly facts: CustomerFacts;
  readonly mode: DiscountMode;
  readonly manual: ManualDiscount | null;
  readonly limitBasisPoints: number;
}

export interface QuotePricing extends QuoteTotals {
  readonly subtotal: Cents;
  readonly appliedRule: DiscountRule | null;
  readonly exceedsLimit: boolean;
}

export type SendBlock =
  | 'Locked'
  | 'Empty'
  | 'ZeroTotal'
  | 'InvalidQuantity'
  | 'InvalidDiscount'
  | 'ReasonRequired'
  | 'ApprovalRequired'
  | 'ApprovalPending'
  | 'ApprovalRejected';

export interface Installment {
  readonly count: number;
  readonly amount: Cents;
  readonly first: Cents;
  readonly total: Cents;
  readonly interestFree: boolean;
}

export interface StockWarning {
  readonly lineId: string;
  readonly name: string;
  readonly missing: number;
  readonly leadTimeDays: number | null;
  readonly supplier: string | null;
}

export interface PriceChange {
  readonly lineId: string;
  readonly name: string;
  readonly before: Cents;
  readonly after: Cents;
}

export interface LineChange {
  readonly key: string;
  readonly name: string;
  readonly before: PricedLine | null;
  readonly after: PricedLine | null;
}

export interface VersionComparison {
  readonly changes: readonly LineChange[];
  readonly totalDelta: Cents;
}

export function isValidQuantity(quantity: number, fractional: boolean): boolean {
  if (!Number.isFinite(quantity) || quantity <= 0 || quantity > MAX_QUANTITY) {
    return false;
  }

  const thousandths = quantity * 1000;
  return fractional
    ? Math.abs(thousandths - Math.round(thousandths)) < 1e-6
    : Number.isInteger(quantity);
}

export function lineTotal(line: PricedLine): Cents {
  return multiplyCents(line.unitPrice, line.quantity);
}

export function sumLines(lines: readonly PricedLine[]): { services: Cents; parts: Cents } {
  const total = (kinds: readonly PricedLine['kind'][]): Cents =>
    addCents(...lines.filter((line) => kinds.includes(line.kind)).map(lineTotal));
  return { services: total(['Service', 'Package']), parts: total(['Part']) };
}

export function isValidDiscount(value: DiscountValue): boolean {
  return value.kind === 'Percent'
    ? Number.isInteger(value.basisPoints) &&
        value.basisPoints > 0 &&
        value.basisPoints <= MAX_BASIS_POINTS
    : Number.isSafeInteger(value.amount) && value.amount > 0;
}

export function discountAmount(value: DiscountValue, subtotal: Cents): Cents {
  if (subtotal <= 0 || !isValidDiscount(value)) {
    return cents(0);
  }

  const raw = value.kind === 'Percent' ? percentOf(subtotal, value.basisPoints) : value.amount;
  return cents(Math.min(raw, subtotal));
}

export function ruleMatches(rule: DiscountRule, facts: CustomerFacts): boolean {
  if (!rule.isActive) {
    return false;
  }

  switch (rule.condition) {
    case 'LoyalCustomer':
      return facts.loyal;
    case 'FirstService':
      return facts.firstService;
    case 'Fleet':
      return facts.fleet;
  }
}

export function bestRule(
  rules: readonly DiscountRule[],
  facts: CustomerFacts,
  subtotal: Cents,
): { readonly rule: DiscountRule; readonly amount: Cents } | null {
  return rules
    .filter((rule) => ruleMatches(rule, facts))
    .map((rule) => ({ rule, amount: discountAmount(rule.value, subtotal) }))
    .reduce<{ rule: DiscountRule; amount: Cents } | null>(
      (best, current) => (best === null || current.amount > best.amount ? current : best),
      null,
    );
}

export function exceedsLimit(discount: Cents, subtotal: Cents, limitBasisPoints: number): boolean {
  return subtotal > 0 && discount * MAX_BASIS_POINTS > limitBasisPoints * subtotal;
}

export function priceQuote(input: PricingInput): QuotePricing {
  const { services, parts } = sumLines(input.lines);
  const subtotal = addCents(services, parts);
  const automatic =
    input.mode === 'Automatic' ? bestRule(input.rules, input.facts, subtotal) : null;
  const discount =
    input.mode === 'Manual' && input.manual
      ? discountAmount(input.manual.value, subtotal)
      : (automatic?.amount ?? cents(0));
  return {
    services,
    parts,
    subtotal,
    discount,
    total: cents(subtotal - discount),
    appliedRule: automatic?.rule ?? null,
    exceedsLimit:
      input.mode === 'Manual' && exceedsLimit(discount, subtotal, input.limitBasisPoints),
  };
}

export function sameDiscount(a: DiscountValue, b: DiscountValue): boolean {
  return a.kind === 'Percent'
    ? b.kind === 'Percent' && a.basisPoints === b.basisPoints
    : b.kind === 'Amount' && a.amount === b.amount;
}

export function approvalBlock(
  manual: ManualDiscount,
  approval: DiscountApproval | null,
): SendBlock | null {
  if (!approval || !sameDiscount(approval.discount, manual.value)) {
    return 'ApprovalRequired';
  }

  switch (approval.status) {
    case 'Approved':
      return null;
    case 'Pending':
      return 'ApprovalPending';
    case 'Rejected':
      return 'ApprovalRejected';
  }
}

export function sendBlocks(state: {
  readonly editable: boolean;
  readonly lines: readonly QuoteLine[];
  readonly mode: DiscountMode;
  readonly manual: ManualDiscount | null;
  readonly approval: DiscountApproval | null;
  readonly pricing: QuotePricing;
}): readonly SendBlock[] {
  const blocks: SendBlock[] = [];
  if (!state.editable) {
    blocks.push('Locked');
  }

  if (state.lines.length === 0) {
    blocks.push('Empty');
  } else if (state.pricing.total <= 0) {
    blocks.push('ZeroTotal');
  }

  if (state.lines.some((line) => !isValidQuantity(line.quantity, line.fractional))) {
    blocks.push('InvalidQuantity');
  }

  if (state.mode === 'Manual') {
    const manual = state.manual;
    if (!manual || !isValidDiscount(manual.value)) {
      blocks.push('InvalidDiscount');
    } else {
      if (manual.reason.trim().length < MIN_REASON_LENGTH) {
        blocks.push('ReasonRequired');
      }

      const approval = state.pricing.exceedsLimit ? approvalBlock(manual, state.approval) : null;
      if (approval) {
        blocks.push(approval);
      }
    }
  }

  return blocks;
}

export function simulateInstallments(total: Cents, credit: CreditSettings): readonly Installment[] {
  if (total <= 0) {
    return [];
  }

  const rate = credit.monthlyInterestBasisPoints / MAX_BASIS_POINTS;
  return Array.from({ length: Math.max(credit.maxInstallments, 1) }, (_, index) => {
    const count = index + 1;
    if (count <= credit.interestFreeInstallments || rate <= 0) {
      const amount = Math.floor(total / count);
      return {
        count,
        amount: cents(amount),
        first: cents(total - amount * (count - 1)),
        total,
        interestFree: true,
      };
    }

    const amount = cents(Math.round((total * rate) / (1 - (1 + rate) ** -count)));
    return {
      count,
      amount,
      first: amount,
      total: multiplyCents(amount, count),
      interestFree: false,
    };
  });
}

export function stockWarnings(
  lines: readonly QuoteLine[],
  parts: readonly QuoteOption[],
): readonly StockWarning[] {
  return lines.flatMap((line) => {
    const stock = parts.find((part) => part.itemId === line.itemId)?.stock;
    if (line.kind !== 'Part' || !stock || line.quantity <= stock.available) {
      return [];
    }

    return [
      {
        lineId: line.lineId,
        name: line.name,
        missing: Math.round((line.quantity - stock.available) * 1000) / 1000,
        leadTimeDays: stock.leadTimeDays,
        supplier: stock.supplier,
      },
    ];
  });
}

export function priceChanges(
  lines: readonly QuoteLine[],
  catalog: readonly QuoteOption[],
): readonly PriceChange[] {
  return lines.flatMap((line) => {
    const current = catalog.find(
      (option) => option.kind === line.kind && option.itemId === line.itemId,
    );
    return current && current.unitPrice !== line.unitPrice
      ? [{ lineId: line.lineId, name: line.name, before: line.unitPrice, after: current.unitPrice }]
      : [];
  });
}

function lineKey(line: QuoteLine): string {
  return `${line.kind}:${line.itemId}`;
}

function sameLine(a: PricedLine, b: PricedLine): boolean {
  return a.quantity === b.quantity && a.unitPrice === b.unitPrice;
}

export function compareVersions(
  before: { readonly lines: readonly QuoteLine[]; readonly totals: QuoteTotals },
  after: { readonly lines: readonly QuoteLine[]; readonly totals: QuoteTotals },
): VersionComparison {
  const previous = new Map(before.lines.map((line) => [lineKey(line), line]));
  const next = new Map(after.lines.map((line) => [lineKey(line), line]));
  const keys = [...new Set([...previous.keys(), ...next.keys()])];
  const changes = keys.flatMap((key): LineChange[] => {
    const old = previous.get(key) ?? null;
    const current = next.get(key) ?? null;
    if (old && current && sameLine(old, current)) {
      return [];
    }

    return [{ key, name: (current ?? old)?.name ?? key, before: old, after: current }];
  });
  return { changes, totalDelta: cents(after.totals.total - before.totals.total) };
}

export function toBasisPoints(percent: number): number {
  return Math.round(percent * 100);
}

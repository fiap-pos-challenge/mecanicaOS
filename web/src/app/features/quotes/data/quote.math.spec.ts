import { cents, Cents } from '../../../shared/util/money';
import {
  approvalBlock,
  bestRule,
  compareVersions,
  discountAmount,
  exceedsLimit,
  isValidDiscount,
  isValidQuantity,
  lineTotal,
  priceChanges,
  priceQuote,
  PricingInput,
  ruleMatches,
  sameDiscount,
  sendBlocks,
  simulateInstallments,
  stockWarnings,
  sumLines,
  toBasisPoints,
} from './quote.math';
import {
  CustomerFacts,
  DiscountApproval,
  DiscountRule,
  DiscountValue,
  QuoteLine,
  QuoteOption,
} from './quote.models';

const percent = (basisPoints: number): DiscountValue => ({ kind: 'Percent', basisPoints });
const amount = (value: number): DiscountValue => ({ kind: 'Amount', amount: cents(value) });

const noFacts: CustomerFacts = { loyal: false, firstService: false, fleet: false };

function line(
  kind: QuoteLine['kind'],
  unitPrice: number,
  quantity = 1,
  itemId = `${kind}-${unitPrice}`,
): QuoteLine {
  return {
    lineId: `line-${itemId}`,
    kind,
    itemId,
    name: itemId,
    unitPrice: cents(unitPrice),
    quantity,
    source: 'Manual',
    fractional: kind === 'Part',
  };
}

function rule(
  id: string,
  value: DiscountValue,
  condition: DiscountRule['condition'],
  isActive = true,
): DiscountRule {
  return { id, name: id, value, condition, isActive };
}

const loyal5 = rule('loyal', percent(500), 'LoyalCustomer');
const first30 = rule('first', amount(3_000), 'FirstService');
const fleet8 = rule('fleet', percent(800), 'Fleet');

function input(overrides: Partial<PricingInput> = {}): PricingInput {
  return {
    lines: [line('Service', 10_000), line('Part', 5_000, 2)],
    rules: [loyal5, first30, fleet8],
    facts: noFacts,
    mode: 'Automatic',
    manual: null,
    limitBasisPoints: 1_000,
    ...overrides,
  };
}

function approval(status: DiscountApproval['status'], discount: DiscountValue): DiscountApproval {
  return {
    id: 'a',
    status,
    discount,
    reason: 'Cliente antigo',
    requestedBy: 'Ana',
    requestedAt: '2026-10-01T00:00:00Z',
    decidedBy: null,
  };
}

describe('quantities', () => {
  it.each([
    [1, false, true],
    [999, false, true],
    [1000, false, false],
    [0, false, false],
    [-1, false, false],
    [1.5, false, false],
    [1.5, true, true],
    [0.125, true, true],
    [0.1234, true, false],
    [Number.NaN, true, false],
    [Number.POSITIVE_INFINITY, true, false],
  ])('quantity %s (fractional %s) is valid: %s', (quantity, fractional, expected) => {
    expect(isValidQuantity(quantity, fractional)).toBe(expected);
  });
});

describe('line and subtotal math in integer cents', () => {
  it('multiplies with half-up rounding of the cent', () => {
    expect(lineTotal(line('Part', 2_990, 1.5))).toBe(4_485);
    expect(lineTotal(line('Part', 1_999, 0.333))).toBe(666);
    expect(lineTotal(line('Part', 1, 0.5))).toBe(1);
    expect(lineTotal(line('Part', 3, 0.5))).toBe(2);
    expect(Number.isInteger(lineTotal(line('Part', 3_333, 0.7)))).toBe(true);
  });

  it('splits packages and services from parts', () => {
    expect(
      sumLines([line('Package', 55_900), line('Service', 6_000), line('Part', 4_400, 4)]),
    ).toEqual({ services: 61_900, parts: 17_600 });
    expect(sumLines([])).toEqual({ services: 0, parts: 0 });
  });
});

describe('discount values', () => {
  it.each([
    [percent(1), true],
    [percent(10_000), true],
    [percent(0), false],
    [percent(10_001), false],
    [percent(12.5), false],
    [amount(1), true],
    [amount(0), false],
    [amount(-100), false],
    [{ kind: 'Amount', amount: 1.5 as Cents } as DiscountValue, false],
  ])('%j is valid: %s', (value, expected) => {
    expect(isValidDiscount(value)).toBe(expected);
  });

  it('rounds percentages to the nearest cent and never exceeds the subtotal', () => {
    expect(discountAmount(percent(1_000), cents(20_000))).toBe(2_000);
    expect(discountAmount(percent(333), cents(10_001))).toBe(333);
    expect(discountAmount(percent(250), cents(99))).toBe(2);
    expect(discountAmount(percent(10_000), cents(12_345))).toBe(12_345);
    expect(discountAmount(amount(50_000), cents(12_345))).toBe(12_345);
    expect(discountAmount(amount(1_000), cents(0))).toBe(0);
    expect(discountAmount(percent(0), cents(1_000))).toBe(0);
  });

  it('compares discounts by kind and value', () => {
    expect(sameDiscount(percent(500), percent(500))).toBe(true);
    expect(sameDiscount(percent(500), percent(501))).toBe(false);
    expect(sameDiscount(percent(500), amount(500))).toBe(false);
    expect(sameDiscount(amount(500), amount(500))).toBe(true);
    expect(sameDiscount(amount(500), percent(500))).toBe(false);
  });

  it('converts percent to basis points', () => {
    expect(toBasisPoints(12.5)).toBe(1_250);
    expect(toBasisPoints(0.015)).toBe(2);
  });
});

describe('discount rules', () => {
  it('matches only active rules whose condition holds', () => {
    expect(ruleMatches(loyal5, { ...noFacts, loyal: true })).toBe(true);
    expect(ruleMatches(loyal5, noFacts)).toBe(false);
    expect(ruleMatches(first30, { ...noFacts, firstService: true })).toBe(true);
    expect(ruleMatches(fleet8, { ...noFacts, fleet: true })).toBe(true);
    expect(ruleMatches({ ...fleet8, isActive: false }, { ...noFacts, fleet: true })).toBe(false);
  });

  it('applies the biggest discount when several rules match', () => {
    const all = { loyal: true, firstService: true, fleet: true };
    expect(bestRule([loyal5, first30, fleet8], all, cents(20_000))?.rule.id).toBe('first');
    expect(bestRule([loyal5, first30, fleet8], all, cents(100_000))?.rule.id).toBe('fleet');
    expect(bestRule([loyal5, first30, fleet8], noFacts, cents(100_000))).toBeNull();
  });

  it('keeps the first rule on ties', () => {
    const twin = rule('twin', percent(500), 'LoyalCustomer');
    expect(bestRule([loyal5, twin], { ...noFacts, loyal: true }, cents(1_000))?.rule.id).toBe(
      'loyal',
    );
  });
});

describe('limits', () => {
  it('compares the discount share with the profile limit without rounding', () => {
    expect(exceedsLimit(cents(1_000), cents(10_000), 1_000)).toBe(false);
    expect(exceedsLimit(cents(1_001), cents(10_000), 1_000)).toBe(true);
    expect(exceedsLimit(cents(1), cents(10_000), 0)).toBe(true);
    expect(exceedsLimit(cents(0), cents(10_000), 0)).toBe(false);
    expect(exceedsLimit(cents(500), cents(0), 0)).toBe(false);
  });
});

describe('priceQuote', () => {
  it('prices without discount when no rule matches', () => {
    expect(priceQuote(input())).toEqual({
      services: 10_000,
      parts: 10_000,
      subtotal: 20_000,
      discount: 0,
      total: 20_000,
      appliedRule: null,
      exceedsLimit: false,
    });
  });

  it('applies the best automatic rule', () => {
    const pricing = priceQuote(input({ facts: { loyal: true, firstService: false, fleet: true } }));
    expect(pricing.appliedRule?.id).toBe('fleet');
    expect(pricing.discount).toBe(1_600);
    expect(pricing.total).toBe(18_400);
    expect(pricing.exceedsLimit).toBe(false);
  });

  it('ignores rules when the mode is none', () => {
    const pricing = priceQuote(input({ mode: 'None', facts: { ...noFacts, loyal: true } }));
    expect(pricing.discount).toBe(0);
    expect(pricing.appliedRule).toBeNull();
  });

  it('replaces automatic rules with the manual discount and checks the limit', () => {
    const facts = { ...noFacts, loyal: true };
    const within = priceQuote(
      input({ facts, mode: 'Manual', manual: { value: percent(1_000), reason: 'Fidelidade' } }),
    );
    expect(within).toMatchObject({ discount: 2_000, total: 18_000, appliedRule: null });
    expect(within.exceedsLimit).toBe(false);

    const above = priceQuote(
      input({ mode: 'Manual', manual: { value: amount(2_001), reason: 'Concorrente' } }),
    );
    expect(above).toMatchObject({ discount: 2_001, total: 17_999, exceedsLimit: true });
  });

  it('never lets the total go negative', () => {
    const pricing = priceQuote(
      input({ mode: 'Manual', manual: { value: amount(999_999), reason: 'Cortesia total' } }),
    );
    expect(pricing.discount).toBe(20_000);
    expect(pricing.total).toBe(0);
    expect(priceQuote(input({ lines: [], facts: { ...noFacts, fleet: true } })).total).toBe(0);
  });

  it('treats a manual mode without a value as no discount', () => {
    expect(priceQuote(input({ mode: 'Manual', manual: null })).discount).toBe(0);
  });
});

describe('approvals and send blocks', () => {
  const manual = { value: percent(1_500), reason: 'Cliente antigo' };

  it('requires an approval for the exact discount', () => {
    expect(approvalBlock(manual, null)).toBe('ApprovalRequired');
    expect(approvalBlock(manual, approval('Approved', percent(1_000)))).toBe('ApprovalRequired');
    expect(approvalBlock(manual, approval('Pending', percent(1_500)))).toBe('ApprovalPending');
    expect(approvalBlock(manual, approval('Rejected', percent(1_500)))).toBe('ApprovalRejected');
    expect(approvalBlock(manual, approval('Approved', percent(1_500)))).toBeNull();
  });

  function blocks(overrides: Partial<Parameters<typeof sendBlocks>[0]> & Partial<PricingInput>) {
    const pricingInput = input(overrides);
    return sendBlocks({
      editable: true,
      lines: pricingInput.lines as QuoteLine[],
      mode: pricingInput.mode,
      manual: pricingInput.manual,
      approval: null,
      pricing: priceQuote(pricingInput),
      ...overrides,
    });
  }

  it('allows a valid quote', () => {
    expect(blocks({})).toEqual([]);
  });

  it('blocks locked, empty, zero and invalid quantity quotes', () => {
    expect(blocks({ editable: false })).toEqual(['Locked']);
    expect(blocks({ lines: [] })).toEqual(['Empty']);
    expect(blocks({ lines: [line('Service', 0)] })).toEqual(['ZeroTotal']);
    expect(blocks({ lines: [line('Service', 1_000, 1.5)] })).toEqual(['InvalidQuantity']);
  });

  it('blocks invalid manual discounts, short reasons and approvals', () => {
    expect(blocks({ mode: 'Manual', manual: null })).toEqual(['InvalidDiscount']);
    expect(blocks({ mode: 'Manual', manual: { value: percent(0), reason: 'Motivo' } })).toEqual([
      'InvalidDiscount',
    ]);
    expect(blocks({ mode: 'Manual', manual: { value: percent(500), reason: ' ok ' } })).toEqual([
      'ReasonRequired',
    ]);
    expect(blocks({ mode: 'Manual', manual })).toEqual(['ApprovalRequired']);
    expect(
      blocks({ mode: 'Manual', manual, approval: approval('Pending', percent(1_500)) }),
    ).toEqual(['ApprovalPending']);
    expect(
      blocks({ mode: 'Manual', manual, approval: approval('Approved', percent(1_500)) }),
    ).toEqual([]);
  });

  it('blocks a full courtesy as a zero total', () => {
    expect(
      blocks({ mode: 'Manual', manual: { value: percent(10_000), reason: 'Cortesia' } }),
    ).toEqual(['ZeroTotal', 'ApprovalRequired']);
  });
});

describe('installments', () => {
  const credit = {
    maxInstallments: 6,
    interestFreeInstallments: 3,
    monthlyInterestBasisPoints: 199,
  };

  it('splits interest-free installments and puts the remainder in the first one', () => {
    const plans = simulateInstallments(cents(10_000), credit);
    expect(plans).toHaveLength(6);
    expect(plans[0]).toEqual({
      count: 1,
      amount: 10_000,
      first: 10_000,
      total: 10_000,
      interestFree: true,
    });
    expect(plans[2]).toEqual({
      count: 3,
      amount: 3_333,
      first: 3_334,
      total: 10_000,
      interestFree: true,
    });
    for (const plan of plans.filter((item) => item.interestFree)) {
      expect(plan.first + plan.amount * (plan.count - 1)).toBe(10_000);
    }
  });

  it('applies compound monthly interest (Price table) above the free installments', () => {
    const plans = simulateInstallments(cents(10_000), credit);
    expect(plans[3]).toEqual({
      count: 4,
      amount: 2_626,
      first: 2_626,
      total: 10_504,
      interestFree: false,
    });
    expect(plans[5]?.amount).toBe(1_785);
    expect(plans.every((plan) => Number.isInteger(plan.amount) && plan.total >= 10_000)).toBe(true);
  });

  it('handles zero interest, zero totals and a minimum of one installment', () => {
    expect(
      simulateInstallments(cents(1_000), { ...credit, monthlyInterestBasisPoints: 0 })[5],
    ).toEqual({ count: 6, amount: 166, first: 170, total: 1_000, interestFree: true });
    expect(simulateInstallments(cents(0), credit)).toEqual([]);
    expect(simulateInstallments(cents(500), { ...credit, maxInstallments: 0 })).toHaveLength(1);
  });
});

describe('stock and price checks', () => {
  const option = (itemId: string, price: number, stock: QuoteOption['stock']): QuoteOption => ({
    kind: 'Part',
    itemId,
    name: itemId,
    detail: null,
    unitPrice: cents(price),
    fractional: false,
    stock,
  });

  it('warns about parts without enough stock with the supplier lead time', () => {
    const lines = [
      line('Part', 4_400, 4, 'spark'),
      line('Part', 3_800, 1, 'filter'),
      line('Service', 1),
    ];
    const parts = [
      option('spark', 4_400, { available: 0, leadTimeDays: 2, supplier: 'Auto Peças Centro' }),
      option('filter', 3_800, { available: 10, leadTimeDays: 2, supplier: null }),
    ];
    expect(stockWarnings(lines, parts)).toEqual([
      {
        lineId: 'line-spark',
        name: 'spark',
        missing: 4,
        leadTimeDays: 2,
        supplier: 'Auto Peças Centro',
      },
    ]);
    expect(
      stockWarnings(
        [line('Part', 1, 2.5, 'oil')],
        [option('oil', 1, { available: 1.2, leadTimeDays: null, supplier: null })],
      )[0]?.missing,
    ).toBe(1.3);
    expect(stockWarnings([line('Part', 1, 1, 'gone')], [])).toEqual([]);
  });

  it('shows the difference when the catalog price changed after the draft', () => {
    const lines = [line('Part', 17_900, 1, 'pads'), line('Part', 3_800, 1, 'filter')];
    const catalog = [option('pads', 18_900, null), option('filter', 3_800, null)];
    expect(priceChanges(lines, catalog)).toEqual([
      { lineId: 'line-pads', name: 'pads', before: 17_900, after: 18_900 },
    ]);
  });
});

describe('compareVersions', () => {
  const totals = (total: number) => ({
    services: cents(0),
    parts: cents(0),
    discount: cents(0),
    total: cents(total),
  });

  it('lists added, removed and changed lines with the total delta', () => {
    const before = {
      lines: [
        line('Service', 6_000, 1, 'oil'),
        line('Part', 4_400, 4, 'spark'),
        line('Part', 3_800, 1, 'air'),
      ],
      totals: totals(27_400),
    };
    const after = {
      lines: [
        line('Service', 6_000, 1, 'oil'),
        line('Part', 4_400, 2, 'spark'),
        line('Service', 9_000, 1, 'align'),
      ],
      totals: totals(23_800),
    };
    const result = compareVersions(before, after);
    expect(result.totalDelta).toBe(-3_600);
    expect(
      result.changes.map((change) => [
        change.name,
        change.before?.quantity ?? null,
        change.after?.quantity ?? null,
      ]),
    ).toEqual([
      ['spark', 4, 2],
      ['air', 1, null],
      ['align', null, 1],
    ]);
  });
});

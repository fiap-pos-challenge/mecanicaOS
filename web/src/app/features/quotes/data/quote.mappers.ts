import { required } from '../../../core/data/contract-error';
import { Page } from '../../../core/data/page';
import { Cents, centsFromDecimal, centsToDecimal } from '../../../shared/util/money';
import {
  DiscountApprovalDto,
  DiscountDto,
  DiscountRuleDto,
  OpenServiceOrderDto,
  PaymentSettingsDto,
  QuoteDeliveryDto,
  QuoteDraftInputDto,
  QuoteDto,
  QuoteLineDto,
  QuoteOptionDto,
  QuoteOptionsDto,
  QuotePageDto,
  QuoteSummaryDto,
  QuoteVersionDto,
  SendQuoteResultDto,
} from './quote.contracts';
import { toBasisPoints } from './quote.math';
import {
  AiSuggestion,
  DiscountApproval,
  DiscountRule,
  DiscountValue,
  OpenServiceOrder,
  PaymentSettings,
  Quote,
  QuoteDelivery,
  QuoteDraft,
  QuoteLine,
  QuoteOption,
  QuoteOptions,
  QuoteSummary,
  QuoteTotals,
  QuoteVersion,
  SendQuoteResult,
} from './quote.models';

function money(value: number | undefined, field: string): Cents {
  return centsFromDecimal(required(value, field));
}

export function toDiscountValue(dto: DiscountDto | undefined, field: string): DiscountValue {
  const discount = required(dto, field);
  const value = required(discount.value, `${field}.value`);
  return required(discount.type, `${field}.type`) === 'Percent'
    ? { kind: 'Percent', basisPoints: toBasisPoints(value) }
    : { kind: 'Amount', amount: centsFromDecimal(value) };
}

export function toDiscountDto(value: DiscountValue): { type: 'Percent' | 'Amount'; value: number } {
  return value.kind === 'Percent'
    ? { type: 'Percent', value: value.basisPoints / 100 }
    : { type: 'Amount', value: centsToDecimal(value.amount) };
}

export function toQuoteLine(dto: QuoteLineDto): QuoteLine {
  return {
    lineId: required(dto.id, 'items.id'),
    kind: required(dto.kind, 'items.kind'),
    itemId: required(dto.itemId, 'items.itemId'),
    name: required(dto.name, 'items.name'),
    unitPrice: money(dto.unitPrice, 'items.unitPrice'),
    quantity: required(dto.quantity, 'items.quantity'),
    source: dto.source ?? 'Manual',
    fractional: dto.fractional ?? false,
  };
}

export function toApproval(dto: DiscountApprovalDto): DiscountApproval {
  return {
    id: required(dto.id, 'approval.id'),
    status: required(dto.status, 'approval.status'),
    discount: toDiscountValue(dto.discount, 'approval.discount'),
    reason: dto.reason ?? '',
    requestedBy: required(dto.requestedBy, 'approval.requestedBy'),
    requestedAt: required(dto.requestedAt, 'approval.requestedAt'),
    decidedBy: dto.decidedBy ?? null,
  };
}

export function toDelivery(dto: QuoteDeliveryDto): QuoteDelivery {
  return {
    id: required(dto.id, 'delivery.id'),
    channel: required(dto.channel, 'delivery.channel'),
    status: required(dto.status, 'delivery.status'),
    link: required(dto.link, 'delivery.link'),
    expiresAt: required(dto.expiresAt, 'delivery.expiresAt'),
    sentAt: dto.sentAt ?? null,
    openedAt: dto.openedAt ?? null,
    failureReason: dto.failureReason ?? null,
    attempts: dto.attempts ?? 1,
  };
}

function toTotals(dto: {
  servicesTotal?: number;
  partsTotal?: number;
  discountTotal?: number;
  total?: number;
}): QuoteTotals {
  return {
    services: money(dto.servicesTotal, 'servicesTotal'),
    parts: money(dto.partsTotal, 'partsTotal'),
    discount: money(dto.discountTotal, 'discountTotal'),
    total: money(dto.total, 'total'),
  };
}

export function toQuote(dto: QuoteDto): Quote {
  const customer = required(dto.customer, 'customer');
  const vehicle = required(dto.vehicle, 'vehicle');
  const manual = dto.manualDiscount ?? null;
  return {
    id: required(dto.id, 'id'),
    number: required(dto.number, 'number'),
    serviceOrderId: required(dto.serviceOrderId, 'serviceOrderId'),
    serviceOrderNumber: required(dto.serviceOrderNumber, 'serviceOrderNumber'),
    version: required(dto.version, 'version'),
    status: required(dto.status, 'status'),
    customer: {
      id: required(customer.id, 'customer.id'),
      name: required(customer.name, 'customer.name'),
      maskedPhone: customer.maskedPhone ?? null,
      maskedEmail: customer.maskedEmail ?? null,
      consentWhatsApp: customer.consentWhatsApp ?? false,
      consentEmail: customer.consentEmail ?? false,
      facts: {
        loyal: customer.loyal ?? false,
        firstService: customer.firstService ?? false,
        fleet: customer.fleet ?? false,
      },
    },
    vehicle: {
      plate: required(vehicle.licensePlate, 'vehicle.licensePlate'),
      description: vehicle.description ?? '',
      mileage: vehicle.mileage ?? 0,
    },
    diagnosis: dto.diagnosis ?? null,
    lines: (dto.items ?? []).map(toQuoteLine),
    discountMode: dto.discountMode ?? 'Automatic',
    manualDiscount: manual
      ? { value: toDiscountValue(manual, 'manualDiscount'), reason: manual.reason ?? '' }
      : null,
    payment: {
      methods: dto.payment?.methods ?? [],
      maxInstallments: dto.payment?.maxInstallments ?? 1,
    },
    approval: dto.approval ? toApproval(dto.approval) : null,
    delivery: dto.delivery ? toDelivery(dto.delivery) : null,
    rejectionReason: dto.rejectionReason ?? null,
    totals: toTotals(dto),
    updatedAt: required(dto.updatedAt, 'updatedAt'),
  };
}

export function toQuoteDraftDto(draft: QuoteDraft): QuoteDraftInputDto {
  return {
    items: draft.lines.map((line) => ({
      kind: line.kind,
      itemId: line.itemId,
      quantity: line.quantity,
      unitPrice: centsToDecimal(line.unitPrice),
      source: line.source,
    })),
    discountMode: draft.discountMode,
    manualDiscount: draft.manualDiscount
      ? { ...toDiscountDto(draft.manualDiscount.value), reason: draft.manualDiscount.reason.trim() }
      : null,
    payment: { methods: draft.payment.methods, maxInstallments: draft.payment.maxInstallments },
  };
}

export function toQuoteSummary(dto: QuoteSummaryDto): QuoteSummary {
  return {
    id: required(dto.id, 'id'),
    number: required(dto.number, 'number'),
    version: required(dto.version, 'version'),
    status: required(dto.status, 'status'),
    customerName: required(dto.customerName, 'customerName'),
    plate: required(dto.licensePlate, 'licensePlate'),
    total: money(dto.total, 'total'),
    deliveryStatus: dto.deliveryStatus ?? null,
    approvalPending: dto.approvalPending ?? false,
    updatedAt: required(dto.updatedAt, 'updatedAt'),
  };
}

export function toQuotePage(dto: QuotePageDto): Page<QuoteSummary> {
  return {
    items: (dto.items ?? []).map(toQuoteSummary),
    page: required(dto.page, 'page'),
    pageSize: required(dto.pageSize, 'pageSize'),
    totalItems: required(dto.totalItems, 'totalItems'),
  };
}

export function toQuoteVersion(dto: QuoteVersionDto): QuoteVersion {
  return {
    id: required(dto.id, 'id'),
    version: required(dto.version, 'version'),
    status: required(dto.status, 'status'),
    lines: (dto.items ?? []).map(toQuoteLine),
    totals: toTotals(dto),
    rejectionReason: dto.rejectionReason ?? null,
    createdAt: required(dto.createdAt, 'createdAt'),
  };
}

export function toQuoteOption(dto: QuoteOptionDto): QuoteOption {
  const stock = dto.stock ?? null;
  return {
    kind: required(dto.kind, 'kind'),
    itemId: required(dto.itemId, 'itemId'),
    name: required(dto.name, 'name'),
    detail: dto.detail ?? null,
    unitPrice: money(dto.unitPrice, 'unitPrice'),
    fractional: dto.fractional ?? false,
    stock: stock
      ? {
          available: stock.available ?? 0,
          leadTimeDays: stock.leadTimeDays ?? null,
          supplier: stock.supplier ?? null,
        }
      : null,
  };
}

export function toSuggestion(dto: QuoteOptionDto): AiSuggestion {
  return { ...toQuoteOption(dto), reason: dto.reason ?? '' };
}

export function toDiscountRule(rule: DiscountRuleDto): DiscountRule {
  return {
    id: required(rule.id, 'discountRules.id'),
    name: required(rule.name, 'discountRules.name'),
    value: toDiscountValue(rule, 'discountRules'),
    condition: required(rule.condition, 'discountRules.condition'),
    isActive: rule.isActive ?? true,
  };
}

export function toPaymentSettings(payment: PaymentSettingsDto): PaymentSettings {
  return {
    methods: payment.methods ?? [],
    credit: {
      maxInstallments: payment.maxInstallments ?? 1,
      interestFreeInstallments: payment.interestFreeInstallments ?? 1,
      monthlyInterestBasisPoints: toBasisPoints(payment.monthlyInterestPercent ?? 0),
    },
  };
}

export function toQuoteOptions(dto: QuoteOptionsDto): QuoteOptions {
  return {
    packages: (dto.packages ?? []).map(toQuoteOption),
    services: (dto.services ?? []).map(toQuoteOption),
    parts: (dto.parts ?? []).map(toQuoteOption),
    suggestions: (dto.suggestions ?? []).map(toSuggestion),
    rules: (dto.discountRules ?? []).map(toDiscountRule),
    discountLimitBasisPoints: toBasisPoints(dto.discountLimitPercent ?? 0),
    payment: toPaymentSettings(dto.payment ?? {}),
    expirationDays: dto.expirationDays ?? [7],
  };
}

export function toOpenServiceOrder(dto: OpenServiceOrderDto): OpenServiceOrder {
  return {
    id: required(dto.id, 'id'),
    number: required(dto.number, 'number'),
    customerName: required(dto.customerName, 'customerName'),
    plate: required(dto.licensePlate, 'licensePlate'),
    vehicle: dto.vehicle ?? '',
  };
}

export function toSendResult(dto: SendQuoteResultDto): SendQuoteResult {
  return {
    delivery: toDelivery(required(dto.delivery, 'delivery')),
    whatsAppUrl: dto.whatsAppUrl ?? null,
  };
}

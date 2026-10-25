import { Cents } from '../../../shared/util/money';

export type QuoteStatus = 'Pending' | 'Approved' | 'Rejected';

export type QuoteItemKind = 'Package' | 'Service' | 'Part';

export type QuoteItemSource = 'Manual' | 'Ai';

export type DiscountCondition = 'LoyalCustomer' | 'FirstService' | 'Fleet';

export type DiscountMode = 'Automatic' | 'Manual' | 'None';

export type PaymentMethod = 'Pix' | 'Credit' | 'Debit' | 'Cash' | 'Boleto';

export type DeliveryChannel = 'WhatsApp' | 'Email' | 'Link';

export type DeliveryStatus = 'Queued' | 'Sent' | 'Opened' | 'Failed';

export type ApprovalStatus = 'Pending' | 'Approved' | 'Rejected';

export const PAYMENT_METHODS: readonly PaymentMethod[] = [
  'Pix',
  'Credit',
  'Debit',
  'Cash',
  'Boleto',
];

export const DELIVERY_CHANNELS: readonly DeliveryChannel[] = ['WhatsApp', 'Email', 'Link'];

export type DiscountValue =
  | { readonly kind: 'Percent'; readonly basisPoints: number }
  | { readonly kind: 'Amount'; readonly amount: Cents };

export interface ManualDiscount {
  readonly value: DiscountValue;
  readonly reason: string;
}

export interface DiscountRule {
  readonly id: string;
  readonly name: string;
  readonly value: DiscountValue;
  readonly condition: DiscountCondition;
  readonly isActive: boolean;
}

export interface CustomerFacts {
  readonly loyal: boolean;
  readonly firstService: boolean;
  readonly fleet: boolean;
}

export interface PricedLine {
  readonly kind: QuoteItemKind;
  readonly unitPrice: Cents;
  readonly quantity: number;
}

export interface QuoteLine extends PricedLine {
  readonly lineId: string;
  readonly itemId: string;
  readonly name: string;
  readonly source: QuoteItemSource;
  readonly fractional: boolean;
}

export interface PartStock {
  readonly available: number;
  readonly leadTimeDays: number | null;
  readonly supplier: string | null;
}

export interface QuoteOption {
  readonly kind: QuoteItemKind;
  readonly itemId: string;
  readonly name: string;
  readonly detail: string | null;
  readonly unitPrice: Cents;
  readonly fractional: boolean;
  readonly stock: PartStock | null;
}

export interface AiSuggestion extends QuoteOption {
  readonly reason: string;
}

export interface CreditSettings {
  readonly maxInstallments: number;
  readonly interestFreeInstallments: number;
  readonly monthlyInterestBasisPoints: number;
}

export interface PaymentSettings {
  readonly methods: readonly PaymentMethod[];
  readonly credit: CreditSettings;
}

export interface PaymentPlan {
  readonly methods: readonly PaymentMethod[];
  readonly maxInstallments: number;
}

export interface QuoteOptions {
  readonly packages: readonly QuoteOption[];
  readonly services: readonly QuoteOption[];
  readonly parts: readonly QuoteOption[];
  readonly suggestions: readonly AiSuggestion[];
  readonly rules: readonly DiscountRule[];
  readonly discountLimitBasisPoints: number;
  readonly payment: PaymentSettings;
  readonly expirationDays: readonly number[];
}

export interface QuoteCustomer {
  readonly id: string;
  readonly name: string;
  readonly maskedPhone: string | null;
  readonly maskedEmail: string | null;
  readonly consentWhatsApp: boolean;
  readonly consentEmail: boolean;
  readonly facts: CustomerFacts;
}

export interface QuoteVehicle {
  readonly plate: string;
  readonly description: string;
  readonly mileage: number;
}

export interface DiscountApproval {
  readonly id: string;
  readonly status: ApprovalStatus;
  readonly discount: DiscountValue;
  readonly reason: string;
  readonly requestedBy: string;
  readonly requestedAt: string;
  readonly decidedBy: string | null;
}

export interface QuoteDelivery {
  readonly id: string;
  readonly channel: DeliveryChannel;
  readonly status: DeliveryStatus;
  readonly link: string;
  readonly expiresAt: string;
  readonly sentAt: string | null;
  readonly openedAt: string | null;
  readonly failureReason: string | null;
  readonly attempts: number;
}

export interface QuoteTotals {
  readonly services: Cents;
  readonly parts: Cents;
  readonly discount: Cents;
  readonly total: Cents;
}

export interface QuoteDraft {
  readonly lines: readonly QuoteLine[];
  readonly discountMode: DiscountMode;
  readonly manualDiscount: ManualDiscount | null;
  readonly payment: PaymentPlan;
}

export interface Quote extends QuoteDraft {
  readonly id: string;
  readonly number: number;
  readonly serviceOrderId: string;
  readonly serviceOrderNumber: number;
  readonly version: number;
  readonly status: QuoteStatus;
  readonly customer: QuoteCustomer;
  readonly vehicle: QuoteVehicle;
  readonly diagnosis: string | null;
  readonly approval: DiscountApproval | null;
  readonly delivery: QuoteDelivery | null;
  readonly rejectionReason: string | null;
  readonly totals: QuoteTotals;
  readonly updatedAt: string;
}

export interface QuoteSummary {
  readonly id: string;
  readonly number: number;
  readonly version: number;
  readonly status: QuoteStatus;
  readonly customerName: string;
  readonly plate: string;
  readonly total: Cents;
  readonly deliveryStatus: DeliveryStatus | null;
  readonly approvalPending: boolean;
  readonly updatedAt: string;
}

export interface QuoteVersion {
  readonly id: string;
  readonly version: number;
  readonly status: QuoteStatus;
  readonly lines: readonly QuoteLine[];
  readonly totals: QuoteTotals;
  readonly rejectionReason: string | null;
  readonly createdAt: string;
}

export interface OpenServiceOrder {
  readonly id: string;
  readonly number: number;
  readonly customerName: string;
  readonly plate: string;
  readonly vehicle: string;
}

export interface QuoteListQuery {
  readonly status: QuoteStatus | null;
  readonly page: number;
  readonly pageSize: number;
}

export interface SendQuoteInput {
  readonly channel: DeliveryChannel;
  readonly expiresInDays: number;
  readonly message: string;
}

export interface SendQuoteResult {
  readonly delivery: QuoteDelivery;
  readonly whatsAppUrl: string | null;
}
